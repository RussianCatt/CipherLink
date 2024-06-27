using CipherLink.Plugins;
using CipherLink.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AccountGenerator.Commands
{
    public class GenerateAccountsPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";
        private const string AccountsFileName = "accounts.json";
        private const string FilesFolder = "Files";

        private readonly AccgenConfigData accgenConfig;
        private readonly MysqlConfigData mysqlConfig;
        private static readonly Random Random = new Random();

        public string Name => "accgen";
        public string Description => "**************************************************************\n*                    Account Generator Command                          *\n**************************************************************\n  The `accgen` command generates and saves account information \n  locally as JSON files and optionally pushes the data to a \n  remote MySQL database specified in `mysql.conf`.\n\n  Usage:\n  accgen <number> <platform> <service|game> <value> [-dbpush]\n\n  Parameters:\n  - `<number>`: Number of accounts to generate (positive integer).\n  - `<platform>`: Platform name where accounts are generated.\n  - `<service|game>`: Type of service or game associated with \n                     the accounts.\n  - `<value>`: Additional value or identifier associated with \n               the accounts.\n  - `-dbpush`: Optional flag to push generated accounts to the \n               database.\n\n  Examples:\n  1. Generate accounts without pushing to the database:\n     ```\n     accgen 10 social_media free 2024\n     ```\n\n  2. Generate accounts and push to the database:\n     ```\n     accgen 5 gaming premium 2023 -dbpush\n     ```\n\n  Notes:\n  - Ensure all configurations in `accgen.conf` and `mysql.conf` \n    are correctly set up before executing this command.\n  - Generated accounts are saved locally as `accounts.json` \n    under the `Files` directory.\n";
        public GenerateAccountsPlugin()
        {
            accgenConfig = ConfigLoader.LoadAccgenConfig(Path.Combine(ConfigFolder, "accgen.conf"));
            mysqlConfig = ConfigLoader.LoadMysqlConfig(Path.Combine(ConfigFolder, "mysql.conf"));

            if (accgenConfig == null || mysqlConfig == null)
            {
                Console.WriteLine("Configuration error: Ensure all required configuration values are set correctly.");
                return;
            }
        }

        public void Execute(string[] args)
        {
            if (args.Length == 1 && args[0].ToLower() == "-?")
            {
                PrintHelp();
                return;
            }

            if (accgenConfig.EmailDomain == "emailhere.com")
            {
                Console.WriteLine("Please set a valid email domain in the accgen.conf file.");
                return;
            }

            if (args.Length < 4)
            {
                PrintHelp();
                return;
            }

            if (!int.TryParse(args[0], out var numberOfAccounts) || numberOfAccounts <= 0)
            {
                Console.WriteLine("Error: <number> must be a positive integer.");
                return;
            }

            var platform = args[1];
            var accountType = args[2];
            var value = args[3];
            var dbPush = args.Length == 5 && args[4].ToLower() == "-dbpush";

            if (!accgenConfig.Platforms.Contains(platform))
            {
                Console.WriteLine($"Error: Invalid platform. Valid platforms are: {string.Join(", ", accgenConfig.Platforms)}");
                return;
            }

            var accounts = GenerateAccounts(accgenConfig, numberOfAccounts, platform, accountType, value);

            SaveAccounts(accounts).Wait();

            if (dbPush)
            {
                DatabaseHandler.SaveToDatabase(mysqlConfig, accounts).Wait();
            }
        }

        private void PrintHelp()
        {
            Console.WriteLine("Usage: accgen <number> <platform> <service|game> <value> [-dbpush]");
            Console.WriteLine($"Platforms: {string.Join(", ", accgenConfig.Platforms)}");
        }
        
        private async Task SaveAccounts(List<Account> accounts)
        {
            // Ensure the Files directory exists
            Directory.CreateDirectory(FilesFolder);

            var accountsFilePath = Path.Combine(FilesFolder, AccountsFileName);

            // Ensure the accounts file exists
            if (!File.Exists(accountsFilePath))
            {
                File.Create(accountsFilePath).Dispose(); // Dispose is called to release the file handle
            }

            await ConfigLoader.SaveAccounts(accounts, accountsFilePath);
        }

        private List<Account> GenerateAccounts(AccgenConfigData accgenConfig, int numberOfAccounts, string platform, string accountType, string value)
        {
            var accounts = new List<Account>();

            for (var i = 0; i < numberOfAccounts; i++)
            {
                var username = GenerateUsername();
                var password = GeneratePassword();
                var email = $"{username}@{accgenConfig.EmailDomain}";

                accounts.Add(new Account
                {
                    Username = username,
                    Password = password,
                    Email = email,
                    Platform = platform,
                    AccountType = accountType,
                    Status = "available",
                    Value = value
                });
            }

            return accounts;
        }

        private string GenerateUsername()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private string GeneratePassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
