using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace SingleBoostr.Client
{
    public class Program 
    {
        private static async Task<int> Main()
        {
            var applist = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applist.txt");
            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SingleBoostr.IdlingProcess.exe");
            var config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

            if (!File.Exists(exe))
            {
                Console.WriteLine("Helper exe not located, please put this program beside the helper exe or reinstall this program");
                await Task.Delay(-1);
                return 1;
            }

            if (!File.Exists(applist))
            {
                File.CreateText(applist).Dispose();
                Console.WriteLine("App list doesn't exist (applist.txt)");
                Console.WriteLine("App list file has been created, please add your appids that you want to idle in the config file");
                Console.WriteLine("(exit this program, then edit the config file)");
                await Task.Delay(-1);
                return 1;
            }

            if (!File.Exists(config))
            {
                File.CreateText(config).Dispose();
                Console.WriteLine("Config doesn't exist (config.ini)");
                Console.WriteLine("Config file has been created, please adjust accordingly");
                
                var _configParser = new FileIniDataParser();
                var _configData = new IniData();
                _configData.Sections.AddSection("Config");
                _configData["Config"].AddKey("SecondsUntilRestart", "3600");
                _configParser.WriteFile(config, _configData);
                
                Console.WriteLine("(exit this program, then edit the config file)");
                await Task.Delay(-1);
                return 1;
            }

            var configParser = new FileIniDataParser();
            var configData = configParser.ReadFile(config);

            if (int.TryParse(configData["Config"]["SecondsUntilRestart"], out var seconds))
            {
                // If config fails, set the default reset interval to 1 hour
                if (seconds <= 0)
                {
                    Console.WriteLine("SecondsUntilRestart is zero or a negative number - defaulting to 3600 seconds (1 hour)");
                    seconds = 3600;
                }
                
                Console.WriteLine($"Idling processes will restart every {seconds} seconds");
            }

            foreach (var i in File.ReadAllLines(applist))
            {
                if (!int.TryParse(i, out _) || string.IsNullOrWhiteSpace(i))
                {
                    Console.WriteLine($"AppId {i} is an invalid AppId, skipping");
                    continue;
                }

                var currentProcessId = Process.GetCurrentProcess().Id;
                
                var startInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = true,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    Arguments = $"{i} {currentProcessId}"
                };

                var startProcess = Process.Start(startInfo);
                ActiveIdlingProcesses.Add(new IdlingAppData(startProcess, int.Parse(i)));
                Console.WriteLine($"AppId {i} is now boosting!");
            }

            var random = new Random();

            while (true)
            {
                await Task.Delay(seconds * 1000);
                
                var index = ActiveIdlingProcesses.Count == 1 ? 1 : random.Next(0, ActiveIdlingProcesses.Count+1);
                
                var toKillData = ActiveIdlingProcesses[index];
                toKillData.IdlingProcess.Kill();
                toKillData.IdlingProcess.Close();
                ActiveIdlingProcesses.RemoveAt(index);
                
                var startInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = true,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    Arguments = $"{toKillData.AppId} {Process.GetCurrentProcess().Id}"
                };

                var startProcess = Process.Start(startInfo);
                ActiveIdlingProcesses.Add(new IdlingAppData(startProcess, toKillData.AppId));
                
                Console.WriteLine($"Idling process for AppId {toKillData.AppId} has been restarted");
            }
        }

        private static List<IdlingAppData> ActiveIdlingProcesses { get; } = new List<IdlingAppData>();
    }

    public class IdlingAppData
    {
        public Process IdlingProcess { get; set; }
        public int AppId { get; set; }

        public IdlingAppData(Process _i, int _a)
        {
            IdlingProcess = _i;
            AppId = _a; 
        }
    }
}