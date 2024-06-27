using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CipherLink.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        void Execute(string[] args);
        string Description { get; } // New: Description property to describe the plugin's functionality
    }
}
