using Dalamud.Game.Command;
using Dalamud.Plugin;
using ECommons;
using ECommons.ExcelServices;
using ECommons.Funding;
using ECommons.ImGuiMethods;
using ECommons.Singletons;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Weatherman.Services;

namespace Weatherman;

public unsafe partial class Weatherman : IDalamudPlugin
{
    internal static bool Init = false;
    public static Configuration C => P.Config;

    public string Name => "Weatherman";
    public static Weatherman P;
    internal OrchestrionController orchestrionController;
    internal Gui ConfigGui;
    internal byte WeatherTestActive = 255;
    internal ClockOffNag clockOffNag;
    internal Configuration Config;
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

    public Weatherman(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, this);
        PatreonBanner.IsOfficialPlugin = () => true;
        
        new TickScheduler(delegate
            {
                stopwatch = new();
                orchestrionController = new(this);
                
                PluginLog.Verbose($"Loading configuration");
                Config = pluginInterface.GetPluginConfig() as Configuration ?? new();
                Config.Initialize();
                SingletonServiceManager.Initialize(typeof(S));
                Svc.Framework.Update += HandleFrameworkUpdate;
                ConfigGui = new(this);
                Svc.PluginInterface.UiBuilder.Draw += ConfigGui.Draw;
                Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigGui.configOpen = !ConfigGui.configOpen ? true : false; };
                Svc.ClientState.TerritoryChanged += HandleZoneChange;
                ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                Svc.Commands.AddHandler("/weatherman", new CommandInfo(delegate { ConfigGui.configOpen = !ConfigGui.configOpen ? true : false; }) { HelpMessage = "Toggle plugin settings" });
                Svc.ClientState.Logout += StopSongIfModified;
                clockOffNag = new(this);
                Svc.PluginInterface.UiBuilder.DisableGposeUiHide = Config.DisplayInGpose;
                Init = true;
            }, Svc.Framework);
    }

    public void Dispose()
    {
        Config.Save();
        Svc.Framework.Update -= HandleFrameworkUpdate;
        Svc.ClientState.Logout -= StopSongIfModified;
        Svc.PluginInterface.UiBuilder.Draw -= ConfigGui.Draw;
        Svc.ClientState.TerritoryChanged -= HandleZoneChange;
        Svc.Commands.RemoveHandler("/weatherman");
        clockOffNag.Dispose();
        StopSongIfModified(0, 0);
        orchestrionController.Dispose();
        ECommonsMain.Dispose();
        P = null;
    }

    private void StopSongIfModified(int a, int b)
    {
        if(orchestrionController.BGMModified)
        {
            orchestrionController.StopSong();
            orchestrionController.BGMModified = false;
        }
    }

    internal bool CanModifyTime()
    {
        return Config.EnableTimeControl && S.DataProvider.TimeAllowedZones.Contains(Svc.ClientState.TerritoryType);
    }

    internal bool CanModifyWeather()
    {
        return Config.EnableWeatherControl && S.DataProvider.WeatherAllowedZones.Contains(Svc.ClientState.TerritoryType);
    }
}
