using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using CipherLink.Plugins;
using System.Data;

namespace AccountGenerator.Commands
{
    public class GenerateAccountsPlugin : IPlugin
    {
        private const string ConfigFolder = "Config";
        private const string AccgenConfigFileName = "accgen.conf";
        private const string MysqlConfigFileName = "mysql.conf";
        private const string AccountsFileName = "accounts.json";
        private const string PulledAccountsFileName = "pulled_accounts.json";
        private const string FilesFolder = "Files";

        private string emailDomain = string.Empty;
        private List<string> platforms = new List<string>();
        private string server = string.Empty;
        private string database = string.Empty;
        private string user = string.Empty;
        private string password = string.Empty;
        private string port = string.Empty;

        private static readonly Random Random = new Random();

        public string Name => "accgen";

        public GenerateAccountsPlugin()
        {
            if (!LoadAccgenConfig() || !LoadMysqlConfig())
            {
                Console.WriteLine("Configuration error: Ensure all required configuration values are set correctly.");
                return;
            }
        }

        public void Execute(string[] args)
        {
            if (args.Length == 1 && args[0].ToLower() == "help")
            {
                Console.WriteLine("Usage: accgen <number> <platform> <service|game> <value> [-dbpush]");
                Console.WriteLine($"Platforms: {string.Join(", ", platforms)}");
                Console.WriteLine("Other commands:");
                Console.WriteLine("PushDB [-filter <platform> <accountType> <value>] - Pushes the data from accounts.json to the database.");
                Console.WriteLine("PullDB [-filter <platform> <accountType> <value>] - Pulls the data from the database to pulled_accounts.json.");
                return;
            }

            if (emailDomain == "emailhere.com")
            {
                Console.WriteLine("Please set a valid email domain in the accgen.conf file.");
                return;
            }

            if (args.Length >= 1 && args[0].ToLower() == "pushdb")
            {
                string? filterPlatform = null;
                string? filterAccountType = null;
                string? filterValue = null;

                if (args.Length == 5 && args[1].ToLower() == "-filter")
                {
                    filterPlatform = args[2];
                    filterAccountType = args[3];
                    filterValue = args[4];
                }

                Task.Run(async () => await PushDataToDatabase(filterPlatform, filterAccountType, filterValue)).GetAwaiter().GetResult();
                return;
            }

            if (args.Length >= 1 && args[0].ToLower() == "pulldb")
            {
                string? filterPlatform = null;
                string? filterAccountType = null;
                string? filterValue = null;

                if (args.Length == 5 && args[1].ToLower() == "-filter")
                {
                    filterPlatform = args[2];
                    filterAccountType = args[3];
                    filterValue = args[4];
                }

                Task.Run(async () => await PullDataFromDatabase(filterPlatform, filterAccountType, filterValue)).GetAwaiter().GetResult();
                return;
            }

            if (args.Length < 4)
            {
                Console.WriteLine("Usage: accgen <number> <platform> <service|game> <value> [-dbpush]");
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

            if (!platforms.Contains(platform))
            {
                Console.WriteLine($"Error: Invalid platform. Valid platforms are: {string.Join(", ", platforms)}");
                return;
            }

            var accounts = GenerateAccounts(numberOfAccounts, platform, accountType, value);

            Task.Run(async () => await SaveAccounts(accounts)).GetAwaiter().GetResult();

            if (dbPush)
            {
                Task.Run(async () => await SaveToDatabase(accounts)).GetAwaiter().GetResult();
            }
        }

        private List<Account> GenerateAccounts(int number, string platform, string accountType, string value)
        {
            var accounts = new List<Account>();

            for (int i = 0; i < number; i++)
            {
                var account = new Account
                {
                    Username = GenerateRandomUsername(),
                    Password = GenerateRandomPassword(),
                    Email = $"{GenerateRandomUsername()}@{emailDomain}",
                    Platform = platform,
                    AccountType = accountType,
                    Value = value
                };

                accounts.Add(account);
            }

            return accounts;
        }

        private bool LoadAccgenConfig()
        {
            try
            {
                var configPath = Path.Combine(ConfigFolder, AccgenConfigFileName);

                if (!File.Exists(configPath))
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    WriteDefaultAccgenConfig(configPath);
                }

                var json = File.ReadAllText(configPath);
                var configData = JsonSerializer.Deserialize<AccgenConfigData>(json);

                if (configData != null)
                {
                    emailDomain = configData.EmailDomain ?? string.Empty;
                    platforms = configData.Platforms ?? new List<string>();
                    return true;
                }
                else
                {
                    Console.WriteLine("Error: Failed to deserialize accgen config file.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accgen config file: {ex.Message}");
                return false;
            }
        }

        private bool LoadMysqlConfig()
        {
            try
            {
                var configPath = Path.Combine(ConfigFolder, MysqlConfigFileName);

                if (!File.Exists(configPath))
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    WriteDefaultMysqlConfig(configPath);
                }

                var json = File.ReadAllText(configPath);
                var configData = JsonSerializer.Deserialize<MysqlConfigData>(json);

                if (configData != null)
                {
                    server = configData.Server ?? string.Empty;
                    database = configData.Database ?? string.Empty;
                    user = configData.User ?? string.Empty;
                    password = configData.Password ?? string.Empty;
                    port = configData.Port ?? string.Empty;
                    return true;
                }
                else
                {
                    Console.WriteLine("Error: Failed to deserialize mysql config file.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading mysql config file: {ex.Message}");
                return false;
            }
        }

        private void WriteDefaultAccgenConfig(string path)
        {
            var defaultConfig = new AccgenConfigData();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(path, json);
        }

        private void WriteDefaultMysqlConfig(string path)
        {
            var defaultConfig = new MysqlConfigData();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(path, json);
        }

        private List<Account> LoadAccounts()
        {
            try
            {
                var accountsFilePath = Path.Combine(FilesFolder, AccountsFileName);

                if (!File.Exists(accountsFilePath))
                {
                    if (!Directory.Exists(FilesFolder))
                    {
                        Directory.CreateDirectory(FilesFolder);
                    }

                    File.WriteAllText(accountsFilePath, "[]");
                }

                var json = File.ReadAllText(accountsFilePath);
                var accounts = JsonSerializer.Deserialize<List<Account>>(json);
                return accounts ?? new List<Account>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accounts: {ex.Message}");
                return new List<Account>();
            }
        }

        private async Task SaveAccounts(List<Account> accounts)
        {
            var accountsFilePath = Path.Combine(FilesFolder, AccountsFileName);

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(accounts, options);
                await File.WriteAllTextAsync(accountsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts: {ex.Message}");
            }
        }

        private async Task SaveToDatabase(List<Account> accounts)
        {
            string connectionStringWithoutDb = $"Server={server};User={user};Password={password};Port={port};";
            using (var connection = new MySqlConnection(connectionStringWithoutDb))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database server connection established.");

                    // Create database if it doesn't exist
                    var createDbCommandText = $"CREATE DATABASE IF NOT EXISTS `{database}`;";
                    using (var createDbCommand = new MySqlCommand(createDbCommandText, connection))
                    {
                        await createDbCommand.ExecuteNonQueryAsync();
                    }

                    // Create table if it doesn't exist
                    string connectionStringWithDb = GetConnectionString();
                    using (var connectionWithDb = new MySqlConnection(connectionStringWithDb))
                    {
                        await connectionWithDb.OpenAsync();

                        var createTableCommandText = @"
                            CREATE TABLE IF NOT EXISTS `Accounts` (
                                `Id` INT NOT NULL AUTO_INCREMENT,
                                `Username` VARCHAR(255) NOT NULL,
                                `Password` VARCHAR(255) NOT NULL,
                                `Email` VARCHAR(255) NOT NULL,
                                `Platform` VARCHAR(255) NOT NULL,
                                `AccountType` VARCHAR(255) NOT NULL,
                                `Status` VARCHAR(255) NOT NULL,
                                `Value` VARCHAR(255) NOT NULL,
                                `OnDatabase` VARCHAR(255) NOT NULL,
                                PRIMARY KEY (`Id`)
                            );";
                        using (var createTableCommand = new MySqlCommand(createTableCommandText, connectionWithDb))
                        {
                            await createTableCommand.ExecuteNonQueryAsync();
                        }

                        foreach (var account in accounts.Where(a => a.OnDatabase == "NO"))
                        {
                            var commandText = "INSERT INTO Accounts (Username, Password, Email, Platform, AccountType, Status, Value, OnDatabase) " +
                                              "VALUES (@Username, @Password, @Email, @Platform, @AccountType, @Status, @Value, @OnDatabase)";

                            using (var command = new MySqlCommand(commandText, connectionWithDb))
                            {
                                command.Parameters.AddWithValue("@Username", account.Username);
                                command.Parameters.AddWithValue("@Password", account.Password);
                                command.Parameters.AddWithValue("@Email", account.Email);
                                command.Parameters.AddWithValue("@Platform", account.Platform);
                                command.Parameters.AddWithValue("@AccountType", account.AccountType);
                                command.Parameters.AddWithValue("@Status", account.Status);
                                command.Parameters.AddWithValue("@Value", account.Value);
                                command.Parameters.AddWithValue("@OnDatabase", "YES");

                                await command.ExecuteNonQueryAsync();

                                account.OnDatabase = "YES";
                            }
                        }

                        await SaveAccounts(accounts);
                        Console.WriteLine("Accounts saved to database successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving to database: {ex.Message}");
                }
            }
        }

        private async Task PullDataFromDatabase(string? platform, string? accountType, string? value)
        {
            string connectionStringWithoutDb = $"Server={server};User={user};Password={password};Port={port};";
            using (var connection = new MySqlConnection(connectionStringWithoutDb))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database server connection established.");

                    // Create database if it doesn't exist
                    var createDbCommandText = $"CREATE DATABASE IF NOT EXISTS `{database}`;";
                    using (var createDbCommand = new MySqlCommand(createDbCommandText, connection))
                    {
                        await createDbCommand.ExecuteNonQueryAsync();
                    }

                    // Create table if it doesn't exist
                    string connectionStringWithDb = GetConnectionString();
                    using (var connectionWithDb = new MySqlConnection(connectionStringWithDb))
                    {
                        await connectionWithDb.OpenAsync();

                        var createTableCommandText = @"
                            CREATE TABLE IF NOT EXISTS `Accounts` (
                                `Id` INT NOT NULL AUTO_INCREMENT,
                                `Username` VARCHAR(255) NOT NULL,
                                `Password` VARCHAR(255) NOT NULL,
                                `Email` VARCHAR(255) NOT NULL,
                                `Platform` VARCHAR(255) NOT NULL,
                                `AccountType` VARCHAR(255) NOT NULL,
                                `Status` VARCHAR(255) NOT NULL,
                                `Value` VARCHAR(255) NOT NULL,
                                `OnDatabase` VARCHAR(255) NOT NULL,
                                PRIMARY KEY (`Id`)
                            );";
                        using (var createTableCommand = new MySqlCommand(createTableCommandText, connectionWithDb))
                        {
                            await createTableCommand.ExecuteNonQueryAsync();
                        }

                        var commandText = "SELECT Username, Password, Email, Platform, AccountType, Status, Value, OnDatabase FROM Accounts";

                        if (!string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(accountType) && !string.IsNullOrEmpty(value))
                        {
                            commandText += " WHERE Platform = @Platform AND AccountType = @AccountType AND Value = @Value";
                        }

                        using (var command = new MySqlCommand(commandText, connectionWithDb))
                        {
                            if (!string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(accountType) && !string.IsNullOrEmpty(value))
                            {
                                command.Parameters.AddWithValue("@Platform", platform);
                                command.Parameters.AddWithValue("@AccountType", accountType);
                                command.Parameters.AddWithValue("@Value", value);
                            }

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                var pulledAccounts = new List<Account>();

                                while (await reader.ReadAsync())
                                {
                                    pulledAccounts.Add(new Account
                                    {
                                        Username = reader.GetString("Username"),
                                        Password = reader.GetString("Password"),
                                        Email = reader.GetString("Email"),
                                        Platform = reader.GetString("Platform"),
                                        AccountType = reader.GetString("AccountType"),
                                        Status = reader.GetString("Status"),
                                        Value = reader.GetString("Value"),
                                        OnDatabase = reader.GetString("OnDatabase")
                                    });
                                }

                                await SavePulledAccounts(pulledAccounts);
                                Console.WriteLine("Accounts pulled from database successfully.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pulling data from database: {ex.Message}");
                }
            }
        }

        private string GenerateRandomUsername()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private string GenerateRandomPassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private string GetConnectionString()
        {
            return $"Server={server};Database={database};User={user};Password={password};Port={port};";
        }

        private async Task PushDataToDatabase(string? platform, string? accountType, string? value)
        {
            var accounts = LoadAccounts();

            if (!string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(accountType) && !string.IsNullOrEmpty(value))
            {
                accounts = accounts.Where(a => a.Platform == platform && a.AccountType == accountType && a.Value == value).ToList();
            }

            await SaveToDatabase(accounts);
        }

        private async Task SavePulledAccounts(List<Account> accounts)
        {
            var pulledAccountsFilePath = Path.Combine(FilesFolder, PulledAccountsFileName);

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(accounts, options);
                await File.WriteAllTextAsync(pulledAccountsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving pulled accounts: {ex.Message}");
            }
        }

        private class AccgenConfigData
        {
            public string? EmailDomain { get; set; } = "emailhere.com";
            public List<string>? Platforms { get; set; } = new List<string> { "platform1", "platform2" };
        }

        private class MysqlConfigData
        {
            public string? Server { get; set; } = "localhost";
            public string? Database { get; set; } = "cipherlink";
            public string? User { get; set; } = "root";
            public string? Password { get; set; } = "password";
            public string? Port { get; set; } = "3306";
        }

        private class Account
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Platform { get; set; } = string.Empty;
            public string AccountType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string OnDatabase { get; set; } = "NO";
        }
    }
}
