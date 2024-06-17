using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CipherLink.Plugins;
namespace CipherLink.Commands
{
    public static class HelpCommand
    {
        public static void Run()
        {
            Console.WriteLine("Available Commands:");
            Console.WriteLine("help             Shows this help message");
            Console.WriteLine("exit             Exists the Console");
            List<IPlugin> plugins = PluginLoader.LoadPlugins();
            foreach (IPlugin plugin in plugins)
            {
                Console.WriteLine($" {plugin.Name} Plugin Command");
            }
        }
    }
}
