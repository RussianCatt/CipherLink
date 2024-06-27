using System;
using System.IO;
using CipherLink.Utils;
using Newtonsoft.Json;

namespace CipherLink.Setup
{
    public class MySQLSetup : ISetupScript
    {
        private const string ConfigFolder = "Config";
        private const string MySQLConfigFile = "mysql.conf";
        private readonly string _mysqlConfigFilePath;

        public MySQLSetup()
        {
            _mysqlConfigFilePath = Path.Combine(ConfigFolder, MySQLConfigFile);
        }

        public string Name => "mysql";
        public string Description => "This script configures the MySQL connection settings.";
        public string Usage => @"
  - Interactive mode:
    setup mysql
  - Command-line mode:
    setup mysql <server> <database> <user> <password> <port>

  Examples:
  - Interactive mode:
    ```
    setup mysql
    ```
    This command will guide you through entering MySQL connection details interactively.

  - Command-line mode:
    ```
    setup mysql localhost cipherlink root password 3306
    ```
    This command will directly configure MySQL with the provided parameters.";

        public void ExecuteInteractive()
        {
            Console.WriteLine("Enter MySQL configuration:");
            Console.Write("Server: ");
            string server = Console.ReadLine();

            Console.Write("Database: ");
            string database = Console.ReadLine();

            Console.Write("User: ");
            string user = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Console.Write("Port: ");
            string port = Console.ReadLine();

            var config = new MysqlConfigData
            {
                Server = server,
                Database = database,
                User = user,
                Password = password,
                Port = port
            };

            SaveConfig(config);
            Console.WriteLine("MySQL configuration saved.");
        }

        public void ExecuteCommand(string[] args)
        {
            var config = new MysqlConfigData
            {
                Server = args[1],
                Database = args[2],
                User = args[3],
                Password = args[4],
                Port = args[5]
            };

            SaveConfig(config);
            Console.WriteLine("MySQL configuration saved.");
        }

        private void SaveConfig(MysqlConfigData config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            Directory.CreateDirectory(ConfigFolder);
            File.WriteAllText(_mysqlConfigFilePath, json);
        }
    }
}
