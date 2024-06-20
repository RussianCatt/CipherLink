using CipherLink.Commands;
using CipherLink.Plugins;
using System;
using System.Collections.Generic;

namespace CipherLink
{
    class Program
    {
        static void Main(string[] args)
        {
            List<IPlugin> plugins = PluginLoader.LoadPlugins();

            while (true)
            {
                Console.Title = "CipherLink 0.0.12-beta";
                Console.Write("CipherLink> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0].ToLower();
                string[] commandArgs = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                if (command == "exit")
                {
                    break;
                }
                else if (command == "help")
                {
                    HelpCommand.Run();
                }
                else
                {
                    var plugin = plugins.Find(p => p.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
                    if (plugin != null)
                    {
                        plugin.Execute(commandArgs);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown command: {command}. Use 'help' command to show available commands");


                    }
                }
            }
        }
    }
}