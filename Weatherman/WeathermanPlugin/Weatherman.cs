using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Weatherman
{
    unsafe partial class Weatherman : IDalamudPlugin
    {
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

        public List<byte> GetWeathers(ushort id) //from titleedit https://github.com/lmcintyre/TitleEditPlugin
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
