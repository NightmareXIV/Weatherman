using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Internal;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Weatherman
{
    unsafe class Weatherman : IDalamudPlugin
    {
        internal const int SecondsInDay = 60 * 60 * 24;

        public string Name => "Weatherman";
        internal MemoryManager memoryManager;
        internal OrchestrionController orchestrionController;
        internal Gui ConfigGui;
        internal byte WeatherTestActive = 255;
        internal Dictionary<ushort, TerritoryType> zones;
        internal Dictionary<byte, string> weathers;
        internal ExcelSheet<WeatherRate> weatherRates;
        internal Dictionary<ushort, ZoneSettings> ZoneSettings;
        internal Configuration configuration;
        internal byte SelectedWeather = 255;
        internal byte UnblacklistedWeather = 0;
        internal string[] Log = new string[100];
        internal bool PausePlugin = false;
        internal Stopwatch stopwatch;
        internal long totalTime = 0;
        internal long totalTicks = 0;
        internal bool profiling = false;

        public void Dispose()
        {
            configuration.Save();
            Svc.Framework.Update -= HandleFrameworkUpdate;
            Svc.PluginInterface.UiBuilder.Draw -= ConfigGui.Draw;
            Svc.ClientState.TerritoryChanged -= HandleZoneChange;
            Svc.Commands.RemoveHandler("/weatherman");
            memoryManager.Dispose();
            if (orchestrionController.BGMModified) orchestrionController.StopSong();
            orchestrionController.Dispose();
        }

        public Weatherman(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            stopwatch = new Stopwatch();
            orchestrionController = new OrchestrionController(this);
            memoryManager = new MemoryManager(this);
            zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
            weathers = Svc.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
            weatherRates = Svc.Data.GetExcelSheet<WeatherRate>();
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
            Svc.Framework.Update += HandleFrameworkUpdate;
            ConfigGui = new Gui(this);
            Svc.PluginInterface.UiBuilder.Draw += ConfigGui.Draw;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigGui.configOpen = true; };
            Svc.ClientState.TerritoryChanged += HandleZoneChange;
            ApplyWeatherChanges(Svc.ClientState.TerritoryType);
            Svc.Commands.AddHandler("/weatherman", new Dalamud.Game.Command.CommandInfo(delegate { ConfigGui.configOpen = true; }) { HelpMessage = "Open plugin settings" });
            if(ChlogGui.ChlogVersion > configuration.ChlogReadVer)
            {
                new ChlogGui(this);
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
            820, //eulmore
            962, //sharla
            963, //dragoncity
        };
        public bool IsWorldTerritory(ushort territory)
        {
            if (!ZoneSettings.ContainsKey(territory)) return false;
            if (configuration.Anywhere) return true;
            return Cities.Contains(ZoneSettings[territory].ZoneId) || ZoneSettings[territory].terr.Mount;
        }

        private void HandleZoneChange(object s, ushort u)
        {
            WriteLog("Zone changed to " + u + "; is world = " + IsWorldTerritory(u));
            ApplyWeatherChanges(u);
        }

        public void ApplyWeatherChanges(ushort u)
        {
            WriteLog("Applying weather changes");
            var resolution = new List<string>();
            SelectedWeather = 255;
            UnblacklistedWeather = 0;
            if (orchestrionController.BGMModified)
            {
                orchestrionController.StopSong();
                orchestrionController.BGMModified = false;
            }
            if (ZoneSettings.ContainsKey(u))
            {
                var z = ZoneSettings[u];
                if(configuration.MusicEnabled && z.Music != 0 && !orchestrionController.BGMModified)
                {
                    orchestrionController.PlaySong(z.Music);
                    orchestrionController.BGMModified = true;
                    resolution.Add($"BGM changed to: {(orchestrionController.GetSongList().TryGetValue(z.Music, out Song song)?song.Name:z.Music)}");
                }
                if (z.WeatherControl)
                {
                    resolution.Add("Weather control enabled in this zone.");
                    var weathers = new List<byte>();
                    foreach (var v in z.SupportedWeathers)
                    {
                        if (v.Selected) weathers.Add(v.Id);
                    }
                    if (weathers.Count > 0)
                    {
                        SelectedWeather = weathers[new Random().Next(0, weathers.Count)];
                        resolution.Add($"Selected weather: {this.weathers[SelectedWeather]}");
                    }
                    else
                    {
                        resolution.Add("Natural weather is preserved.");
                    }
                }
                else
                {
                    var unblacklistedWeatherCandidates = new List<byte>();
                    foreach (var v in z.SupportedWeathers)
                    {
                        if (configuration.BlacklistedWeathers.ContainsKey(v.Id)
                            && !configuration.BlacklistedWeathers[v.Id]
                            && IsWeatherNormal(v.Id, Svc.ClientState.TerritoryType))
                        {
                            unblacklistedWeatherCandidates.Add(v.Id);
                        }
                    }
                    if (unblacklistedWeatherCandidates.Count > 0)
                    {
                        UnblacklistedWeather =
                             unblacklistedWeatherCandidates[new Random().Next(0, unblacklistedWeatherCandidates.Count)];
                        resolution.Add($"Unblacklisted weather selected: {this.weathers[UnblacklistedWeather]}");
                    }
                }
            }
            WriteLog("Selected weather:"+ SelectedWeather + "; unblacklisted weather: " + UnblacklistedWeather);
            if (configuration.DisplayNotifications)
            {
                Svc.PluginInterface.UiBuilder.AddNotification(String.Join("\n", resolution), "Weatherman zone change report", NotificationType.Info, 10000);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        void HandleFrameworkUpdate(Framework f)
        {
            try
            {
                if (profiling)
                {
                    totalTicks++;
                    stopwatch.Restart();
                }
                if (Svc.ClientState.LocalPlayer != null
                    && IsWorldTerritory(Svc.ClientState.TerritoryType)
                    && !PausePlugin
                    && !(configuration.DisableInCutscene && (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                    || Svc.Condition[ConditionFlag.WatchingCutscene78])))
                {
                    if (configuration.EnableTimeControl)
                    {
                        SetTimeBySetting(GetZoneTimeFlowSetting(Svc.ClientState.TerritoryType));
                    }
                    else
                    {
                        memoryManager.DisableCustomTime();
                    }
                    if (configuration.EnableWeatherControl)
                    {
                        if (SelectedWeather != 255)
                        {
                            memoryManager.EnableCustomWeather();
                            if (memoryManager.GetWeather() != SelectedWeather)
                            {
                                memoryManager.SetWeather(SelectedWeather);
                                if (configuration.DisplayNotifications)
                                {
                                    Svc.PluginInterface.UiBuilder.AddNotification($"{weathers[SelectedWeather]}\nReason: selected by user", "Weatherman: weather changed", NotificationType.Info, 5000);
                                }
                            }
                        }
                        else
                        {
                            var suggesterWeather = *memoryManager.TrueWeather;
                            if (UnblacklistedWeather != 0 && suggesterWeather != UnblacklistedWeather
                            && configuration.BlacklistedWeathers.ContainsKey(suggesterWeather)
                            && configuration.BlacklistedWeathers[suggesterWeather])
                            {
                                suggesterWeather = UnblacklistedWeather;
                            }
                            //this is to retain smooth transitions
                            if (suggesterWeather == *memoryManager.TrueWeather)
                            {
                                memoryManager.DisableCustomWeather();
                            }
                            else
                            {
                                memoryManager.EnableCustomWeather();
                                if (memoryManager.GetWeather() != suggesterWeather)
                                {
                                    memoryManager.SetWeather(suggesterWeather);
                                    if (configuration.DisplayNotifications)
                                    {
                                        Svc.PluginInterface.UiBuilder.AddNotification($"{weathers[SelectedWeather]}\nReason: found blacklisted weather", "Weatherman: weather changed", NotificationType.Info, 5000);
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        memoryManager.DisableCustomWeather();
                    }
                    
                }
                else
                {
                    memoryManager.DisableCustomTime();
                    memoryManager.DisableCustomWeather();
                }
                if (profiling)
                {
                    stopwatch.Stop();
                    totalTime += stopwatch.ElapsedTicks;
                }
            }
            catch(Exception e)
            {
                WriteLog("Error in weatherman: "+e);
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

        void SetTimeBySetting(int setting)
        {
            if(setting == 0) //game managed
            {
                memoryManager.DisableCustomTime();
            }
            else if (setting == 1) //normal
            {
                memoryManager.EnableCustomTime();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if (setting == 2) //fixed
            {
                memoryManager.EnableCustomTime();
                uint et = (uint)GetZoneTimeFixedSetting(Svc.ClientState.TerritoryType);
                memoryManager.SetTime(et);
            }
            else if (setting == 3) //infiniday
            {
                memoryManager.EnableCustomTime();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay > 18 * 60 * 60 || timeOfDay < 6 * 60 * 60) et += SecondsInDay / 2;
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if (setting == 4) //infiniday r
            {
                memoryManager.EnableCustomTime();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay > 18 * 60 * 60) et -= 2 * (timeOfDay - 18 * 60 * 60);
                if (timeOfDay < 6 * 60 * 60) et += 2 * (6 * 60 * 60 - timeOfDay);
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if (setting == 5) //infininight
            {
                memoryManager.EnableCustomTime();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et += SecondsInDay / 2;
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if (setting == 6) //infininight r
            {
                memoryManager.EnableCustomTime();
                var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                var timeOfDay = et % SecondsInDay;
                if (timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et -= 2 * (timeOfDay - 6 * 60 * 60);
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if (setting == 7) //real world
            {
                memoryManager.EnableCustomTime();
                var now = DateTimeOffset.Now;
                var et = (now + now.Offset).ToUnixTimeSeconds();
                memoryManager.SetTime((uint)(et % SecondsInDay));
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
            foreach (var u in weatherRates.GetRow(zones[terr].WeatherRate).UnkData0)
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
                var file = Svc.Data.GetFile<LvbFile>($"bg/{path.Bg}.lvb");
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
