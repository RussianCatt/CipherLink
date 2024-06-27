using System;
using System.Collections.Generic;
using CipherLink.Plugins;

namespace CipherLink.Commands
{
    public static class HelpCommand
    {
        public static void Run()
        {
            Console.WriteLine();

            // Static commands
            PrintPluginInfo("**************************************************************\n*                      HELP Command                          *\n**************************************************************\n  The `help` command displays information about available \n  commands and plugins within the application console.\n\n  Usage:\n  help\n\n  Examples:\n  1. Display available commands and plugin information:\n     ```\n     help\n     ```\n\n  Notes:\n  - This command provides an overview of all available \n    commands and plugins loaded in the application.\n  - Use `help` to get assistance on how to use specific \n    commands and plugins.\n");
            PrintPluginInfo("**************************************************************\n*                     EXIT Command                           *\n**************************************************************\n  The `exit` command terminates the application console \n  session.\n\n  Usage:\n  exit\n\n  Examples:\n  1. Exit the application console:\n     ```\n     exit\n     ```\n\n  Notes:\n  - Ensure all unsaved data and processes are handled before \n    executing the `exit` command.\n  - This command is used to gracefully terminate the \n    application without any further actions.\n");

            // Dynamic commands (plugins)
            List<IPlugin> plugins = PluginLoader.LoadPlugins();
            foreach (IPlugin plugin in plugins)
            {
                PrintPluginInfo(plugin.Description);
            }
        }

        private static void PrintCommand(string command)
        {
            Console.WriteLine($"   {command}");
        }

        private static void PrintPluginInfo(string description)
        {
            Console.WriteLine($"{description}");
        }
    }
}
