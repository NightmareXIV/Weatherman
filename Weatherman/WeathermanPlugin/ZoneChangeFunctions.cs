using Weatherman.Services;

namespace Weatherman;

public partial class Weatherman
{
    private void HandleZoneChange(ushort u)
    {
        PluginLog.Debug("Zone changed to " + u + "; time mod allowed=" + CanModifyTime() + ", weather mod allowed=" + CanModifyWeather());
        ApplyWeatherChanges(u);
    }

    public void ApplyWeatherChanges(ushort u)
    {
        try
        {
            PluginLog.Debug("Applying weather changes");
            TimeOverride = false;
            SelectedWeather = 255;
            UnblacklistedWeather = 0;
            StopSongIfModified(0, 0);
            if(S.DataProvider.ZoneSettings.TryGetValue(u, out var z))
            {
                if(Config.MusicEnabled && z.Music != 0 && !orchestrionController.BGMModified)
                {
                    orchestrionController.PlaySong(z.Music);
                    orchestrionController.BGMModified = true;
                }
                if(z.WeatherControl)
                {
                    var weathers = new List<byte>();
                    foreach(var v in z.SupportedWeathers)
                    {
                        if(v.Selected) weathers.Add(v.Id);
                    }
                    if(weathers.Count > 0)
                    {
                        SelectedWeather = weathers[new Random().Next(0, weathers.Count)];
                    }
                    else
                    {
                        //
                    }
                }
                else
                {
                    var unblacklistedWeatherCandidates = new List<byte>();
                    foreach(var v in z.SupportedWeathers)
                    {
                        if(Config.BlacklistedWeathers.ContainsKey(v.Id)
                            && !Config.BlacklistedWeathers[v.Id]
                            && S.DataProvider.IsWeatherNormal(v.Id, Svc.ClientState.TerritoryType))
                        {
                            unblacklistedWeatherCandidates.Add(v.Id);
                        }
                    }
                    if(unblacklistedWeatherCandidates.Count > 0)
                    {
                        UnblacklistedWeather =
                             unblacklistedWeatherCandidates[new Random().Next(0, unblacklistedWeatherCandidates.Count)];
                    }
                }
            }
            PluginLog.Debug("Selected weather:" + SelectedWeather + "; unblacklisted weather: " + UnblacklistedWeather);

        }
        catch(Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    internal long GetET()
    {
        return (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * DataProvider.ETMult / 1000D);
    }
}
