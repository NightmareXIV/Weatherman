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
        public bool MusicEnabled = false;
        public bool ShowUnnamedZones = false;
        public bool ShowOnlyModified = false;
        public bool ShowOnlyWorldZones = true;
        public bool ShowCurrentZoneOnTop = true;
        public int ChlogReadVer = 1;

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
            plugin.pi.SavePluginConfig(this);
        }
    }
}
