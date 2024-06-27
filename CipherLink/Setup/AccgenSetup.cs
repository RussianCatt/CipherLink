using System;
using System.Collections.Generic;
using System.IO;
using CipherLink.Utils;
using Newtonsoft.Json;

namespace CipherLink.Setup
{
    public class AccgenSetup : ISetupScript
    {
        private const string ConfigFolder = "Config";
        private const string AccgenConfigFile = "accgen.conf";
        private readonly string _accgenConfigFilePath;

        public AccgenSetup()
        {
            _accgenConfigFilePath = Path.Combine(ConfigFolder, AccgenConfigFile);
        }

        public string Name => "accgen";
        public string Description => "This script configures the Account Generator (accgen) settings.";
        public string Usage => @"
  - Interactive mode:
    setup accgen
  - Command-line mode:
    setup accgen <email_domain> <platform1,platform2,...>

  Examples:
  - Interactive mode:
    ```
    setup accgen
    ```
    This command will guide you through entering accgen configuration interactively.

  - Command-line mode:
    ```
    setup accgen emailhere.com platform1,platform2
    ```
    This command will directly configure accgen with the provided parameters.";

        public void ExecuteInteractive()
        {
            Console.WriteLine("Enter Accgen configuration:");
            Console.Write("Email Domain: ");
            string emailDomain = Console.ReadLine();

            Console.Write("Platforms (comma-separated): ");
            string platformsInput = Console.ReadLine();
            string[] platforms = platformsInput.Split(',');

            var config = new AccgenConfigData
            {
                EmailDomain = emailDomain,
                Platforms = new List<string>(platforms)
            };

            SaveConfig(config);
            Console.WriteLine("Accgen configuration saved.");
        }

        public void ExecuteCommand(string[] args)
        {
            string emailDomain = args[1];
            string[] platforms = args[2].Split(',');

            var config = new AccgenConfigData
            {
                EmailDomain = emailDomain,
                Platforms = new List<string>(platforms)
            };

            SaveConfig(config);
            Console.WriteLine("Accgen configuration saved.");
        }

        private void SaveConfig(AccgenConfigData config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            Directory.CreateDirectory(ConfigFolder);
            File.WriteAllText(_accgenConfigFilePath, json);
        }
    }
}
