using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CipherLink.Plugins
{
    public static class PluginLoader
    {
        public static List<IPlugin> LoadPlugins()
        {
            var plugins = new List<IPlugin>();
            var pluginTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is IPlugin plugin)
                {
                    plugins.Add(plugin);
                }
            }

            return plugins;
        }
    }
}
