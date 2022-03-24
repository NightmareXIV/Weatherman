using Dalamud.Interface.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    internal unsafe partial class Gui
    {
        void DrawTabQuickControl()
        {
            if (p.IsWorldTerritory(Svc.ClientState.TerritoryType))
            {
                if (Svc.Condition[ConditionFlag.WatchingCutscene])
                {
                    ImGui.TextWrapped("Disable \"Stop Time/Weather\" in gpose to control them with Weatherman.");
                }
                ImGui.Text("Time: ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderInt("##customTime", ref p.TimeOverrideValue, 0, Weatherman.SecondsInDay))
                {
                    p.TimeOverride = true;
                }
                if (p.TimeOverrideValue < 0 || p.TimeOverrideValue > Weatherman.SecondsInDay) p.TimeOverrideValue = 0;
                ImGui.SameLine();
                ImGui.Checkbox("Enable", ref p.TimeOverride);
                foreach (byte i in p.GetWeathers(Svc.ClientState.TerritoryType))
                {
                    var colored = false;
                    if (p.IsWeatherNormal(i, Svc.ClientState.TerritoryType))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                        colored = true;
                    }
                    if (ImGui.RadioButton(p.weathers[i], p.SelectedWeather == i))
                    {
                        p.SelectedWeather = i;
                    }
                    if (colored) ImGui.PopStyleColor(1);
                }
                if (p.SelectedWeather != 255 && ImGui.SmallButton("Reset##weather"))
                {
                    p.SelectedWeather = 255;
                }
            }
            else
            {
                ImGui.Text("Can't use now");
            }
        }
    }
}
