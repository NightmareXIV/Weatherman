using Weatherman.Services;

namespace Weatherman;

public unsafe partial class Weatherman
{
    private void SetTimeBySetting(int setting)
    {
        if(TimeOverride)
        {
            S.MemoryManager.EnableCustomTime();
            S.MemoryManager.SetTime((uint)TimeOverrideValue);
        }
        else
        {
            if(setting == 0) //game managed
            {
                S.MemoryManager.DisableCustomTime();
            }
            else if(setting == 1) //normal
            {
                S.MemoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % DataProvider.SecondsInDay * Config.TimeFlowSpeed);
                }
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
            else if(setting == 2) //fixed
            {
                S.MemoryManager.EnableCustomTime();
                var et = (uint)GetZoneTimeFixedSetting(Svc.ClientState.TerritoryType);
                S.MemoryManager.SetTime(et);
            }
            else if(setting == 3) //infiniday
            {
                S.MemoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % DataProvider.SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % DataProvider.SecondsInDay;
                if(timeOfDay > 18 * 60 * 60 || timeOfDay < 6 * 60 * 60) et += DataProvider.SecondsInDay / 2;
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
            else if(setting == 4) //infiniday r
            {
                S.MemoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % DataProvider.SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % DataProvider.SecondsInDay;
                if(timeOfDay > 18 * 60 * 60) et -= 2 * (timeOfDay - 18 * 60 * 60);
                if(timeOfDay < 6 * 60 * 60) et += 2 * (6 * 60 * 60 - timeOfDay);
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
            else if(setting == 5) //infininight
            {
                S.MemoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % DataProvider.SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % DataProvider.SecondsInDay;
                if(timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et += DataProvider.SecondsInDay / 2;
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
            else if(setting == 6) //infininight r
            {
                S.MemoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % DataProvider.SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % DataProvider.SecondsInDay;
                if(timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et -= 2 * (timeOfDay - 6 * 60 * 60);
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
            else if(setting == 7) //real world
            {
                S.MemoryManager.EnableCustomTime();
                var now = DateTimeOffset.Now;
                var offset = Config.UseGMTForRealTime ? TimeSpan.Zero : now.Offset;
                if(Config.Offset != 0)
                {
                    offset += TimeSpan.FromHours(Config.Offset);
                }
                var et = (now + offset).ToUnixTimeSeconds();
                S.MemoryManager.SetTime((uint)(et % DataProvider.SecondsInDay));
            }
        }
    }

    private int GetZoneTimeFlowSetting(ushort terr)
    {
        if(S.DataProvider.ZoneSettings.ContainsKey(terr))
        {
            if(S.DataProvider.ZoneSettings[terr].TimeFlow > 0) return S.DataProvider.ZoneSettings[terr].TimeFlow;
        }
        return Config.GlobalTimeFlowControl;
    }

    private int GetZoneTimeFixedSetting(ushort terr)
    {
        if(S.DataProvider.ZoneSettings.ContainsKey(terr))
        {
            if(S.DataProvider.ZoneSettings[terr].TimeFlow == 2) return S.DataProvider.ZoneSettings[terr].FixedTime;
        }
        return Config.GlobalFixedTime;
    }
}
