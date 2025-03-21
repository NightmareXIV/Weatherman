﻿using Dalamud.Configuration;

namespace Weatherman
{
    [Serializable]
    class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public string ConfigurationString = "";
        public int GlobalTimeFlowControl = 0;
        public int GlobalFixedTime = 0;
        public SortedDictionary<byte, bool> BlacklistedWeathers = new();
        public bool EnableLogging = true;
        public bool MusicEnabled = false;
        public bool ShowUnnamedZones = false;
        public bool ShowOnlyModified = false;
        public bool ShowOnlyWorldZones = true;
        public bool ShowCurrentZoneOnTop = true;
        //public int ChlogReadVer = ChlogGui.ChlogVersion;
        public bool DisplayNotifications = false;
        public bool DisableInCutscene = true;
        public bool EnableTimeControl = true;
        public bool EnableWeatherControl = true;
        public bool NoClockNag = false;
        public bool ChangeTimeFlowSpeed = false;
        public float TimeFlowSpeed = 1.0f;
        public bool DisplayInGpose = false;
        public bool UseGMTForRealTime = false;
        public int Offset = 0;
        public bool? BlacklistCS = null;

        [NonSerialized]
        private Weatherman plugin;

        public void Initialize(Weatherman plugin)
        {
            this.plugin = plugin;
            SetConfigurationString(ConfigurationString);
        }

        public void Save()
        {
            ConfigurationString = GetConfigurationString();
            Svc.PluginInterface.SavePluginConfig(this);
        }

        public string GetConfigurationString()
        {
            var configList = new List<string>();
            foreach (var z in plugin.ZoneSettings)
            {
                var v = z.Value.GetString();
                if (v != null) configList.Add(z.Key + "@" + v);
            }
            return string.Join("\n", configList);
        }

        public void SetConfigurationString(string s)
        {
            foreach (var z in s.Split('\n'))
            {
                try
                {
                    var key = ushort.Parse(z.Split('@')[0]);
                    if (plugin.ZoneSettings.ContainsKey(key))
                    {
                        plugin.ZoneSettings[key].FromString(z.Split('@')[1]);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
    }
}
