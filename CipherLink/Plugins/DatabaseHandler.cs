using CipherLink.Commands;
using CipherLink.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CipherLink.Plugins
{
    public static class DatabaseHandler
    {
        public static async Task SaveToDatabase(MysqlConfigData mysqlConfig, List<Account> accounts)
        {
            string connectionStringWithoutDb = $"Server={mysqlConfig.Server};User={mysqlConfig.User};Password={mysqlConfig.Password};Port={mysqlConfig.Port};";
            using (var connection = new MySqlConnection(connectionStringWithoutDb))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database server connection established.");

                    // Create database if it doesn't exist
                    var createDbCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS {mysqlConfig.Database}", connection);
                    await createDbCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating database: {ex.Message}");
                    return;
                }
            }

            string connectionString = $"Server={mysqlConfig.Server};Database={mysqlConfig.Database};User={mysqlConfig.User};Password={mysqlConfig.Password};Port={mysqlConfig.Port};";

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection established.");

                    // Create table if it doesn't exist
                    var createTableCommand = new MySqlCommand(@"
                        CREATE TABLE IF NOT EXISTS accounts (
                            Username VARCHAR(255),
                            Password VARCHAR(255),
                            Email VARCHAR(255),
                            Platform VARCHAR(255),
                            AccountType VARCHAR(255),
                            Status VARCHAR(255),
                            Value VARCHAR(255)
                        );", connection);
                    await createTableCommand.ExecuteNonQueryAsync();

                    foreach (var account in accounts.Where(a => a.OnDatabase != "YES"))
                    {
                        var command = new MySqlCommand(@"
                            INSERT INTO accounts (Username, Password, Email, Platform, AccountType, Status, Value)
                            VALUES (@Username, @Password, @Email, @Platform, @AccountType, @Status, @Value);", connection);
                        command.Parameters.AddWithValue("@Username", account.Username);
                        command.Parameters.AddWithValue("@Password", account.Password);
                        command.Parameters.AddWithValue("@Email", account.Email);
                        command.Parameters.AddWithValue("@Platform", account.Platform);
                        command.Parameters.AddWithValue("@AccountType", account.AccountType);
                        command.Parameters.AddWithValue("@Status", account.Status);
                        command.Parameters.AddWithValue("@Value", account.Value);

                        await command.ExecuteNonQueryAsync();
                        account.OnDatabase = "YES";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving to database: {ex.Message}");
                }
            }
        }

        public static async Task PushDataToDatabase(MysqlConfigData mysqlConfig, List<Account> accounts, string[] args)
        {
            if (args.Length >= 3 && args[1].ToLower() == "-filter")
            {
                var platformFilter = args[2];
                var accountTypeFilter = args[3];
                var valueFilter = args[4];
                var filteredAccounts = accounts.Where(a =>
                    (string.IsNullOrEmpty(platformFilter) || a.Platform == platformFilter) &&
                    (string.IsNullOrEmpty(accountTypeFilter) || a.AccountType == accountTypeFilter) &&
                    (string.IsNullOrEmpty(valueFilter) || a.Value == valueFilter)
                ).ToList();
                await SaveToDatabase(mysqlConfig, filteredAccounts);
            }
            else
            {
                await SaveToDatabase(mysqlConfig, accounts);
            }
        }

        public static async Task PullDataFromDatabase(MysqlConfigData mysqlConfig, string[] args)
        {
            var pulledAccounts = new List<Account>();

            string connectionString = $"Server={mysqlConfig.Server};Database={mysqlConfig.Database};User={mysqlConfig.User};Password={mysqlConfig.Password};Port={mysqlConfig.Port};";
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection established.");

                    var command = new MySqlCommand("SELECT Username, Password, Email, Platform, AccountType, Status, Value FROM accounts", connection);
                    if (args.Length >= 3 && args[1].ToLower() == "-filter")
                    {
                        var platformFilter = args[2];
                        var accountTypeFilter = args[3];
                        var valueFilter = args[4];
                        command = new MySqlCommand(@"
                            SELECT Username, Password, Email, Platform, AccountType, Status, Value 
                            FROM accounts 
                            WHERE (@Platform IS NULL OR Platform = @Platform)
                            AND (@AccountType IS NULL OR AccountType = @AccountType)
                            AND (@Value IS NULL OR Value = @Value)", connection);
                        command.Parameters.AddWithValue("@Platform", platformFilter);
                        command.Parameters.AddWithValue("@AccountType", accountTypeFilter);
                        command.Parameters.AddWithValue("@Value", valueFilter);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            pulledAccounts.Add(new Account
                            {
                                Username = reader.GetString(0),
                                Password = reader.GetString(1),
                                Email = reader.GetString(2),
                                Platform = reader.GetString(3),
                                AccountType = reader.GetString(4),
                                Status = reader.GetString(5),
                                Value = reader.GetString(6)
                            });
                        }
                    }

                    await ConfigLoader.SaveAccounts(pulledAccounts, Path.Combine("Files", "pulled_accounts.json"));
                    Console.WriteLine("Pulled data from the database and saved to pulled_accounts.json");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pulling data from database: {ex.Message}");
                }
            }
        }
    }
}
