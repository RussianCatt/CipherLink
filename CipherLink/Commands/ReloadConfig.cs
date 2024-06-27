using CipherLink.Plugins;
using CipherLink.Utils;
using System;
using System.IO;

namespace YourNamespace.Commands
{
    public class ReloadConfigPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";

        public string Name => "reloadconfig";

        public string Description => "**************************************************************\r\n*                    RELOADCONFIG Command                     *\r\n**************************************************************\r\n  The `reloadconfig` command reloads all configuration files \r\n  from the `Config` directory. This includes reloading files \r\n  such as `mysql.conf`, `accgen.conf`, etc.\r\n\r\n  Usage:\r\n  reloadconfig\r\n\r\n  Examples:\r\n  - Reload all configuration files:\r\n    ```\r\n    reloadconfig\r\n    ```\r\n";

        public ReloadConfigPlugin()
        {
            
        }

        public void Execute(string[] args)
        {
            ReloadAllConfigs();
            Console.WriteLine("Configuration files reloaded successfully.");
        }

        private void ReloadAllConfigs()
        {
            try
            {
                // Ensure the Config directory exists
                Directory.CreateDirectory(ConfigFolder);

                // Reload MySQL configuration
                string mysqlConfigPath = Path.Combine(ConfigFolder, "mysql.conf");
                if (File.Exists(mysqlConfigPath))
                {
                    ConfigLoader.LoadMysqlConfig(mysqlConfigPath);
                    Console.WriteLine("MySQL configuration reloaded.");
                }

                // Reload Accgen configuration
                string accgenConfigPath = Path.Combine(ConfigFolder, "accgen.conf");
                if (File.Exists(accgenConfigPath))
                {
                    ConfigLoader.LoadAccgenConfig(accgenConfigPath);
                    Console.WriteLine("Accgen configuration reloaded.");
                }

                // Add more configuration files as needed

                // Example for additional config file
                // string anotherConfigPath = Path.Combine(ConfigFolder, "another.conf");
                // if (File.Exists(anotherConfigPath))
                // {
                //     ConfigLoader.LoadAnotherConfig(anotherConfigPath);
                //     Console.WriteLine("Another configuration reloaded.");
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading configuration files: {ex.Message}");
            }
        }
    }
}
