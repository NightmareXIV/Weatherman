using Dalamud;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Weatherman
{
    unsafe class Weatherman : IDalamudPlugin
    {
        public const int SecondsInDay = 60*60*24;

        public DalamudPluginInterface _pi;
        public string Name => "Weatherman";
        public byte[] TimeStopOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        public byte[] TimeStopOff = new byte[] { 0x48, 0x89, 0x83, 0x08, 0x16, 0x00, 0x00 };
        public IntPtr TimeStopPtr;
        public long* TimePtr;
        public byte* CurrentWeatherPtr;
        public Gui ConfigGui;
        public byte WeatherTestActive = 255;
        public Dictionary<ushort, TerritoryType> zones;
        public Dictionary<byte, string> weathers;
        public ExcelSheet<WeatherRate> weatherRates;
        public Dictionary<ushort, ZoneSettings> ZoneSettings;
        public Configuration configuration;

        public void Dispose()
        {
            _pi.Framework.OnUpdateEvent -= HandleFrameworkUpdate;
            _pi.UiBuilder.OnBuildUi -= ConfigGui.Draw;
            SafeMemory.WriteBytes(TimeStopPtr, TimeStopOff);
            _pi.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pi = pluginInterface;
            _pi.Framework.OnUpdateEvent += HandleFrameworkUpdate;
            ConfigGui = new Gui(this);
            _pi.UiBuilder.OnBuildUi += ConfigGui.Draw;
            _pi.UiBuilder.OnOpenConfigUi += delegate { ConfigGui.configOpen = true; };
            TimePtr = (long*)(_pi.Framework.Address.BaseAddress + 0x1608);
            TimeStopPtr = _pi.TargetModuleScanner.ScanText("48 89 83 08 16 00 00 48 69"); //yeeted from cmtool https://github.com/imchillin/CMTool
            CurrentWeatherPtr = (byte*)(*(IntPtr*)_pi.TargetModuleScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 0F B6 EA 48 8B F9 41 8B DE 48 8B 70 08 48 85 F6 0F 84 ?? ?? ?? ??") + 0x27); //thanks daemitus
            //CurrentWeatherPtr = (byte*)(*(IntPtr*)(Process.GetCurrentProcess().MainModule.BaseAddress + 0x1D682B8) + 0x27); //yeeted from cmtool yet again 
            SafeMemory.WriteBytes(TimeStopPtr, TimeStopOn);
            zones = pluginInterface.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
            weathers = pluginInterface.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
            weatherRates = _pi.Data.GetExcelSheet<WeatherRate>();
            ZoneSettings = new Dictionary<ushort, ZoneSettings>();
            foreach(var z in zones)
            {
                var s = new ZoneSettings();
                s.ZoneId = z.Key;
                s.ZoneName = z.Value.PlaceName.Value.Name;
                s.Init(this);
                ZoneSettings.Add(s.ZoneId, s);
            }
            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            configuration.Initialize(this);
        }

        public string GetConfigurationString()
        {
            var configList = new List<string>();
            foreach(var z in ZoneSettings)
            {
                var v = z.Value.GetString();
                if (v != null) configList.Add(z.Key + "@" + v);
            }
            return string.Join("\n", configList);
        }

        public void SetConfigurationString(string s)
        {
            foreach(var z in s.Split('\n'))
            {
                try
                {
                    var key = ushort.Parse(z.Split('@')[0]);
                    if (ZoneSettings.ContainsKey(key))
                    {
                        ZoneSettings[key].FromString(z.Split('@')[1]);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        void HandleFrameworkUpdate(Framework f)
        {
            var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
            var timeOfDay = et % SecondsInDay;
            if (timeOfDay > 18 * 60 * 60) et -= 2 * (timeOfDay - 18 * 60 * 60);
            if (timeOfDay < 6 * 60 * 60) et += 2 * (6 * 60 * 60 - timeOfDay);
            * TimePtr = et;
        }

        public bool IsWeatherNormal(byte id, ushort terr)
        {
            var w = new HashSet<Weather>();
            foreach (var u in weatherRates.GetRow(zones[terr].WeatherRate).UnkStruct0)
            {
                if (u.Weather != 0 && u.Weather == id) return true; 
            }
            return false;
        }

        public List<byte> GetWeathers(ushort id) //yeeted from titleedit https://github.com/lmcintyre/TitleEditPlugin
        {
            var weathers = new List<byte>();
            if (!zones.TryGetValue(id, out var path)) return null;
            try
            {
                var file = _pi.Data.GetFile<LvbFile>($"bg/{path.Bg}.lvb");
                if (file?.weatherIds == null || file.weatherIds.Length == 0)
                    return null;
                foreach (var weather in file.weatherIds)
                    if (weather > 0 && weather < 255)
                        weathers.Add((byte)weather);
                weathers.Sort();
                return weathers;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Failed to load lvb for {path}");
            }
            return null;
        }
    }
}
