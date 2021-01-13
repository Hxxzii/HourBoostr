// TODO: Actually handle the errors+error codes
// NOTE: This helper/background program is called with two arguments
// The first argument is the AppId of the game/app we want to idle
// The second argument is the process id of the "parent/master" exe
// This program closes itself whenever the process id of the "parent/master" exe is no longer a valid process (aka "parent/master" exe exited)
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Steam4NET;

namespace SingleBoostr.IdlingProcess
{
    public class Program
    {
        private static ISteamClient017 _steamClient;
        private static ISteamApps006 _steamApp;
        private static BackgroundWorker _bwg;

        private static ErrorCodes ConnectToSteam()
        {
            if (!Steamworks.Load(true))
            {
                return ErrorCodes.SteamworksFail;
            }

            _steamClient = Steamworks.CreateInterface<ISteamClient017>();
            if (_steamClient == null)
            {
                return ErrorCodes.ClientFail;
            }

            int pipe = _steamClient.CreateSteamPipe();
            if (pipe == 0)
            {
                return ErrorCodes.PipeFail;
            }

            int user = _steamClient.ConnectToGlobalUser(pipe);
            if (user == 0)
            {
                return ErrorCodes.UserFail;
            }

            _steamApp = _steamClient.GetISteamApps<ISteamApps006>(user, pipe);
            if (_steamApp == null)
            {
                return ErrorCodes.AppsFail;
            }

            return ErrorCodes.Success;
        }

        private static int Main(string[] args)
        {
            if (!uint.TryParse(args[0], out _))
            {
                var kill = Process.GetCurrentProcess();
                kill.Close();
                kill.Kill();
                kill.WaitForExit();
                return 1;
            }

            _bwg = new BackgroundWorker { WorkerSupportsCancellation = true };
            _bwg.DoWork += async (sender, e) =>
            {
                var processList = Process.GetProcesses();
                var parentProcess = processList.FirstOrDefault(o => o.Id == (int) e.Argument);

                if (parentProcess == null)
                {
                    await Task.Delay(10000);
                }
                else
                {
                    while (!_bwg.CancellationPending)
                    {
                        if (parentProcess.HasExited)
                            break;

                        await Task.Delay(10000);
                    }
                }

                Environment.Exit(1);
            };

            Environment.SetEnvironmentVariable("SteamAppId", args[0]);

            if (ConnectToSteam() == ErrorCodes.Success)
            {
                if (args.Length >= 2 && int.TryParse(args[1], out var parentProcessId) && parentProcessId != 0)
                    _bwg.RunWorkerAsync(parentProcessId);
                Process.GetCurrentProcess().WaitForExit();
                //await Task.Delay(-1);
            }

            return 0; 
        }
    }

    
    internal enum ErrorCodes : byte
    {
        Success = 0, 
        SteamworksFail = 1,
        ClientFail = 2,
        PipeFail = 3,
        UserFail = 4,
        AppsFail = 5
    }
}
