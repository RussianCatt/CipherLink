using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CipherLink.Setup;

namespace CipherLink.Plugins
{
    public class SetupPlugin : IPlugin
    {
        private readonly Dictionary<string, ISetupScript> _setupScripts;

        public string Name => "setup";

        public string Description => GenerateDescription();

        public SetupPlugin()
        {
            _setupScripts = LoadSetupScripts();
        }

        public void Execute(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            string component = args[0].ToLower();
            if (!_setupScripts.ContainsKey(component))
            {
                PrintHelp();
                return;
            }

            ISetupScript setupScript = _setupScripts[component];
            if (args.Length == 1)
            {
                setupScript.ExecuteInteractive();
            }
            else
            {
                setupScript.ExecuteCommand(args);
            }
        }

        private string GenerateDescription()
        {
            var descriptions = new StringBuilder();

            descriptions.AppendLine(@"**************************************************************
*                    SETUP Command                            *
**************************************************************
  The `setup` command allows you to configure various components 
  using dedicated setup scripts.

  Usage:
  - Interactive mode:
    setup <component>
  - Command-line mode:
    setup <component> [parameters...]

  Available Components:");

            foreach (var script in _setupScripts.Values)
            {
                descriptions.AppendLine($"  - {script.Name}: {script.Description}");
            }

            descriptions.AppendLine("\nDetailed Descriptions for Setup Scripts:");

            foreach (var script in _setupScripts.Values)
            {
                descriptions.AppendLine(script.Description);
                descriptions.AppendLine(script.Usage);
                descriptions.AppendLine();
            }

            return descriptions.ToString();
        }

        private void PrintHelp()
        {
            Console.WriteLine(Description);
        }

        private Dictionary<string, ISetupScript> LoadSetupScripts()
        {
            var setupScripts = new Dictionary<string, ISetupScript>();

            var scriptTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISetupScript).IsAssignableFrom(t) && !t.IsInterface);

            foreach (var type in scriptTypes)
            {
                var scriptInstance = (ISetupScript)Activator.CreateInstance(type);
                setupScripts.Add(scriptInstance.Name.ToLower(), scriptInstance);
            }

            return setupScripts;
        }
    }
}
