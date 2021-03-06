using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace D3_ThudLauncher
{
    internal static class Launcher
    {
        private const string BNet = "Blizzard Battle.net";
        private const string BNetKillProcess = "Battle.net";
        private const string D3ProcessName = "Diablo III64";
        private const string DefaultThudPath = @"D:\THUD\THUD.exe";
        private const string DefaultD3Path = @"D:\Program Files (x86)\Diablo III\Diablo III.exe";
        private const string DefaultAccountPw = "diablo";
        private const string DefaultAccountName = "diablo";
        private const int Limit = 10;
        
        // ReSharper disable once AssignNullToNotNullAttribute
        private static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        private static void Main(string[] args)
        {
            try
            {
                if (!IsElevated) throw new PrivilegeNotHeldException("You must run this as Admin");
                //Default path for my PC + allow anyone to cmd the sh!t out of these
                //Param 1 = THUD Path (including exe)
                //Param 2 = D3 Path (including exe)
                //Param 3 = User name to run Diablo
                //Param 4 = Password for the user that'll run Diablo
                var thudPath = args.Length > 0 ? args[0] : DefaultThudPath;
                var d3Path = args.Length > 1 ? args[1] : DefaultD3Path;
                var pw = args.Length > 3 ? args[3] : DefaultAccountPw;
                var spwD3 = new SecureString();
                foreach (var c in pw)
                {
                    spwD3.AppendChar(c);
                }

                var workingDirectory = new FileInfo(d3Path).DirectoryName;
                if (workingDirectory == null) throw new FileNotFoundException(d3Path);
                var d3StartInfo = new ProcessStartInfo
                {
                    UserName = args.Length > 2 ? args[2] : DefaultAccountName,
                    Password = spwD3,
                    FileName = d3Path,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    LoadUserProfile = true
                };

                //Make sure we won't try to launch something that does not exists
                if (!File.Exists(thudPath)) throw new FileNotFoundException(thudPath);
                if (!File.Exists(d3Path)) throw new FileNotFoundException(d3Path);

                //Starts Battle.Net
                var bnetHandle = StartBNet(d3StartInfo);
                //Starts Diablo 3 from Battle.Net (click at X,Y position on standard windows size)
                StartDiablo(bnetHandle);
                //Stops Battle.Net from being an arse
                KillProcess(BNetKillProcess);
                //Starts THUD
                StartThud(thudPath);
            }
            catch (Exception e)
            {
                WarnAndThrow(e);
            }
        }

        private static void StartThud(string thudPath)
        {
            //Give 5 second to start D3, even if it takes more like 10
            Thread.Sleep(5000);
            var workingDirectory = new FileInfo(thudPath).DirectoryName;
            if (workingDirectory == null) throw new FileNotFoundException(thudPath);

            var thudStartInfo = new ProcessStartInfo
            {
                FileName = thudPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                LoadUserProfile = true,
                Verb = "runas"
            };
            Process.Start(thudStartInfo);
        }

        private static IntPtr StartBNet(ProcessStartInfo d3StartInfo)
        {
            Process.Start(d3StartInfo);
            return BringToFront(BNet);
        }

        private static void StartDiablo(IntPtr handle)
        {
            var tries = 0;
            //Makes sure D3 starts
            while (Process.GetProcessesByName(D3ProcessName).Length < 1)
            {
                //Click Play at coords 290, 677 relatively to the Window's position
                ClickOnPointTool.ClickOnPoint(handle, new Point(290, 677));
                //Avoid overusing the CPU if we have to click alot
                Thread.Sleep(4000);
                ++tries;
                if (tries > Limit) throw new FileNotFoundException(D3ProcessName);
            }
        }

        private static void KillProcess(string processName)
        {
            //Get Battle.net process
            var bnetProcesses = Process.GetProcessesByName(processName);
            if (bnetProcesses.Length <= 0) throw new FileNotFoundException(processName);
            var bnetProcess = bnetProcesses[0];
            //Kill it
            bnetProcess.Kill();
        }

        private static void WarnAndThrow(Exception e)
        {
            MessageBox.Show(e.Message, e.GetType().Name);
            throw e;
        }

        private static IntPtr BringToFront(string title)
        {
            // Get a handle to the Calculator application.
            var handle = FindWindow(null, title);
            var tries = 0;

            //Wait until the app starts and can be shown
            while (handle == IntPtr.Zero)
            {
                //Sleeps one sec to not over-charge the CPU
                Thread.Sleep(1000);
                //Reset the handle
                handle = FindWindow(null, title);
                ++tries;
                if (tries > Limit) throw new FileNotFoundException(title);
            }
            
            // Make Calculator the foreground application
            SetForegroundWindow(handle);
            return handle;
        }

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("USER32.DLL")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
