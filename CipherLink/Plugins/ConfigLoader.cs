using CipherLink.Commands;
using CipherLink.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CipherLink.Plugins
{
    public static class ConfigLoader
    {
        public static AccgenConfigData? LoadAccgenConfig(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    WriteDefaultAccgenConfig(path);
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AccgenConfigData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accgen config file: {ex.Message}");
                return null;
            }
        }

        public static MysqlConfigData? LoadMysqlConfig(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    WriteDefaultMysqlConfig(path);
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<MysqlConfigData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading mysql config file: {ex.Message}");
                return null;
            }
        }

        public static List<Account> LoadAccounts(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, "[]");
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accounts: {ex.Message}");
                return new List<Account>();
            }
        }

        public static async Task SaveAccounts(List<Account> accounts, string path)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(accounts, options);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts: {ex.Message}");
            }
        }

        private static void WriteDefaultAccgenConfig(string path)
        {
            var defaultConfig = new AccgenConfigData();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(path, json);
        }

        private static void WriteDefaultMysqlConfig(string path)
        {
            var defaultConfig = new MysqlConfigData();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(path, json);
        }
    }
}
