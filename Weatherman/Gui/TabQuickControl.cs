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
                ImGui.Checkbox("Pause Weatherman", ref p.PausePlugin);
                if (!p.PausePlugin)
                {
                    if (Svc.Condition[ConditionFlag.WatchingCutscene])
                    {
                        ImGui.TextWrapped("Disable \"Stop Time/Weather\" in gpose to control them with Weatherman.");
                    }
                    else
                    {
                        ImGui.TextWrapped("These controls will allow you to temporarily adjust weather and time. They are reset on zone change.");
                    }
                    ImGui.Checkbox("Time: ", ref p.TimeOverride);
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    var span = TimeSpan.FromSeconds(p.TimeOverrideValue);
                    if (ImGui.SliderInt("##customTime", ref p.TimeOverrideValue, 0, Weatherman.SecondsInDay - 1, ImGui.GetIO().KeyCtrl ? $"{p.TimeOverrideValue}" : $"{span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}"))
                    {
                        p.TimeOverride = true;
                    }
                    if (p.TimeOverrideValue < 0 || p.TimeOverrideValue > Weatherman.SecondsInDay) p.TimeOverrideValue = 0;
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
                    if (p.SelectedWeather != 255 && ImGui.Button("Reset weather##weather"))
                    {
                        p.SelectedWeather = 255;
                    }
                    if (ImGui.Button("Reload zone settings"))
                    {
                        p.ApplyWeatherChanges(Svc.ClientState.TerritoryType);
                    }
                }
            }
            else
            {
                ImGui.Text("Can't use now");
            }
        }
    }
}
