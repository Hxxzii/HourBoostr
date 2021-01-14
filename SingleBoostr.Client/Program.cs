using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SingleBoostr.Client
{
    public class Program
    {
        private static async Task<int> Main()
        {
            var applist = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applist.txt");
            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SingleBoostr.IdlingProcess.exe");

            if (!File.Exists(exe))
            {
                SetConsoleTextColor(ConsoleColor.Red);
                Console.WriteLine("ERROR: Helper exe not located (SingleBoostr.IdlingProcess.exe)");
                Console.WriteLine("Please put this program beside the helper exe or reinstall this program");
                await Task.Delay(-1);
                return 1;
            }

            ConfigHandler config = new ConfigHandler();
            
            if (!config.ConfigExistsAndValid)
            {
                SetConsoleTextColor(ConsoleColor.Red);
                File.CreateText(ConfigHandler.ConfigPath).Dispose();
                Console.WriteLine("ERROR: Config doesn't exist (config.ini), or it is empty");
                Console.WriteLine("Config file has been created/regenerated, please adjust accordingly");
                
                config.FreshConfig();
                
                Console.WriteLine("(exit this program, then edit the config file)");
                await Task.Delay(-1);
                return 1;
            }

            config.InitializeData();

            if (bool.TryParse(config.GetValue("InputAppIdsDuringRuntime"), out var inputDuringRuntime) && !inputDuringRuntime && !File.Exists(applist))
            {
                SetConsoleTextColor(ConsoleColor.Red);
                File.CreateText(applist).Dispose();
                Console.WriteLine("ERROR: App list doesn't exist (applist.txt)");
                Console.WriteLine("App list file has been created, please add your appids that you want to idle in the config file");
                Console.WriteLine("(exit this program, then edit the config file)");
                Console.WriteLine("(OR in config.ini, set InputAppIdsDuringRuntime = true and you can input your appIds via input during runtime)");
                await Task.Delay(-1);
                return 1;
            }

            if (int.TryParse(config.GetValue("SecondsUntilRestart"), out var seconds))
            {
                if (seconds <= 0)
                {
                    SetConsoleTextColor(ConsoleColor.Yellow);
                    Console.WriteLine("WARNING: SecondsUntilRestart is zero or a negative number - defaulting to 3600 seconds (1 hour)");
                    seconds = 3600;
                }
                
                SetConsoleTextColor(ConsoleColor.White);
                Console.WriteLine($"Idling processes will restart every {seconds} seconds");
            }
            else
            {
                SetConsoleTextColor(ConsoleColor.Yellow);
                Console.WriteLine("WARNING: SecondsUntilRestart value is not a number - defaulting to 3600 seconds (1 hour)");
                seconds = 3600;
            }

            var listOfApps = new List<string>();
            SetConsoleTextColor(ConsoleColor.White);
            
            if (inputDuringRuntime)
            {
                SetConsoleTextColor(ConsoleColor.Cyan);
                Console.WriteLine();
                Console.WriteLine("InputAppIdsDuringRuntime is set to true - ignoring applist.txt and receiving appIds via input now");
                Console.WriteLine("Please input one appId at a time, then press enter");
                Console.WriteLine("Do this for each individual appId that you want to input");
                Console.WriteLine("When you are done inputting appIds and you're ready to start idling, input anything that isn't a number (ex: done)");
                Console.WriteLine("Small note: All whitespaces/spaces will be removed from any inputted string");

                SetConsoleTextColor(ConsoleColor.Green);
                
                while (true)
                {
                    var inputtedAppId = RemoveAllWhitespace(Console.ReadLine());
                    if (string.IsNullOrWhiteSpace(inputtedAppId) || !inputtedAppId.All(char.IsDigit))
                    {
                        Console.WriteLine("Detected non-numeric input, proceeding to idle games now");
                        break;
                    }

                    listOfApps.Add(inputtedAppId);
                    Console.WriteLine($"AppId {inputtedAppId} successfully added");
                }

                if (!listOfApps.Any())
                {
                    SetConsoleTextColor(ConsoleColor.Red);
                    Console.WriteLine("ERROR: you inputted no valid appIds");
                    Console.WriteLine("(please restart the app and input valid appIds)");
                    Console.WriteLine("(OR in config.ini, set InputAppIdsDuringRuntime = false and put your appIds in applist.txt, one appId per line)");
                    await Task.Delay(-1);
                }
            }
            else
            {
                listOfApps = File.ReadAllLines(applist).ToList();
                if (!listOfApps.Any())
                {
                    SetConsoleTextColor(ConsoleColor.Red);
                    Console.WriteLine("ERROR: applist.txt is empty - therefore no apps will get idled");
                    Console.WriteLine("(please exit the app and edit your applist.txt file to contain appids)");
                    Console.WriteLine("(OR in config.ini, set InputAppIdsDuringRuntime = true and you can input your appIds via input during runtime)");
                    await Task.Delay(-1);
                }
            }

            if (listOfApps.Count > 33)
            {
                SetConsoleTextColor(ConsoleColor.Yellow);
                Console.WriteLine("WARNING: More than 33 appIds detected");
                Console.WriteLine("Trying to idle more than 33 appIds may result in Steam not idling some of your apps at all!");
            }
            
            foreach (var i in listOfApps)
            {
                if (!int.TryParse(i, out _) || string.IsNullOrWhiteSpace(i))
                {
                    SetConsoleTextColor(ConsoleColor.Yellow);
                    Console.WriteLine($"WARNING: AppId {i} is an invalid AppId, skipping");
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
                SetConsoleTextColor(ConsoleColor.Green);
                Console.WriteLine($"AppId {i} is now boosting!");
            }

            var random = new Random();

            while (true)
            {
                await Task.Delay(seconds * 1000);
                
                var index = random.Next(ActiveIdlingProcesses.Count);
                var appid = ActiveIdlingProcesses[index].AppId;

                ActiveIdlingProcesses[index].IdlingProcess.Kill();
                ActiveIdlingProcesses[index].IdlingProcess.Dispose();
                ActiveIdlingProcesses.RemoveAt(index);
                
                var startInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = true,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    Arguments = $"{appid} {Process.GetCurrentProcess().Id}"
                };

                var startProcess = Process.Start(startInfo);
                ActiveIdlingProcesses.Add(new IdlingAppData(startProcess, appid));
                
                Console.WriteLine($"Idling process for AppId {appid} has been restarted");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string RemoveAllWhitespace(string str)
        {
            return RegexInst.Replace(str, string.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetConsoleTextColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }
        private static Regex RegexInst { get; } = new Regex(@"\s+");
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
