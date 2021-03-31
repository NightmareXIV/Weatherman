using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    [Serializable]
    class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public string ConfigurationString = "";
        public int GlobalTimeFlowControl = 0;
        public int GlobalFixedTime = 0;
        public SortedDictionary<byte, bool> BlacklistedWeathers = new SortedDictionary<byte, bool>();
        public bool EnableLogging = true;
        public bool Unsafe = false;

        [NonSerialized]
        private Weatherman plugin;

        public void Initialize(Weatherman plugin)
        {
            this.plugin = plugin;
            plugin.SetConfigurationString(ConfigurationString);
        }

        public void Save()
        {
            ConfigurationString = plugin.GetConfigurationString();
            plugin.WriteLog(ConfigurationString);
            plugin._pi.SavePluginConfig(this);
        }
    }
}
