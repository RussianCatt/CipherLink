using CipherLink.Plugins;
using System;

namespace CipherLink.Commands
{
    public class ClearConsolePlugin : IPlugin
    {
        public string Name => "clear";

        public string Description => "**************************************************************\r\n*                    CLEAR Command                     *\r\n**************************************************************\r\n  The `clear` command clears the console window \r\n  by removing all existing output.\r\n\r\n  Usage:\r\n  clear\r\n\r\n  Examples:\r\n  - Clear the console:\r\n    ```\r\n    clear\r\n    ```\r\n";

        public ClearConsolePlugin()
        {
           
        }

        public void Execute(string[] args)
        {
            Console.Clear();
        }
    }
}
