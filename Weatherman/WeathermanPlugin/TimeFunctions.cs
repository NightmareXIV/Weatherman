namespace Weatherman;

internal unsafe partial class Weatherman
{
    private void SetTimeBySetting(int setting)
    {
        if(TimeOverride)
        {
            memoryManager.EnableCustomTime();
            memoryManager.SetTime((uint)TimeOverrideValue);
        }
        else
        {
            if(setting == 0) //game managed
            {
                memoryManager.DisableCustomTime();
            }
            else if(setting == 1) //normal
            {
                memoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % SecondsInDay * Config.TimeFlowSpeed);
                }
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if(setting == 2) //fixed
            {
                memoryManager.EnableCustomTime();
                var et = (uint)GetZoneTimeFixedSetting(Svc.ClientState.TerritoryType);
                memoryManager.SetTime(et);
            }
            else if(setting == 3) //infiniday
            {
                memoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % SecondsInDay;
                if(timeOfDay > 18 * 60 * 60 || timeOfDay < 6 * 60 * 60) et += SecondsInDay / 2;
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if(setting == 4) //infiniday r
            {
                memoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % SecondsInDay;
                if(timeOfDay > 18 * 60 * 60) et -= 2 * (timeOfDay - 18 * 60 * 60);
                if(timeOfDay < 6 * 60 * 60) et += 2 * (6 * 60 * 60 - timeOfDay);
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if(setting == 5) //infininight
            {
                memoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % SecondsInDay;
                if(timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et += SecondsInDay / 2;
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if(setting == 6) //infininight r
            {
                memoryManager.EnableCustomTime();
                var et = GetET();
                if(Config.ChangeTimeFlowSpeed)
                {
                    et = (long)(et % SecondsInDay * Config.TimeFlowSpeed);
                }
                var timeOfDay = et % SecondsInDay;
                if(timeOfDay < 18 * 60 * 60 && timeOfDay > 6 * 60 * 60) et -= 2 * (timeOfDay - 6 * 60 * 60);
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
            else if(setting == 7) //real world
            {
                memoryManager.EnableCustomTime();
                var now = DateTimeOffset.Now;
                var offset = Config.UseGMTForRealTime ? TimeSpan.Zero : now.Offset;
                if(Config.Offset != 0)
                {
                    offset += TimeSpan.FromHours(Config.Offset);
                }
                var et = (now + offset).ToUnixTimeSeconds();
                memoryManager.SetTime((uint)(et % SecondsInDay));
            }
        }
    }

    private int GetZoneTimeFlowSetting(ushort terr)
    {
        if(ZoneSettings.ContainsKey(terr))
        {
            if(ZoneSettings[terr].TimeFlow > 0) return ZoneSettings[terr].TimeFlow;
        }
        return Config.GlobalTimeFlowControl;
    }

    private int GetZoneTimeFixedSetting(ushort terr)
    {
        if(ZoneSettings.ContainsKey(terr))
        {
            if(ZoneSettings[terr].TimeFlow == 2) return ZoneSettings[terr].FixedTime;
        }
        return Config.GlobalFixedTime;
    }
}
