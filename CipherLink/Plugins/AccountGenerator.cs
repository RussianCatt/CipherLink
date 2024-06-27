using CipherLink.Commands;
using CipherLink.Utils; // Update namespace reference to CipherLink

namespace CipherLink.Plugins
{
    public static class AccountGenerator
    {
        private static readonly Random Random = new Random();

        public static List<Account> GenerateAccounts(AccgenConfigData accgenConfig, int numberOfAccounts, string platform, string accountType, string value)
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

        private static string GenerateUsername()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private static string GeneratePassword()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
