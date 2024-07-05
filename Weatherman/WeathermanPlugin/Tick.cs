using Dalamud.Interface.ImGuiNotification;

namespace Weatherman
{
    internal unsafe partial class Weatherman
    {
        void HandleFrameworkUpdate(object f)
        {
            try
            {
                if (profiling)
                {
                    totalTicks++;
                    stopwatch.Restart();
                }
                if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78])
                {
                    if (!InCutscene)
                    {
                        PluginLog.Debug("Cutscene started");
                        InCutscene = true;
                        if (configuration.DisableInCutscene)
                        {
                            StopSongIfModified();
                        }
                    }
                }
                else
                {
                    if (InCutscene)
                    {
                        PluginLog.Debug("Cutscene ended");
                        InCutscene = false;
                        ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                    }
                }
                if (Svc.ClientState.LocalPlayer != null
                    && !PausePlugin
                    && !(configuration.DisableInCutscene && InCutscene))
                {
                    if (CanModifyTime())
                    {
                        SetTimeBySetting(GetZoneTimeFlowSetting(Svc.ClientState.TerritoryType));
                    }
                    else
                    {
                        memoryManager.DisableCustomTime();
                    }
                    if (CanModifyWeather())
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
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
            }
        }
    }
}
