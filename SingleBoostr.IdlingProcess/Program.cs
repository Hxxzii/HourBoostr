// TODO: Find out how to replicate every error code 
// NOTE: This helper/background program is called with two arguments
// The first argument is the AppId of the game/app we want to idle
// The second argument is the process id of the "parent/master" exe
// This program closes itself whenever the process id of the "parent/master" exe is no longer a valid process (aka "parent/master" exe exited)
// This program will insta-close if any arguments are invalid/nonexistent
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

            var pipe = _steamClient.CreateSteamPipe();
            if (pipe == 0)
            {
                return ErrorCodes.PipeFail;
            }

            var user = _steamClient.ConnectToGlobalUser(pipe);
            if (user == 0)
            {
                return ErrorCodes.UserFail;
            }

            _steamApp = _steamClient.GetISteamApps<ISteamApps006>(user, pipe);
            return _steamApp == null ? ErrorCodes.AppsFail : ErrorCodes.Success;
        }

        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args[0] is null || args[1] is null || 
                !uint.TryParse(args[0], out _) || !int.TryParse(args[1], out var parentProcessId) && parentProcessId >= 0)
            {
                return (byte) ErrorCodes.InvalidArguments;
            }

            var appId = args[0];

            _bwg = new BackgroundWorker { WorkerSupportsCancellation = true };
            _bwg.DoWork += async (sender, e) =>
            {
                var processList = Process.GetProcesses();
                var parentProcess = processList.FirstOrDefault(o => o.Id == (int) e.Argument);

                if (parentProcess == null)
                {
                    Environment.Exit((byte) ErrorCodes.InvalidParentProcessId); 
                }
                else
                {
                    while (!_bwg.CancellationPending)
                    {
                        await Task.Delay(10000);

                        if (!parentProcess.HasExited) continue;
                        
                        _bwg.CancelAsync();
                        Environment.Exit(0);
                    }
                }
            };

            Environment.SetEnvironmentVariable("SteamAppId", appId);

            var tryConnectSteam = ConnectToSteam();
            
            if (tryConnectSteam == ErrorCodes.Success)
            { 
                _bwg.RunWorkerAsync(parentProcessId);
                await Task.Delay(-1);
            }
            else
            {
                return (byte) tryConnectSteam;
            }

            return 0;
        }
    }
}
