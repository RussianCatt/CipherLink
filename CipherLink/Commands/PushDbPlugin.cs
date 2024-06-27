using CipherLink.Plugins;
using CipherLink.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AccountGenerator.Commands
{
    public class PushDbPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";
        private const string AccountsFileName = "accounts.json";
        private const string FilesFolder = "Files";

        private readonly MysqlConfigData mysqlConfig;

        public string Name => "pushdb";
        public string Description => "**************************************************************\r\n*                    PUSHDB Command                          *\r\n**************************************************************\r\n  The `pushdb` command pushes local account data from \r\n  `accounts.json` to a remote MySQL database specified \r\n  in `mysql.conf`.\r\n\r\n  Usage:\r\n  pushdb [-filter <platform> <accountType> <value>]\r\n\r\n  Parameters:\r\n  - `-filter`: Optional parameter to filter the data pushed \r\n    to the database based on specific criteria:\r\n    - `<platform>`: Platform name (e.g., `social_media`, `gaming`).\r\n    - `<accountType>`: Type of account (e.g., `free`, `premium`).\r\n    - `<value>`: Additional value or identifier associated \r\n      with the accounts.\r\n\r\n  Examples:\r\n  1. Push all accounts to the database:\r\n     ```\r\n     pushdb\r\n     ```\r\n\r\n  2. Push accounts filtered by platform and account type:\r\n     ```\r\n     pushdb -filter social_media free 2024\r\n     ```\r\n\r\n  Notes:\r\n  - Ensure MySQL configuration (`mysql.conf`) is correctly set \r\n    up before executing this command.\r\n  - The command loads account data from `accounts.json` located \r\n    in the `Files` directory for pushing to the database.\r\n";

        public PushDbPlugin()
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
            var accounts = LoadAccounts();
            if (args.Length >= 2 && args[0].ToLower() == "-filter")
            {
                var platform = args[1];
                var accountType = args.Length > 2 ? args[2] : "";
                var value = args.Length > 3 ? args[3] : "";

                // Filter accounts
                accounts = FilterAccounts(accounts, platform, accountType, value);
            }

            DatabaseHandler.PushDataToDatabase(mysqlConfig, accounts, args).Wait();
        }

        private List<Account> LoadAccounts()
        {
            return ConfigLoader.LoadAccounts(Path.Combine(FilesFolder, AccountsFileName));
        }

        private List<Account> FilterAccounts(List<Account> accounts, string platform, string accountType, string value)
        {
            return accounts.Where(acc =>
                (string.IsNullOrEmpty(platform) || acc.Platform.ToLower() == platform.ToLower()) &&
                (string.IsNullOrEmpty(accountType) || acc.AccountType.ToLower() == accountType.ToLower()) &&
                (string.IsNullOrEmpty(value) || acc.Value.ToLower() == value.ToLower()))
                .ToList();
        }
    }
}
