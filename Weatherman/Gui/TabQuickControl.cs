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
            var canModWeather = p.weatherAllowedZones.Contains(Svc.ClientState.TerritoryType);
            var canModTime = p.timeAllowedZones.Contains(Svc.ClientState.TerritoryType);
            if (canModWeather || canModTime)
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
                    if (canModTime)
                    {
                        ImGui.Checkbox("Time: ", ref p.TimeOverride);
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(150f);
                        var span = TimeSpan.FromSeconds(p.TimeOverrideValue);
                        var position = (int)MathF.Ceiling((float)p.TimeOverrideValue / 3600f);
                        if (ImGui.GetIO().KeyCtrl)
                        {
                            ImGui.Text("Right click me instead!");
                        }
                        else
                        {
                            if (ImGui.SliderInt("##customTime", ref position, 0, 24, $"{span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}"))
                            {
                                p.TimeOverrideValue = position * 3600;
                                if (p.TimeOverrideValue == Weatherman.SecondsInDay) p.TimeOverrideValue -= 1;
                                p.TimeOverride = true;
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            {
                                p.TimeOverride = true;
                                ImGui.OpenPopup("#PreciseTimeSet");
                            }
                        }
                        if (ImGui.BeginPopup("#PreciseTimeSet"))
                        {
                            var oSpan = TimeSpan.FromSeconds(p.TimeOverrideValue);
                            var s = oSpan.Seconds;
                            var m = oSpan.Minutes;
                            var h = oSpan.Hours;
                            ImGui.Text("Precise editing");
                            ImGui.Text("Hours:");
                            ImGui.SameLine(75f);
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt("##preciseH", ref h);
                            ValidateRange(ref h, 0, 23);
                            ImGui.Text("Minutes:");
                            ImGui.SameLine(75f);
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt("##precisem", ref m);
                            ValidateRange(ref m, -1, 60);
                            ImGui.Text("Seconds:");
                            ImGui.SameLine(75f);
                            ImGui.SetNextItemWidth(100f);
                            ImGui.InputInt("##precises", ref s);
                            ValidateRange(ref s, -1, 60);
                            p.TimeOverrideValue = h * 60 * 60 + m * 60 + s;
                            ImGui.EndPopup();
                        }
                        ValidateRange(ref p.TimeOverrideValue, 0, Weatherman.SecondsInDay-1);
                    }
                    if (canModWeather)
                    {
                        foreach (byte i in p.GetWeathers(Svc.ClientState.TerritoryType))
                        {
                            var colored = false;
                            if (p.IsWeatherNormal(i, Svc.ClientState.TerritoryType))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                                colored = true;
                            }
                            if (ImGui.RadioButton(String.IsNullOrWhiteSpace(p.weathers[i]) ? i.ToString() : p.weathers[i], p.SelectedWeather == i))
                            {
                                p.SelectedWeather = i;
                            }
                            if (colored) ImGui.PopStyleColor(1);
                        }
                        if (p.SelectedWeather != 255 && ImGui.Button("Reset weather##weather"))
                        {
                            p.SelectedWeather = 255;
                        }
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
