// TODO: Actually handle the errors+error codes
// NOTE: This helper/background program is called with two arguments
// The first argument is the AppId of the game/app we want to idle
// The second argument is the process id of the "parent/master" exe
// This program closes itself whenever the process id of the "parent/master" exe is no longer a valid process (aka "parent/master" exe exited)
// This program will insta-close if any arguments are invalid/nonexistent
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private static async Task Main(string[] args)
        {
            var parentProcessId = -1;
            if (args.Length == 0 || args[0] is null || args[1] is null || 
                !uint.TryParse(args[0], out _) || !int.TryParse(args[1], out parentProcessId) && parentProcessId > 0)
            {
                ErrorPopup("Insufficient/invalid arguments called - exiting\nIf you're trying to idle your games, open SingleBoostr.Client.exe instead");
            }

            _bwg = new BackgroundWorker { WorkerSupportsCancellation = true };
            _bwg.DoWork += async (sender, e) =>
            {
                var processList = Process.GetProcesses();
                var parentProcess = processList.FirstOrDefault(o => o.Id == (int) e.Argument);

                if (parentProcess == null)
                {
                    ErrorPopup("Invalid parent process ID\nPlease exit the client and create a GitHub issue if you get this error");
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

            Environment.SetEnvironmentVariable("SteamAppId", args[0]);

            var tryConnectSteam = ConnectToSteam();
            
            if (tryConnectSteam == ErrorCodes.Success)
            { 
                _bwg.RunWorkerAsync(parentProcessId);
                await Task.Delay(-1);
            }
            else
            {
                ErrorPopup($"{Enum.GetName(typeof(ErrorCodes), tryConnectSteam)} fatal error caused by appId: {args[0]}\nPlease exit the client and create a GitHub issue and describe what error you got");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ErrorPopup(string msg)
        {
            MessageBox.Show(msg, "SingleBoostr.IdlingProcess ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            GC.Collect(GC.MaxGeneration);
            Environment.Exit(1);
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
