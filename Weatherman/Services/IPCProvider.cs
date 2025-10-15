using ECommons.EzIpcManager;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman.Services;
public class IPCProvider
{
    private IPCProvider()
    {
        EzIPC.Init(this);
    }

    [EzIPC]
    public bool IsPluginEnabled()
    {
        return !P.PausePlugin;
    }

    [EzIPC]
    public bool IsTimeCustom()
    {
        return S.MemoryManager.IsTimeCustom();
    }

    [EzIPC]
    public bool IsWeatherCustom()
    {
        return S.MemoryManager.IsWeatherCustom();
    }

    [EzIPC]
    public bool SetTime(uint newValue)
    {
        return S.MemoryManager.SetTime(newValue);
    }

    [EzIPC]
    public bool SetWeather(byte newWeather)
    {
        return S.MemoryManager.SetWeather(newWeather);
    }

    [EzIPC] 
    public uint[][] DataGetZoneToWeatherIndexMap()
    {
        return S.DataProvider.ZoneToWeatherIndexMap;
    }

    [EzIPC]
    public Dictionary<ushort, TerritoryType> DataGetZones()
    {
        return S.DataProvider.Zones;
    }

    [EzIPC]
    public Dictionary<ushort, (List<byte> WeatherList, string EnvbFile)> DataGetWeatherList()
    {
        return S.DataProvider.WeatherList;
    }

    [EzIPC]
    public HashSet<ushort> DataGetWeatherAllowedZones()
    {
        return S.DataProvider.WeatherAllowedZones;
    }

    [EzIPC]
    public HashSet<ushort> DataGetTimeAllowedZones()
    {
        return S.DataProvider.TimeAllowedZones;
    }

    [EzIPC]
    public Dictionary<byte, string> DataGetWeathers()
    {
        return S.DataProvider.Weathers;
    }

    [EzIPC]
    public Dictionary<ushort, ZoneSettings> DataGetZoneSettings()
    {
        return S.DataProvider.ZoneSettings;
    }
}
