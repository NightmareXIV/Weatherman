using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Weatherman
{
    unsafe class Gui
    {
        private Weatherman plugin;
        private int curW = 0;
        private Vector4 colorGreen = new Vector4(0,1,0,1);
        public bool configOpen = false;

        public Gui(Weatherman plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (!configOpen) return;
            if (!plugin.configuration.ConfigurationString.Equals(plugin.GetConfigurationString()))
            {
                plugin.configuration.Save();
                plugin._pi.Framework.Gui.Chat.Print("Configuration saved");
            }
            if (ImGui.Begin("Weatherman configuration", ref configOpen))
            {
                ImGui.BeginTabBar("weatherman_settings");
                if (ImGui.BeginTabItem("Global settings"))
                {
                    ImGui.Text("This will be global settings");
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Zone-specific settings"))
                {
                    ImGui.BeginChild("##zonetable");
                    ImGui.Columns(3);
                    ImGui.Text("Zone id and name");
                    ImGui.NextColumn();
                    ImGui.Text("Time control");
                    ImGui.NextColumn();
                    ImGui.Text("Weather control");
                    ImGui.NextColumn();
                    ImGui.Separator();
                    var timeflowcombo = new string[] { "Not managed", "Normal", "Fixed", "InfiniDay", "InfiniDay reversed", "InfiniNight", "InfiniNight reversed" };
                    foreach (var z in plugin.ZoneSettings.Values)
                    {
                        ImGui.Text(z.ZoneId + " | " + z.ZoneName);
                        ImGui.NextColumn();
                        ImGui.PushItemWidth(150f);
                        ImGui.Combo("##timecombo"+z.ZoneId, ref z.TimeFlow, timeflowcombo, timeflowcombo.Length);
                        ImGui.PopItemWidth();
                        if (z.TimeFlow == 2)
                        {
                            ImGui.PushItemWidth(70f);
                            ImGui.DragInt("##timecontrol" + z.ZoneId, ref z.CustomTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                            ImGui.PopItemWidth();
                            ImGui.SameLine();
                            ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(z.CustomTime).ToString("HH:mm:ss"));
                        }
                        ImGui.NextColumn();
                        ImGui.Checkbox("Override weather##wcontrol" + z.ZoneId, ref z.WeatherControl);
                        if (z.WeatherControl)
                        {
                            if(z.SupportedWeathers.Count == 0)
                            {
                                ImGui.Text("Zone has no supported weathers");
                            }
                            else
                            {
                                foreach (var weather in z.SupportedWeathers)
                                {
                                    if (weather.IsNormal) ImGui.PushStyleColor(ImGuiCol.Text, colorGreen);
                                    ImGui.Checkbox(weather.Id + " | " + plugin.weathers[weather.Id] + "##" + z.ZoneId, ref weather.Selected);
                                    if (weather.IsNormal) ImGui.PopStyleColor();
                                }
                            }
                        }
                        ImGui.NextColumn();
                        ImGui.Separator();
                    }
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Weather blacklist"))
                {
                    ImGui.Text("This will be weather blacklist");
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Debug"))
                {
                    try
                    {
                        if(ImGui.Button("Print configuration string"))
                        {
                            plugin._pi.Framework.Gui.Chat.Print(plugin.GetConfigurationString());
                        }
                        ImGui.Text("Current weather: " + *plugin.CurrentWeatherPtr + " / " + plugin.weathers[*plugin.CurrentWeatherPtr]);
                        ImGui.Text("Current time: " + *plugin.TimePtr + " / " + DateTimeOffset.FromUnixTimeSeconds(*plugin.TimePtr).ToString());
                        ImGui.Text("Current zone: " + plugin._pi.ClientState.TerritoryType + " / " +
                            plugin.zones[plugin._pi.ClientState.TerritoryType].PlaceName.Value.Name);
                        List<string> wGui = new List<string>();
                        foreach (var w in plugin.weathers)
                        {
                            wGui.Add(w.Key + " / " + w.Value);
                        }
                        ImGui.Text("All weathers");
                        ImGui.SameLine();
                        ImGui.Combo("##allweathers", ref curW, wGui.ToArray(), wGui.Count);
                        ImGui.SameLine();
                        if (ImGui.Button("Set"))
                        {
                            *plugin.CurrentWeatherPtr = (byte)curW;
                        }
                        ImGui.Text("Supported weathers:");
                        foreach(byte i in plugin.GetWeathers(plugin._pi.ClientState.TerritoryType))
                        {
                            var colored = false;
                            if (*plugin.CurrentWeatherPtr == i)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1,0,0,1));
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                                colored = true;
                            }
                            if(ImGui.SmallButton(i + " / " + plugin.weathers[i]))
                            {
                                *plugin.CurrentWeatherPtr = (byte)i;
                            }
                            if (colored) ImGui.PopStyleColor(2);
                            if(plugin.IsWeatherNormal(i, plugin._pi.ClientState.TerritoryType))
                            {
                                ImGui.SameLine();
                                ImGui.TextColored(new Vector4(0,1,0,1), "Occurs normally");
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        ImGui.Text(e.Message);
                    }
                }
                ImGui.EndTabBar();
            }
        }
    }
}
