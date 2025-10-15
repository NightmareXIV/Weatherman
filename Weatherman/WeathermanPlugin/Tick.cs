using Dalamud.Interface.ImGuiNotification;
using ECommons;
using Weatherman.Services;

namespace Weatherman;

public unsafe partial class Weatherman
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
                    S.MemoryManager.DisableCustomTime();
                }
                if(CanModifyWeather())
                {
                    if(SelectedWeather != 255)
                    {
                        S.MemoryManager.EnableCustomWeather();
                        if(S.MemoryManager.GetCustomWeather() != SelectedWeather)
                        {
                            S.MemoryManager.SetWeather(SelectedWeather);
                            if(Config.DisplayNotifications)
                            {
                                Svc.PluginInterface.UiBuilder.AddNotification($"{S.DataProvider.Weathers[SelectedWeather]}\nReason: selected by user", "Weatherman: weather changed", NotificationType.Info, 5000);
                            }
                        }
                    }
                    else
                    {
                        var suggesterWeather = *S.MemoryManager.TrueWeather;
                        if(UnblacklistedWeather != 0 && suggesterWeather != UnblacklistedWeather
                        && Config.BlacklistedWeathers.TryGetValue(suggesterWeather, out var value)
                        && value && Config.BlacklistCS.EqualsAny(null, Utils.IsPlayerWatchingCutscene()))
                        {
                            suggesterWeather = UnblacklistedWeather;
                        }
                        //this is to retain smooth transitions
                        if(suggesterWeather == *S.MemoryManager.TrueWeather)
                        {
                            S.MemoryManager.DisableCustomWeather();
                        }
                        else
                        {
                            S.MemoryManager.EnableCustomWeather();
                            if(S.MemoryManager.GetCustomWeather() != suggesterWeather)
                            {
                                S.MemoryManager.SetWeather(suggesterWeather);
                                if(Config.DisplayNotifications)
                                {
                                    Svc.PluginInterface.UiBuilder.AddNotification($"{S.DataProvider.Weathers[SelectedWeather]}\nReason: found blacklisted weather", "Weatherman: weather changed", NotificationType.Info, 5000);
                                }
                            }
                        }

                    }
                }
                else
                {
                    S.MemoryManager.DisableCustomWeather();
                }

            }
            else
            {
                S.MemoryManager.DisableCustomTime();
                S.MemoryManager.DisableCustomWeather();
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
