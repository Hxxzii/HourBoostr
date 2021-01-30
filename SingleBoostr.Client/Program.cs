using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            SetConsoleTextColor(ConsoleColor.Red);

            if (!File.Exists(exe))
            {
                Console.WriteLine("ERROR: Helper exe not located (SingleBoostr.IdlingProcess.exe)");
                Console.WriteLine("Please put this program beside the helper exe or reinstall this program");
                await Task.Delay(-1);
                return 1;
            }

            var config = new ConfigHandler();
            
            config.InitializeData();

            if (bool.TryParse(config.GetValue("InputAppIdsDuringRuntime"), out var inputDuringRuntime) 
                && !inputDuringRuntime && !File.Exists(applist))
            {
                File.CreateText(applist).Dispose();
                Console.WriteLine("ERROR: App list doesn't exist (applist.txt)");
                Console.WriteLine("App list file has been created, please add your appids that you want to idle in the config file");
                Console.WriteLine("(exit this program, then edit the config file)");
                Console.WriteLine("(OR in config.ini, set InputAppIdsDuringRuntime = true and you can input your appIds via input during runtime)");
                await Task.Delay(-1);
                return 1;
            }
            
            SetConsoleTextColor(ConsoleColor.Yellow);
            
            if (int.TryParse(config.GetValue("SecondsUntilRestart"), out var seconds))
            {
                if (seconds <= 0)
                { 
                    Console.WriteLine("WARNING: SecondsUntilRestart is zero or a negative number - defaulting to 3600 seconds (1 hour)");
                    seconds = 3600;
                }
            }
            else
            {
                Console.WriteLine("WARNING: SecondsUntilRestart value is not a number - defaulting to 3600 seconds (1 hour)");
                seconds = 3600;
            }

            SetConsoleTextColor(ConsoleColor.White);
            Console.WriteLine($"Idling processes will restart every {seconds} seconds");

            var listOfApps = new List<string>();
            
            if (inputDuringRuntime)
            {
                SetConsoleTextColor(ConsoleColor.Cyan);
                Console.WriteLine();
                Console.WriteLine("InputAppIdsDuringRuntime is set to true - ignoring applist.txt and receiving appIds via input now");
                Console.WriteLine("Please input one appId at a time, then press enter");
                Console.WriteLine("Do this for each individual appId that you want to input");
                Console.WriteLine("When you are done inputting appIds and you're ready to start idling, input anything that isn't a number (ex: done)");

                SetConsoleTextColor(ConsoleColor.Green);

                var whitespaceRegex = new Regex(@"\s+");

                while (true)
                {
                    var inputtedAppId = whitespaceRegex.Replace(Console.ReadLine() ?? string.Empty, string.Empty);
                    if (string.IsNullOrWhiteSpace(inputtedAppId) || !inputtedAppId.All(char.IsDigit))
                    {
                        SetConsoleTextColor(ConsoleColor.White);
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

            // i is the AppId
            foreach (var i in listOfApps)
            {
                if (!uint.TryParse(i, out var id) || string.IsNullOrWhiteSpace(i))
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

                ActiveIdlingApps.Add(i, Process.Start(startInfo));

                var justAdded = ActiveIdlingApps.Last().Value;
                await Task.Delay(256);

                ErrorCodes exitCode;
                try
                {
                    exitCode = (ErrorCodes) justAdded.ExitCode;
                }
                // An InvalidOperationException will be raised if we try accessing the exit code of a process which hasn't exited
                // Process hasn't exited yet within the 256ms window, meaning it hasn't crashed/raised an exception which means success
                catch (InvalidOperationException) 
                {
                    exitCode = ErrorCodes.Success;
                }

                SetConsoleTextColor(ConsoleColor.DarkRed);
                switch (exitCode)
                {
                    case ErrorCodes.AppsFail:
                    case ErrorCodes.ClientFail:
                    case ErrorCodes.InvalidArguments:
                    case ErrorCodes.InvalidParentProcessId:
                        Console.WriteLine($"FATAL ERROR: Trying to idle appId {i} returned error {Enum.GetName(typeof(ErrorCodes), exitCode)}");
                        Console.WriteLine("(exit the app and please make a GitHub issue with the specified error if you get this error)");
                        await Task.Delay(-1);
                        break;
                    case ErrorCodes.UserFail:
                        Console.WriteLine($"FATAL ERROR: you tried to idle appId {i}, which you apparently don't own (?)");
                        Console.WriteLine("(please exit the app & try again with only appIds that you actually own)");
                        await Task.Delay(-1);
                        break;
                    case ErrorCodes.SteamworksFail:
                    case ErrorCodes.PipeFail:
                        Console.WriteLine("FATAL ERROR: you tried to idle without the Steam client open - the Steam client must be open");
                        Console.WriteLine("(please exit the app & try again with the Steam client open)");
                        await Task.Delay(-1);
                        break;
                    case ErrorCodes.Success:
                    default:
                        break;
                }
                SetConsoleTextColor(ConsoleColor.Green);
                Console.WriteLine($"AppId {i} is now boosting!");
            }

            SetConsoleTextColor(ConsoleColor.Yellow);
            while (true)
            {
                await Task.Delay(seconds * 1000);
                
                var toRestart = ActiveIdlingApps.ElementAt(new Random().Next(ActiveIdlingApps.Count));

                toRestart.Value.Kill();
                toRestart.Value.WaitForExit();
                toRestart.Value.Start();

                Console.WriteLine($"Idling process for AppId {toRestart.Key} has been restarted");
            }
        }

        internal static void SetConsoleTextColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        // Key: AppId, Value: Process
        private static readonly Dictionary<string, Process> ActiveIdlingApps = new Dictionary<string, Process>();
    }
}
