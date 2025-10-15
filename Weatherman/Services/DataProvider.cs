using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices.TerritoryEnumeration;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Weatherman.Services;
public sealed class DataProvider
{
    public const int SecondsInDay = 60 * 60 * 24;
    public static double ETMult = 144D / 7D;
    public uint[][] ZoneToWeatherIndexMap = [];
    public Dictionary<ushort, TerritoryType> Zones;
    public Dictionary<ushort, (List<byte> WeatherList, string EnvbFile)> WeatherList = [];
    public HashSet<ushort> WeatherAllowedZones;
    public HashSet<ushort> TimeAllowedZones;
    public Dictionary<byte, string> Weathers;
    public ExcelSheet<WeatherRate> WeatherRates;
    public Dictionary<ushort, ZoneSettings> ZoneSettings;

    private DataProvider()
    {
        WeatherAllowedZones = [
           ..Utils.WhitelistedTypes,
         ];
        TimeAllowedZones = [
            ..Utils.WhitelistedTypes,
            163, 367, //qarn
            158, 362, //brayfox
            623, //bardam
            Dungeons.Cutters_Cry,
        ];
        Zones = Svc.Data.GetExcelSheet<TerritoryType>().ToDictionary(row => (ushort)row.RowId, row => row);
        WeatherAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.Mount && !x.IsPvpZone).Select(x => (ushort)x.RowId));
        WeatherAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.TerritoryIntendedUse.RowId == 14).Select(x => (ushort)x.RowId));
        TimeAllowedZones.UnionWith(WeatherAllowedZones);
        TimeAllowedZones.UnionWith(Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.QuestBattle.Value.RowId != 0 && !x.IsPvpZone).Select(x => (ushort)x.RowId));
        Weathers = Svc.Data.GetExcelSheet<Weather>().ToDictionary(row => (byte)row.RowId, row => row.Name.ToString());
        WeatherRates = Svc.Data.GetExcelSheet<WeatherRate>();
        ZoneSettings = [];
        foreach(var z in Zones)
        {
            var v = Utils.ParseLvb(z.Key);
            WeatherList[z.Key] = (v.WeatherList, v.EnvbFile);
            var s = new ZoneSettings
            {
                ZoneId = z.Key,
                ZoneName = z.Value.PlaceName.Value.Name.ToString(),
                terr = z.Value,
                SupportedWeathers = []
            };
            if(GetWeathers(s.ZoneId) != null) 
                foreach(var w in GetWeathers(s.ZoneId))
                {
                    s.SupportedWeathers.Add(new WeathermanWeather(w, false, IsWeatherNormal(w, s.ZoneId)));
                }
            ZoneSettings.Add(s.ZoneId, s);
        }
        var normalweathers = new HashSet<byte>();
        foreach(var z in ZoneSettings)
        {
            foreach(var a in z.Value.SupportedWeathers)
            {
                if(a.IsNormal)
                {
                    normalweathers.Add(a.Id);
                }
            }
        }
        var tempdict = new Dictionary<byte, bool>(P.Config.BlacklistedWeathers);
        foreach(var i in tempdict)
        {
            if(!normalweathers.Contains(i.Key))
            {
                P.Config.BlacklistedWeathers.Remove(i.Key);
            }
        }
        foreach(var i in normalweathers)
        {
            if(!P.Config.BlacklistedWeathers.ContainsKey(i)) P.Config.BlacklistedWeathers.Add(i, false);
        }
        ZoneToWeatherIndexMap = new uint[ZoneSettings.Max(x => x.Value.ZoneId) + 1][];
        foreach(var x in ZoneSettings)
        {
            ZoneToWeatherIndexMap[x.Key] = new uint[255];
            for(var i = 0; i < x.Value.SupportedWeathers.Count; i++)
            {
                var w = x.Value.SupportedWeathers[i];
                ZoneToWeatherIndexMap[x.Key][w.Id] = (uint)i;
            }
        }
    }

    public List<byte> GetWeathers(ushort id)
    {
        return WeatherList[id].WeatherList;
    }

    public bool IsWeatherNormal(byte id, ushort terr)
    {
        if(!Zones.TryGetValue(terr, out var value)) return false;
        foreach(var u in WeatherRates.GetRow(value.WeatherRate.RowId).Weather)
        {
            if(u.RowId != 0 && u.RowId == id) return true;
        }
        return false;
    }
}