using CipherLink.Plugins;
using CipherLink.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AccountGenerator.Commands
{
    public class PullDbPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";

        private readonly MysqlConfigData mysqlConfig;

        public string Name => "pulldb";
        public string Description => "**************************************************************\n*                    PULLDB Command                          *\n**************************************************************\n  The `pulldb` command retrieves data from a remote database\n  and saves it locally as `pulled_accounts.json` in the `Files`\n  directory.\n\n  Usage:\n  pulldb [-filter <platform> <accountType> <value>]\n\n  Parameters:\n  - `-filter`: Optional parameter to filter data based\n    on specific criteria:\n      - `<platform>`: Platform name\n      - `<accountType>`: Type of account\n      - `<value>`: Additional identifier\n\n  Examples:\n  1. Pull all accounts:\n     ```\n     pulldb\n     ```\n\n  2. Pull filtered accounts:\n     ```\n     pulldb -filter social_media free 2024\n     ```\n\n  Notes:\n  - Ensure MySQL configuration (`mysql.conf`) is set\n    correctly.\n  - Data is stored in `pulled_accounts.json` for local\n    use.\n";
        public PullDbPlugin()
        {
            mysqlConfig = ConfigLoader.LoadMysqlConfig(Path.Combine(ConfigFolder, "mysql.conf"));

            if (mysqlConfig == null)
            {
                Console.WriteLine("Configuration error: Ensure the mysql.conf configuration file is set correctly.");
                return;
            }
        }

        public void Execute(string[] args)
        {
            DatabaseHandler.PullDataFromDatabase(mysqlConfig, args).Wait();
        }
    }
}
