using Dalamud.Interface.ImGuiNotification;
using ECommons;

namespace Weatherman;

internal unsafe partial class Weatherman
{
    private void HandleFrameworkUpdate(object f)
    {
        try
        {
            if(profiling)
            {
                totalTicks++;
                stopwatch.Restart();
            }
            if(Utils.IsPlayerWatchingCutscene())
            {
                if(!InCutscene)
                {
                    PluginLog.Debug("Cutscene started");
                    InCutscene = true;
                    if(Config.DisableInCutscene)
                    {
                        StopSongIfModified(0, 0);
                    }
                }
            }
            else
            {
                if(InCutscene)
                {
                    PluginLog.Debug("Cutscene ended");
                    InCutscene = false;
                    ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                }
            }
            if(Svc.ClientState.LocalPlayer != null
                && !PausePlugin
                && !(Config.DisableInCutscene && InCutscene))
            {
                if(CanModifyTime())
                {
                    SetTimeBySetting(GetZoneTimeFlowSetting(Svc.ClientState.TerritoryType));
                }
                else
                {
                    memoryManager.DisableCustomTime();
                }
                if(CanModifyWeather())
                {
                    if(SelectedWeather != 255)
                    {
                        memoryManager.EnableCustomWeather();
                        if(memoryManager.GetWeather() != SelectedWeather)
                        {
                            memoryManager.SetWeather(SelectedWeather);
                            if(Config.DisplayNotifications)
                            {
                                Svc.PluginInterface.UiBuilder.AddNotification($"{weathers[SelectedWeather]}\nReason: selected by user", "Weatherman: weather changed", NotificationType.Info, 5000);
                            }
                        }
                    }
                    else
                    {
                        var suggesterWeather = *memoryManager.TrueWeather;
                        if(UnblacklistedWeather != 0 && suggesterWeather != UnblacklistedWeather
                        && Config.BlacklistedWeathers.TryGetValue(suggesterWeather, out var value)
                        && value && Config.BlacklistCS.EqualsAny(null, Utils.IsPlayerWatchingCutscene()))
                        {
                            suggesterWeather = UnblacklistedWeather;
                        }
                        //this is to retain smooth transitions
                        if(suggesterWeather == *memoryManager.TrueWeather)
                        {
                            memoryManager.DisableCustomWeather();
                        }
                        else
                        {
                            memoryManager.EnableCustomWeather();
                            if(memoryManager.GetWeather() != suggesterWeather)
                            {
                                memoryManager.SetWeather(suggesterWeather);
                                if(Config.DisplayNotifications)
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
            if(profiling)
            {
                stopwatch.Stop();
                totalTime += stopwatch.ElapsedTicks;
            }
        }
        catch(Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }
}
