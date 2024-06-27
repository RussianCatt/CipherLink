using CipherLink.Plugins;
using System;
using System.Diagnostics;
using System.Reflection;

namespace CipherLink.Commands
{
    public class ReloadProgramPlugin : IPlugin
    {
        public string Name => "reload";

        public string Description => "**************************************************************\r\n*                    RELOAD Command                    *\r\n**************************************************************\r\n  The `reload` command restarts the entire application \r\n  by launching a new instance and terminating the current one.\r\n\r\n  Usage:\r\n  reload\r\n\r\n  Examples:\r\n  - Restart the application:\r\n    ```\r\n    reload\r\n    ```\r\n";

        public ReloadProgramPlugin()
        {
            
        }

        public void Execute(string[] args)
        {
            ReloadProgram();
        }

        private void ReloadProgram()
        {
            try
            {
                // Get current process information
                var currentProcess = Process.GetCurrentProcess();

                // Start a new instance of the application
                var startInfo = new ProcessStartInfo
                {
                    FileName = Assembly.GetEntryAssembly().Location,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);

                // Close the current process
                currentProcess.CloseMainWindow();
                currentProcess.Kill(); // Force kill if not terminated properly
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading program: {ex.Message}");
            }
        }
    }
}
