using ECommons;
using ECommons.ExcelServices;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman;
public static class Utils
{
    public static ushort[] WhitelistedTypes => [..Svc.Data.GetExcelSheet<TerritoryType>().Where(x => ((TerritoryIntendedUseEnum)x.TerritoryIntendedUse).EqualsAny([
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
