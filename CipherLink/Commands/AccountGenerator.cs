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

        private string emailDomain;
        private List<string> platforms;
        private string server;
        private string database;
        private string user;
        private string password;
        private string port;

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
                Console.WriteLine("Usage: accgen <number> <platform> <service|game> <value>");
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
                string filterPlatform = null;
                string filterAccountType = null;
                string filterValue = null;

                if (args.Length == 5 && args[1].ToLower() == "-filter")
                {
                    filterPlatform = args[2];
                    filterAccountType = args[3];
                    filterValue = args[4];
                }

                Task.Run(async () => await PushDataToDatabase(filterPlatform, filterAccountType, filterValue)).Wait();
                return;
            }

            if (args.Length >= 1 && args[0].ToLower() == "pulldb")
            {
                string filterPlatform = null;
                string filterAccountType = null;
                string filterValue = null;

                if (args.Length == 5 && args[1].ToLower() == "-filter")
                {
                    filterPlatform = args[2];
                    filterAccountType = args[3];
                    filterValue = args[4];
                }

                Task.Run(async () => await PullDataFromDatabase(filterPlatform, filterAccountType, filterValue)).Wait();
                return;
            }

            if (args.Length < 4)
            {
                Console.WriteLine("Usage: accgen <number> <platform> <service|game> <value>");
                return;
            }

            if (!int.TryParse(args[0], out var numberOfAccounts) || numberOfAccounts <= 0)
            {
                Console.WriteLine("Invalid number of accounts. Please specify a positive integer.");
                return;
            }

            string platform = args[1].ToLower();
            string accountType = args[2].ToLower();
            string value = args[3];

            if (!platforms.Contains(platform))
            {
                Console.WriteLine($"Invalid platform. Available platforms are: {string.Join(", ", platforms)}");
                return;
            }

            Task.Run(async () =>
            {
                await GenerateAccounts(numberOfAccounts, platform, accountType, value);
            }).Wait();
        }

        private async Task GenerateAccounts(int numberOfAccounts, string platform, string accountType, string value)
        {
            var accounts = LoadAccounts();

            for (var i = 0; i < numberOfAccounts; i++)
            {
                var username = GenerateRandomUsername();
                var password = GenerateRandomPassword();
                var email = $"{username}@{emailDomain}";

                accounts.Add(new Account
                {
                    Username = username,
                    Password = password,
                    Email = email,
                    Platform = platform,
                    AccountType = accountType,
                    Status = "Not Ready",
                    Value = value,
                    OnDatabase = "NO"
                });
            }

            await SaveAccounts(accounts);

            Console.WriteLine($"Generated {numberOfAccounts} accounts with Platform: {platform}, Account Type: {accountType}, Value: {value}, and Email Domain: {emailDomain}");

            Console.Write("Do you want to save these accounts to the MySQL database? (yes/no): ");
            string response = Console.ReadLine().Trim().ToLower();

            if (response == "yes")
            {
                await SaveToDatabase(accounts);
            }
        }

        private bool LoadAccgenConfig()
        {
            try
            {
                string accgenConfigFilePath = Path.Combine(ConfigFolder, AccgenConfigFileName);

                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                }

                if (!File.Exists(accgenConfigFilePath))
                {
                    WriteDefaultAccgenConfig(accgenConfigFilePath);
                }

                var json = File.ReadAllText(accgenConfigFilePath);
                var configData = JsonSerializer.Deserialize<AccgenConfigData>(json);
                if (configData != null)
                {
                    emailDomain = configData.EmailDomain;
                    platforms = configData.Platforms ?? new List<string> { "steam", "epicgames", "gog" };
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
                string mysqlConfigFilePath = Path.Combine(ConfigFolder, MysqlConfigFileName);

                if (!File.Exists(mysqlConfigFilePath))
                {
                    WriteDefaultMysqlConfig(mysqlConfigFilePath);
                }

                var json = File.ReadAllText(mysqlConfigFilePath);
                var configData = JsonSerializer.Deserialize<MysqlConfigData>(json);
                if (configData != null)
                {
                    server = configData.Server;
                    database = configData.Database;
                    user = configData.User;
                    password = configData.Password;
                    port = configData.Port;
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

        private void WriteDefaultAccgenConfig(string filePath)
        {
            try
            {
                var configData = new AccgenConfigData
                {
                    EmailDomain = "emailhere.com",
                    Platforms = new List<string> { "steam", "epicgames", "gog" }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(configData, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing accgen config file: {ex.Message}");
            }
        }

        private void WriteDefaultMysqlConfig(string filePath)
        {
            try
            {
                var configData = new MysqlConfigData
                {
                    Server = "localhost",
                    Database = "myDatabase",
                    User = "myUsername",
                    Password = "myPassword",
                    Port = "3306"
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(configData, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing mysql config file: {ex.Message}");
            }
        }

        private List<Account> LoadAccounts()
        {
            var accountsFilePath = Path.Combine(FilesFolder, AccountsFileName);
            if (File.Exists(accountsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(accountsFilePath);
                    var accounts = JsonSerializer.Deserialize<List<Account>>(json);
                    if (accounts != null)
                        return accounts;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading accounts file: {ex.Message}");
                }
            }
            return new List<Account>();
        }

        private async Task SaveAccounts(List<Account> accounts)
        {
            try
            {
                if (!Directory.Exists(FilesFolder))
                {
                    Directory.CreateDirectory(FilesFolder);
                }

                var accountsFilePath = Path.Combine(FilesFolder, AccountsFileName);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var accountsJson = JsonSerializer.Serialize(accounts, options);
                await File.WriteAllTextAsync(accountsFilePath, accountsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts: {ex.Message}");
            }
        }

        private string GenerateRandomUsername()
        {
            return "User" + string.Concat(Enumerable.Range(0, 8).Select(_ => (char)('A' + Random.Next(26))));
        }

        private string GenerateRandomPassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Range(0, 12).Select(_ => chars[Random.Next(chars.Length)]).ToArray());
        }

        private async Task SaveToDatabase(List<Account> accounts)
        {
            try
            {
                using var connection = new MySqlConnection(GetConnectionString());
                await connection.OpenAsync();

                await EnsureTableExists(connection);

                foreach (var account in accounts)
                {
                    if (account.OnDatabase == "NO")
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = @"INSERT INTO Accounts (Username, Password, Email, Platform, AccountType, Status, Value, OnDatabase) VALUES (@Username, @Password, @Email, @Platform, @AccountType, @Status, @Value, @OnDatabase)";
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

                Console.WriteLine("Accounts saved to the database successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts to the database: {ex.Message}");
            }
        }

        private async Task PushDataToDatabase(string platform = null, string accountType = null, string value = null)
        {
            var accounts = LoadAccounts();

            try
            {
                using var connection = new MySqlConnection(GetConnectionString());
                await connection.OpenAsync();

                await EnsureTableExists(connection);

                var filteredAccounts = accounts.Where(a =>
                    a.OnDatabase == "NO" &&
                    (string.IsNullOrEmpty(platform) || a.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(accountType) || a.AccountType.Equals(accountType, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(value) || a.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                foreach (var account in filteredAccounts)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"INSERT INTO Accounts (Username, Password, Email, Platform, AccountType, Status, Value, OnDatabase) VALUES (@Username, @Password, @Email, @Platform, @AccountType, @Status, @Value, @OnDatabase)";
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

                await SaveAccounts(accounts);

                Console.WriteLine("Filtered accounts pushed to the database successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pushing accounts to the database: {ex.Message}");
            }
        }

        private async Task PullDataFromDatabase(string platform = null, string accountType = null, string value = null)
        {
            var pulledAccounts = new List<Account>();

            try
            {
                using var connection = new MySqlConnection(GetConnectionString());
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                var query = "SELECT * FROM Accounts WHERE 1=1";
                if (!string.IsNullOrEmpty(platform))
                {
                    query += " AND Platform = @Platform";
                    command.Parameters.AddWithValue("@Platform", platform);
                }
                if (!string.IsNullOrEmpty(accountType))
                {
                    query += " AND AccountType = @AccountType";
                    command.Parameters.AddWithValue("@AccountType", accountType);
                }
                if (!string.IsNullOrEmpty(value))
                {
                    query += " AND Value = @Value";
                    command.Parameters.AddWithValue("@Value", value);
                }
                command.CommandText = query;

                using var reader = await command.ExecuteReaderAsync();

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

                Console.WriteLine("Filtered data pulled from the database successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pulling data from the database: {ex.Message}");
            }
        }

        private async Task SavePulledAccounts(List<Account> accounts)
        {
            try
            {
                if (!Directory.Exists(FilesFolder))
                {
                    Directory.CreateDirectory(FilesFolder);
                }

                var pulledAccountsFilePath = Path.Combine(FilesFolder, PulledAccountsFileName);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var accountsJson = JsonSerializer.Serialize(accounts, options);
                await File.WriteAllTextAsync(pulledAccountsFilePath, accountsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving pulled accounts: {ex.Message}");
            }
        }

        private async Task EnsureTableExists(MySqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(255) NOT NULL,
                    Password VARCHAR(255) NOT NULL,
                    Email VARCHAR(255) NOT NULL,
                    Platform VARCHAR(255) NOT NULL,
                    AccountType VARCHAR(255) NOT NULL,
                    Status VARCHAR(255) NOT NULL,
                    Value VARCHAR(255) NOT NULL,
                    OnDatabase VARCHAR(3) NOT NULL
                )";
            await command.ExecuteNonQueryAsync();
        }

        private string GetConnectionString()
        {
            try
            {
                string mysqlConfigFilePath = Path.Combine(ConfigFolder, MysqlConfigFileName);

                if (!File.Exists(mysqlConfigFilePath))
                {
                    WriteDefaultMysqlConfig(mysqlConfigFilePath);
                }

                var json = File.ReadAllText(mysqlConfigFilePath);
                var mysqlConfigData = JsonSerializer.Deserialize<MysqlConfigData>(json);
                if (mysqlConfigData != null)
                {
                    server = mysqlConfigData.Server;
                    database = mysqlConfigData.Database;
                    user = mysqlConfigData.User;
                    password = mysqlConfigData.Password;
                    port = mysqlConfigData.Port;
                }

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) ||
                    string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(port))
                {
                    throw new Exception("MySQL configuration values are null or empty.");
                }

                return $"Server={server};Database={database};Uid={user};Pwd={password};Port={port};";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting connection string: {ex.Message}");
                return null;
            }
        }

        private class AccgenConfigData
        {
            public string EmailDomain { get; set; }
            public List<string> Platforms { get; set; }
        }

        private class MysqlConfigData
        {
            public string Server { get; set; }
            public string Database { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string Port { get; set; }
        }

        public class Account
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public string Platform { get; set; }
            public string AccountType { get; set; }
            public string Status { get; set; }
            public string Value { get; set; }
            public string OnDatabase { get; set; }
        }
    }
}
