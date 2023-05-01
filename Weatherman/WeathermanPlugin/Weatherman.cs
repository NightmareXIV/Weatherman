using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Weatherman
{
    unsafe partial class Weatherman : IDalamudPlugin
    {
        internal const int SecondsInDay = 60 * 60 * 24;
        internal static double ETMult = 144D / 7D;
        internal static bool Init = false;

        public string Name => "Weatherman";
        internal MemoryManager memoryManager;
        internal OrchestrionController orchestrionController;
        internal Gui ConfigGui;
        internal byte WeatherTestActive = 255;
        internal Dictionary<ushort, TerritoryType> zones;
        internal Dictionary<ushort, (List<byte> WeatherList, string EnvbFile)> weatherList = new();
        internal HashSet<ushort> weatherAllowedZones = new()
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
        internal HashSet<ushort> timeAllowedZones = new()
        {
            163, 367, //qarn
            158, 362, //brayfox
            623, //bardam

        };
        internal Dictionary<byte, string> weathers;
        internal ExcelSheet<WeatherRate> weatherRates;
        internal Dictionary<ushort, ZoneSettings> ZoneSettings;
        internal ClockOffNag clockOffNag;
        internal Configuration configuration;
        internal byte SelectedWeather = 255;
        internal byte UnblacklistedWeather = 0;
        internal bool PausePlugin = false;
        internal Stopwatch stopwatch;
        internal long totalTime = 0;
        internal long totalTicks = 0;
        internal bool profiling = false;
        internal bool InCutscene = false;

        internal bool TimeOverride = false;
        internal int TimeOverrideValue = 0;

        public Weatherman(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            new TickScheduler(delegate
            {
                PluginLog.Verbose($"Weatherman boot begins");
                stopwatch = new();
                PluginLog.Verbose($"Registering orchestrion controller");
                orchestrionController = new(this);
                PluginLog.Verbose($"Registering memory manager");
                memoryManager = new(this);
                PluginLog.Verbose($"Populating zones");
                zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
                weatherAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.Mount && !x.IsPvpZone).Select(x => (ushort)x.RowId));
                weatherAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.TerritoryIntendedUse == 14).Select(x => (ushort)x.RowId));
                PluginLog.Verbose($"Populating zones 2");
                timeAllowedZones.UnionWith(weatherAllowedZones);
                PluginLog.Verbose($"Populating zones 3");
                timeAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.QuestBattle.Value.RowId != 0 && !x.IsPvpZone).Select(x => (ushort)x.RowId));
                PluginLog.Verbose($"Populating weathers");
                weathers = Svc.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
                PluginLog.Verbose($"Populating weathers 2");
                weatherRates = Svc.Data.GetExcelSheet<WeatherRate>();
                ZoneSettings = new();
                PluginLog.Verbose($"Populating zone settings");
                foreach (var z in zones)
                {
                    var v = ParseLvb(z.Key);
                    weatherList[z.Key] = (v.WeatherList, v.EnvbFile);
                    var s = new ZoneSettings
                    {
                        ZoneId = z.Key,
                        ZoneName = z.Value.PlaceName.Value.Name,
                        terr = z.Value
                    };
                    s.Init(this);
                    ZoneSettings.Add(s.ZoneId, s);
                }
                PluginLog.Verbose($"Loading configuration");
                configuration = pluginInterface.GetPluginConfig() as Configuration ?? new();
                configuration.Initialize(this);
                var normalweathers = new HashSet<byte>();
                PluginLog.Verbose($"Loading normal weathers");
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
                PluginLog.Verbose($"Loading blacklisted weathers");
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
                PluginLog.Verbose($"Registering events");
                Svc.Framework.Update += HandleFrameworkUpdate;
                ConfigGui = new(this);
                Svc.PluginInterface.UiBuilder.Draw += ConfigGui.Draw;
                Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigGui.configOpen = !ConfigGui.configOpen ? true : false; };
                Svc.ClientState.TerritoryChanged += HandleZoneChange;
                PluginLog.Verbose($"Applying weather changes");
                ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                Svc.Commands.AddHandler("/weatherman", new CommandInfo(delegate { ConfigGui.configOpen = !ConfigGui.configOpen ? true : false; }) { HelpMessage = "Toggle plugin settings" });
                /*if (ChlogGui.ChlogVersion > configuration.ChlogReadVer)
                {
                    new ChlogGui(this);
                }*/
                Svc.ClientState.Logout += StopSongIfModified;
                PluginLog.Verbose($"Checking clock");
                clockOffNag = new(this);
                Svc.PluginInterface.UiBuilder.DisableGposeUiHide = configuration.DisplayInGpose;
                Init = true;
                PluginLog.Verbose($"Weatherman boot ends");
            }, Svc.Framework);
        }

        public void Dispose()
        {
            configuration.Save();
            Svc.Framework.Update -= HandleFrameworkUpdate;
            Svc.ClientState.Logout -= StopSongIfModified;
            Svc.PluginInterface.UiBuilder.Draw -= ConfigGui.Draw;
            Svc.ClientState.TerritoryChanged -= HandleZoneChange;
            Svc.Commands.RemoveHandler("/weatherman");
            memoryManager.Dispose();
            clockOffNag.Dispose();
            StopSongIfModified();
            orchestrionController.Dispose();
        }

        void StopSongIfModified(object _ = null, object __ = null)
        {
            if (orchestrionController.BGMModified)
            {
                orchestrionController.StopSong();
                orchestrionController.BGMModified = false;
            }
        }
        
        internal bool CanModifyTime()
        {
            return configuration.EnableTimeControl && timeAllowedZones.Contains(Svc.ClientState.TerritoryType);
        }

        internal bool CanModifyWeather()
        {
            return configuration.EnableWeatherControl && weatherAllowedZones.Contains(Svc.ClientState.TerritoryType);
        }

        public bool IsWeatherNormal(byte id, ushort terr)
        {
            foreach (var u in weatherRates.GetRow(zones[terr].WeatherRate).UnkData0)
            {
                if (u.Weather != 0 && u.Weather == id) return true;
            }
            return false;
        }

        public List<byte> GetWeathers(ushort id)
        {
            return weatherList[id].WeatherList;
        }

        (List<byte> WeatherList, string EnvbFile) ParseLvb(ushort id) //from titleedit https://github.com/lmcintyre/TitleEditPlugin
        {
            var weathers = new List<byte>();
            if (!zones.TryGetValue(id, out var territoryType)) return (null, null);
            try
            {
                var file = Svc.Data.GetFile<LvbFile>($"bg/{territoryType.Bg}.lvb");
                if (file?.weatherIds == null || file.weatherIds.Length == 0)
                    return (null, null);
                foreach (var weather in file.weatherIds)
                    if (weather > 0 && weather < 255)
                        weathers.Add((byte)weather);
                weathers.Sort();
                return (weathers, file.envbFile);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Failed to load lvb for {territoryType}");
            }
            return (null, null);
        }
    }
}
