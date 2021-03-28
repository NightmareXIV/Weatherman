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
        private bool trashBool = false;
        private int trashInt = 0;
        public bool configOpen = false;

        public Gui(Weatherman plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (!configOpen) return;
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
                    foreach (var z in plugin.zones)
                    {
                        ImGui.Text(z.Key + " | " + z.Value.PlaceName.Value.Name);
                        ImGui.NextColumn();
                        ImGui.PushItemWidth(100f);
                        ImGui.DragInt("##timecontrol"+z.Key, ref trashInt, 100.0f, 0, Weatherman.SecondsInDay-1);
                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(trashInt).ToString("HH:mm:ss"));
                        ImGui.NextColumn();
                        ImGui.Checkbox("##wcontrol" + z.Key, ref trashBool);
                        ImGui.NextColumn();
                        if (trashBool)
                        {
                            ImGui.Columns(1);
                            ImGui.Text("test");
                            ImGui.Columns(3);
                        }
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
