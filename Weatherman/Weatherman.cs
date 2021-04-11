using Dalamud;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Weatherman
{
    unsafe class Weatherman : IDalamudPlugin
    {
        public const int SecondsInDay = 60 * 60 * 24;

        public DalamudPluginInterface _pi;
        public string Name => "Weatherman";
        public byte[] TimeStopOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        public byte[] TimeStopOff = new byte[] { 0x48, 0x89, 0x83, 0x08, 0x16, 0x00, 0x00 };
        public IntPtr TimeStopPtr;
        public byte* FirstByteTimeStopPtr;
        public long* TimePtr;
        public byte* CurrentWeatherPtr;
        public Gui ConfigGui;
        public byte WeatherTestActive = 255;
        public Dictionary<ushort, TerritoryType> zones;
        public Dictionary<byte, string> weathers;
        public ExcelSheet<WeatherRate> weatherRates;
        public Dictionary<ushort, ZoneSettings> ZoneSettings;
        public Configuration configuration;
        public byte SelectedWeather = 255;
        public byte UnblacklistedWeather = 0;
        public string[] Log = new string[100];
        public bool PausePlugin = false;
        public bool AtVista = false;
        public bool WeatherWasChanged = false;
        public IFFXIVWeatherLuminaService WeatherSvc;
        public bool BGMModified = false;
        private Dictionary<int, Song> SongList = new Dictionary<int, Song>
        {
            [0] = new Song(0, "Default")
        };

        public void Dispose()
        {
            configuration.Save();
            _pi.Framework.OnUpdateEvent -= HandleFrameworkUpdate;
            _pi.UiBuilder.OnBuildUi -= ConfigGui.Draw;
            _pi.ClientState.TerritoryChanged -= HandleZoneChange;
            _pi.CommandManager.RemoveHandler("/weatherman");
            _pi.Framework.Gui.Chat.OnChatMessage -= HandleChatMessage;
            EnableNaturalTimeFlow();
            if (WeatherWasChanged) RestoreOriginalWeather();
            if (BGMModified) StopSong();
            _pi.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pi = pluginInterface;
            TimePtr = (long*)(_pi.Framework.Address.BaseAddress + 0x1608);
            TimeStopPtr = _pi.TargetModuleScanner.ScanText("48 89 83 08 16 00 00 48 69"); //yeeted from cmtool https://github.com/imchillin/CMTool
            CurrentWeatherPtr = (byte*)(*(IntPtr*)_pi.TargetModuleScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 0F B6 EA 48 8B F9 41 8B DE 48 8B 70 08 48 85 F6 0F 84 ?? ?? ?? ??") + 0x27); //thanks daemitus
            //CurrentWeatherPtr = (byte*)(*(IntPtr*)(Process.GetCurrentProcess().MainModule.BaseAddress + 0x1D682B8) + 0x27); //yeeted from cmtool yet again 
            FirstByteTimeStopPtr = (byte*)TimeStopPtr;
            zones = pluginInterface.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
            weathers = pluginInterface.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
            weatherRates = _pi.Data.GetExcelSheet<WeatherRate>();
            ZoneSettings = new Dictionary<ushort, ZoneSettings>();
            foreach (var z in zones)
            {
                var s = new ZoneSettings();
                s.ZoneId = z.Key;
                s.ZoneName = z.Value.PlaceName.Value.Name;
                s.terr = z.Value;
                s.Init(this);
                ZoneSettings.Add(s.ZoneId, s);
            }
            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            configuration.Initialize(this);
            var normalweathers = new HashSet<byte>();
            foreach (var z in ZoneSettings)
            {
                foreach (var a in z.Value.SupportedWeathers)
                {
                    if (a.IsNormal)
                    {
                        normalweathers.Add(a.Id);
                    }
                }
            }
            var tempdict = new Dictionary<byte, bool>(configuration.BlacklistedWeathers);
            foreach (var i in tempdict)
            {
                if (!normalweathers.Contains(i.Key))
                {
                    configuration.BlacklistedWeathers.Remove(i.Key);
                }
            }
            foreach (var i in normalweathers)
            {
                if (!configuration.BlacklistedWeathers.ContainsKey(i)) configuration.BlacklistedWeathers.Add(i, false);
            }
            WeatherSvc = new FFXIVWeatherLuminaService(_pi.Data);
            _pi.Framework.OnUpdateEvent += HandleFrameworkUpdate;
            ConfigGui = new Gui(this);
            _pi.UiBuilder.OnBuildUi += ConfigGui.Draw;
            _pi.UiBuilder.OnOpenConfigUi += delegate { ConfigGui.configOpen = true; };
            _pi.ClientState.TerritoryChanged += HandleZoneChange;
            if (_pi.ClientState != null) ApplyWeatherChanges(_pi.ClientState.TerritoryType);
            _pi.CommandManager.AddHandler("/weatherman", new Dalamud.Game.Command.CommandInfo(delegate { ConfigGui.configOpen = true; }) { });
            _pi.Framework.Gui.Chat.OnChatMessage += HandleChatMessage;
        }

        public IDalamudPlugin GetOrchestrionPlugin()
        {
            try
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var d = (Dalamud.Dalamud)_pi.GetType().GetField("dalamud", flags).GetValue(_pi);
                var pmanager = d.GetType().GetProperty("PluginManager", flags).GetValue(d);
                var plugins =
                    (List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)>)
                    pmanager.GetType().GetField("Plugins").GetValue(pmanager);
                WriteLog("Found plugins: " + plugins.Count);
                foreach (var p in plugins)
                {
                    if(p.Plugin.Name == "Orchestrion plugin")
                    {
                        var porch = p.Plugin;
                        WriteLog("Found Orchestrion plugin.");
                        return porch;
                    }
                }
                return null;
            }
            catch(Exception e)
            {
                WriteLog("Can't find orchestrion plugin: " + e.Message);
                WriteLog(e.StackTrace);
                return null;
            }
        }

        public void PlaySong(int id)
        {
            try
            {
                var p = GetOrchestrionPlugin();
                if (p == null) return;
                p.GetType().GetMethod("PlaySong").Invoke(p, new object[] { id });
            }
            catch(Exception e)
            {
                WriteLog("Failed to play song:" + e.Message);
            }
        }

        public void StopSong()
        {
            try
            {
                var p = GetOrchestrionPlugin();
                if (p == null) return;
                p.GetType().GetMethod("StopSong").Invoke(p, new object[] { });
            }
            catch (Exception e)
            {
                WriteLog("Failed to stop song:" + e.Message);
            }
        }

        public Dictionary<int, Song> GetSongList()
        {
            if (SongList.Count > 1) return SongList;
            try
            {
                var p = GetOrchestrionPlugin();
                if (p == null) return null;
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var slist = p.GetType().GetField("songList", flags).GetValue(p);
                var songlist = (IDictionary)slist.GetType().GetField("songs", flags).GetValue(slist);
                WriteLog("Songs found: "+ songlist.Count);
                int i = 0;
                foreach(var o in songlist.Keys)
                {
                    SongList.Add(++i, new Song(
                        (int)songlist[o].GetType().GetField("Id").GetValue(songlist[o]),
                        (string)songlist[o].GetType().GetField("Name").GetValue(songlist[o])
                        ));
                }
                WriteLog("Song list contains " + SongList.Count + " entries / " + i);
                if (SongList.Count > 1) return SongList;
            }
            catch (Exception e)
            {
                WriteLog("Failed to retrieve song list:" + e.Message);
            }
            return null;
        }

        public Song GetSongById(int id)
        {
            foreach(var i in SongList)
            {
                if (i.Value.Id == id) return i.Value;
            }
            return null;
        }

        HashSet<string> ArrivedAtVistaMessages = new HashSet<string>
        {
            "You have arrived at a vista!"
        };
        HashSet<string> AwayFromVistaMesssages = new HashSet<string>
        {
            "You have strayed too far from the vista."
        };
        void HandleChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if ((ushort)type != 2105) return;
            var m = message.ToString();
            if (ArrivedAtVistaMessages.Contains(m))
            {
                AtVista = true;
                WriteLog("Arrived at vista: " + m);
            }
            else if (AwayFromVistaMesssages.Contains(m))
            {
                AtVista = false;
                WriteLog("Away from vista: " + m);
            }
        }

        //probably easiest way to get overworld territories - includes eureka and bozja but have to add cities myself
        HashSet<ushort> Cities = new HashSet<ushort>
        {
            128, 129, //limsa lominsa
            132, 133, //gridania
            130, 131, //uldah
            628, //kugane
            418, 419, //ishgard
            819, //crys
            820 //eulmore
        };
        public bool IsWorldTerritory(ushort territory)
        {
            if (!ZoneSettings.ContainsKey(territory)) return false;
            return Cities.Contains(ZoneSettings[territory].ZoneId) || ZoneSettings[territory].terr.Mount;
        }

        public string GetConfigurationString()
        {
            var configList = new List<string>();
            foreach (var z in ZoneSettings)
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

        private void HandleZoneChange(object s, ushort u)
        {
            WriteLog("Zone changed to " + u + "; is world = " + IsWorldTerritory(u));
            AtVista = false;
            WeatherWasChanged = false;
            ApplyWeatherChanges(u);
        }

        public void ApplyWeatherChanges(ushort u)
        {
            WriteLog("Applying weather changes");
            SelectedWeather = 255;
            UnblacklistedWeather = 0;
            if (BGMModified)
            {
                StopSong();
                BGMModified = false;
            }
            if (ZoneSettings.ContainsKey(u))
            {
                var z = ZoneSettings[u];
                if(configuration.MusicEnabled && z.Music != 0 && !BGMModified)
                {
                    PlaySong(z.Music);
                    BGMModified = true;
                }
                if (z.WeatherControl)
                {
                    var weathers = new List<byte>();
                    foreach (var v in z.SupportedWeathers)
                    {
                        if (v.Selected) weathers.Add(v.Id);
                    }
                    if (weathers.Count > 0) SelectedWeather = weathers[new Random().Next(0, weathers.Count)];
                }
                else
                {
                    foreach (var v in z.SupportedWeathers)
                    {
                        if (UnblacklistedWeather == 0
                            && configuration.BlacklistedWeathers.ContainsKey(v.Id)
                            && !configuration.BlacklistedWeathers[v.Id]
                            && IsWeatherNormal(v.Id, _pi.ClientState.TerritoryType))
                        {
                            UnblacklistedWeather = v.Id;
                        }
                    }
                }
            }
            WriteLog("Selected weather:"+ SelectedWeather + "; unblacklisted weather: " + UnblacklistedWeather);
        }

        [HandleProcessCorruptedStateExceptions]
        void HandleFrameworkUpdate(Framework f)
        {
            try
            {
                if (_pi.ClientState != null && _pi.ClientState.LocalPlayer != null
                    && IsWorldTerritory(_pi.ClientState.TerritoryType)
                    && !PausePlugin && !AtVista)
                {
                    SetTimeBySetting(GetZoneTimeFlowSetting(_pi.ClientState.TerritoryType));
                    if (SelectedWeather != 255 && *CurrentWeatherPtr != SelectedWeather)
                    {
                        WriteLog("Weather set to " + SelectedWeather + " from " + *CurrentWeatherPtr);
                        *CurrentWeatherPtr = SelectedWeather;
                        WeatherWasChanged = true;
                    }
                    if (UnblacklistedWeather != 0 && *CurrentWeatherPtr != UnblacklistedWeather
                        && configuration.BlacklistedWeathers.ContainsKey(*CurrentWeatherPtr)
                        && configuration.BlacklistedWeathers[*CurrentWeatherPtr])
                    {
                        WriteLog("Blacklisted weather " + *CurrentWeatherPtr + " found and changed to " + UnblacklistedWeather);
                        *CurrentWeatherPtr = UnblacklistedWeather;
                        WeatherWasChanged = true;
                    }
                }
                else
                {
                    EnableNaturalTimeFlow();
                }
                if (_pi.ClientState != null && AtVista && WeatherWasChanged)
                {
                    RestoreOriginalWeather();
                }
            }
            catch(Exception e)
            {
                WriteLog("Error in weatherman: "+e);
            }
        }

        public void RestoreOriginalWeather()
        {
            var origweather = WeatherSvc.GetCurrentWeather(_pi.ClientState.TerritoryType, 0);
            if (origweather.Item1 != null)
            {
                var origweatherid = (byte)origweather.Item1.RowId;
                if(IsWeatherNormal(origweatherid, _pi.ClientState.TerritoryType))
                {
                    WeatherWasChanged = false;
                    * CurrentWeatherPtr = origweatherid;
                    WriteLog("Weather restored to original "+origweatherid);
                }
                else
                {
                    WriteLog("Unable to restore weather to original "+origweatherid);
                }
            }
        }

        public void WriteLog(string line)
        {
            if (!configuration.EnableLogging) return;
            line = DateTimeOffset.Now.ToString() + ": " + line;
            for (var i = 0; i < Log.Length; i++)
            {
                if(Log[i] == null)
                {
                    Log[i] = line;
                    return;
                }
            }
            for(var i = 1;i < Log.Length; i++)
            {
                Log[i - 1] = Log[i];
            }
            Log[Log.Length - 1] = line;
        }

        void DisableNaturalTimeFlow()
        {
            if (*FirstByteTimeStopPtr == TimeStopOff[0])
            {
                SafeMemory.WriteBytes(TimeStopPtr, TimeStopOn);
                WriteLog("Time flow stopped");
            }
        }

        void EnableNaturalTimeFlow()
        {
            if (*FirstByteTimeStopPtr == TimeStopOn[0])
            {
                SafeMemory.WriteBytes(TimeStopPtr, TimeStopOff);
                WriteLog("Time flow reenabled");
            }
        }

        void SetTimeBySetting(int setting)
        {
            if(setting == 0) //game managed
            {
                EnableNaturalTimeFlow();
            }
            else if (setting == 1) //normal
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                *TimePtr = et;
            }
            else if (setting == 2) //fixed
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                et -= timeOfDay;
                et += GetZoneTimeFixedSetting(_pi.ClientState.TerritoryType);
                *TimePtr = et;
            }
            else if (setting == 3) //infiniday
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay > 18 * 60 * 60 || timeOfDay < 6 * 60 * 60) et += SecondsInDay / 2;
                *TimePtr = et;
            }
            else if (setting == 4) //infiniday r
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay > 18 * 60 * 60) et -= 2 * (timeOfDay - 18 * 60 * 60);
                if (timeOfDay < 6 * 60 * 60) et += 2 * (6 * 60 * 60 - timeOfDay);
                *TimePtr = et;
            }
            else if (setting == 5) //infininight
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et += SecondsInDay / 2;
                *TimePtr = et;
            }
            else if (setting == 6) //infininight r
            {
                DisableNaturalTimeFlow();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et -= 2 * (timeOfDay - 6 * 60 * 60);
                *TimePtr = et;
            }
        }

        int GetZoneTimeFlowSetting(ushort terr)
        {
            if (ZoneSettings.ContainsKey(terr))
            {
                if (ZoneSettings[terr].TimeFlow > 0) return ZoneSettings[terr].TimeFlow;
            }
            return configuration.GlobalTimeFlowControl;
        }

        int GetZoneTimeFixedSetting(ushort terr)
        {
            if (ZoneSettings.ContainsKey(terr))
            {
                if (ZoneSettings[terr].TimeFlow == 2) return ZoneSettings[terr].FixedTime;
            }
            return configuration.GlobalFixedTime;
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
