using Lumina.Excel.Sheets;

namespace Weatherman
{
    internal class ZoneSettings
    {
        public ushort ZoneId;
        public string ZoneName;
        public List<WeathermanWeather> SupportedWeathers;
        public bool WeatherControl = false;
        public int TimeFlow = 0;
        public int FixedTime = 0;
        public TerritoryType terr;
        public int Music = 0;

        public ZoneSettings() { }

        public void Init(Weatherman plugin)
        {
            SupportedWeathers = [];
            if(plugin.GetWeathers(ZoneId) != null) foreach(var w in plugin.GetWeathers(ZoneId))
                {
                    SupportedWeathers.Add(new WeathermanWeather(w, false, plugin.IsWeatherNormal(w, ZoneId)));
                }
        }

        public string GetString()
        {
            var b = new List<string>
            {
                WeatherControl.ToString(),
                TimeFlow.ToString(),
                FixedTime.ToString()
            };
            if(IsUntouched()) return null;
            var sel = new List<string>();
            foreach(var z in SupportedWeathers)
            {
                if(z.Selected) sel.Add(z.Id.ToString());
            }
            b.Add(string.Join("|", sel));
            b.Add(Music.ToString());
            return string.Join("/", b);
        }

        public void FromString(string s)
        {
            var ss = s.Split('/');
            WeatherControl = bool.Parse(ss[0]);
            TimeFlow = int.Parse(ss[1]);
            FixedTime = int.Parse(ss[2]);
            var selectedw = ss[3].Split('|');
            Music = ss.Length < 5 ? 0 : int.Parse(ss[4]);
            foreach(var i in selectedw)
            {
                var ii = byte.Parse(i);
                foreach(var z in SupportedWeathers)
                {
                    if(z.Id == ii) z.Selected = true;
                }
            }
        }
        public bool IsUntouched()
        {
            var sel = new List<string>();
            foreach(var z in SupportedWeathers)
            {
                if(z.Selected) sel.Add(z.Id.ToString());
            }
            return WeatherControl == false && TimeFlow == 0 && FixedTime == 0 && sel.Count == 0 && Music == 0;
        }
    }
}
