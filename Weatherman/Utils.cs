using ECommons;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

namespace Weatherman;
public static class Utils
{
    public static (List<byte> WeatherList, string EnvbFile) ParseLvb(ushort id) //from titleedit https://github.com/lmcintyre/TitleEditPlugin
    {
        var weathers = new List<byte>();
        try
        {
            var file = Svc.Data.GetFile<LvbFile>($"bg/{Svc.Data.GetExcelSheet<TerritoryType>().GetRow(id).Bg}.lvb");
            if(file?.weatherIds == null || file.weatherIds.Length == 0)
                return (null, null);
            foreach(var weather in file.weatherIds)
                if(weather > 0 && weather < 255)
                    weathers.Add((byte)weather);
            weathers.Sort();
            return (weathers, file.envbFile);
        }
        catch(Exception e)
        {
            PluginLog.Error($"Failed to load lvb for {id}\n{e}");
        }
        return (null, null);
    }

    public static bool IsPlayerWatchingCutscene() => Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78];

    public static ushort[] WhitelistedTypes => [..Svc.Data.GetExcelSheet<TerritoryType>().Where(x => ((TerritoryIntendedUseEnum)x.TerritoryIntendedUse.RowId).EqualsAny([
                TerritoryIntendedUseEnum.Open_World,
                TerritoryIntendedUseEnum.City_Area,
                TerritoryIntendedUseEnum.Bozja,
                TerritoryIntendedUseEnum.Eureka,
                TerritoryIntendedUseEnum.Quest_Area,
                TerritoryIntendedUseEnum.Quest_Area_2,
                TerritoryIntendedUseEnum.Quest_Area_3,
                TerritoryIntendedUseEnum.Quest_Area_4,
                TerritoryIntendedUseEnum.Quest_Battle,
                TerritoryIntendedUseEnum.Quest_Battle_2,
                TerritoryIntendedUseEnum.Residential_Area,
                TerritoryIntendedUseEnum.Housing_Instances,
                TerritoryIntendedUseEnum.Diadem,
                TerritoryIntendedUseEnum.Diadem_2,
                TerritoryIntendedUseEnum.Diadem_3,
                TerritoryIntendedUseEnum.Inn,
                TerritoryIntendedUseEnum.Island_Sanctuary,
                ])).Select(x => (ushort)x.RowId)];
}
