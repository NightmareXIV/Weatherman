using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    internal partial class Weatherman
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
                stopwatch = new();
                orchestrionController = new(this);
                memoryManager = new(this);
                zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
                weatherAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.Mount).Select(x => (ushort)x.RowId));
                timeAllowedZones.UnionWith(weatherAllowedZones);
                timeAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.QuestBattle.Value.RowId != 0).Select(x => (ushort)x.RowId));
                weathers = Svc.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
                weatherRates = Svc.Data.GetExcelSheet<WeatherRate>();
                ZoneSettings = new();
                foreach (var z in zones)
                {
                    var s = new ZoneSettings
                    {
                        ZoneId = z.Key,
                        ZoneName = z.Value.PlaceName.Value.Name,
                        terr = z.Value
                    };
                    s.Init(this);
                    ZoneSettings.Add(s.ZoneId, s);
                }
                configuration = pluginInterface.GetPluginConfig() as Configuration ?? new();
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
                ConfigGui = new(this);
                Svc.PluginInterface.UiBuilder.Draw += ConfigGui.Draw;
                Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigGui.configOpen = true; };
                Svc.ClientState.TerritoryChanged += HandleZoneChange;
                ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                Svc.Commands.AddHandler("/weatherman", new CommandInfo(delegate { ConfigGui.configOpen = true; }) { HelpMessage = "Open plugin settings" });
                if (ChlogGui.ChlogVersion > configuration.ChlogReadVer)
                {
                    new ChlogGui(this);
                }
                Svc.ClientState.Logout += StopSongIfModified;
                clockOffNag = new(this);
                Init = true;
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
    }
}
