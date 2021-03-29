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
        string[] timeflowcombo = new string[] { "No override", "Normal", "Fixed", "InfiniDay", "InfiniDay reversed", "InfiniNight", "InfiniNight reversed" };
        bool configWasOpen = false;
        int uid = 0;
        string filter = "";
        bool displayCurrentZone = true;
        bool displayOnlyModified = false;
        bool displayOnlyReal = true;

        public Gui(Weatherman plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (!configOpen)
            {
                if (configWasOpen)
                {
                    plugin.configuration.Save();
                    plugin._pi.Framework.Gui.Chat.Print("Configuration saved");
                }
                configWasOpen = false;
                return;
            }
            uid = 0;
            configWasOpen = true;
            if (!plugin.configuration.ConfigurationString.Equals(plugin.GetConfigurationString()))
            {
                plugin.configuration.Save();
                plugin._pi.Framework.Gui.Chat.Print("Configuration saved");
            }
            if (ImGui.Begin("Weatherman configuration", ref configOpen))
            {
                ImGui.BeginTabBar("weatherman_settings");
                if (ImGui.BeginTabItem("Global setting"))
                {
                    ImGui.Text("Global time control: ");
                    ImGui.SameLine();
                    HelpMarker("No override - time controlled by game;\n" +
                        "Normal - time controlled by plugin, normal flow; \nFixed - time is fixed to specified value;\n"
                        + "InfiniDay - instead of night, another day cycle will begin\n"
                        + "InfiniDay reversed - instead of night, day cycle rewinds backward\n"
                        + "InfiniNight/InfiniNight reversed - same as day options");
                    ImGui.PushItemWidth(150f);
                    ImGui.Combo("##timecomboglobal", ref plugin.configuration.GlobalTimeFlowControl, timeflowcombo, timeflowcombo.Length);
                    ImGui.PopItemWidth();
                    if (plugin.configuration.GlobalTimeFlowControl == 2)
                    {
                        ImGui.Text("Set desired time of day in seconds. Double-click to edit field manually.");
                        ImGui.PushItemWidth(150f);
                        ImGui.DragInt("##timecontrolfixedglobal", ref plugin.configuration.GlobalFixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                        if (plugin.configuration.GlobalFixedTime > Weatherman.SecondsInDay
                            || plugin.configuration.GlobalFixedTime < 0) plugin.configuration.GlobalFixedTime = 0;
                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(plugin.configuration.GlobalFixedTime).ToString("HH:mm:ss"));
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Zone-specific settings"))
                {
                    ImGui.Text("Filter:");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(200f);
                    ImGui.InputTextWithHint("##filter", "ID, partial area or zone name", ref filter, 1000);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.Checkbox("Only modified", ref displayOnlyModified);
                    ImGui.SameLine();
                    ImGui.Checkbox("Only world zones", ref displayOnlyReal);
                    ImGui.SameLine();
                    ImGui.Checkbox("Current zone on top", ref displayCurrentZone);
                    if(ImGui.Button("Apply weather changes"))
                    {
                        plugin.OnZoneChange(null, plugin._pi.ClientState.TerritoryType);
                    }
                    ImGui.SameLine();
                    ImGui.Text("Either click this button or change your zone for weather settings to become effective.");
                    ImGui.BeginChild("##zonetable");
                    ImGui.Columns(6);
                    ImGui.Text("ID");
                    ImGui.NextColumn();
                    ImGui.Text("Area");
                    ImGui.NextColumn();
                    ImGui.Text("Location");
                    ImGui.NextColumn();
                    ImGui.Text("Time control");
                    ImGui.NextColumn();
                    ImGui.Text("Weather control");
                    ImGui.NextColumn();
                    ImGui.Text("");
                    ImGui.NextColumn();
                    ImGui.Separator();

                    //current zone
                    if (displayCurrentZone && plugin.ZoneSettings.ContainsKey(plugin._pi.ClientState.TerritoryType)) {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                        PrintZoneRow(plugin.ZoneSettings[plugin._pi.ClientState.TerritoryType], false);
                        ImGui.PopStyleColor();
                    }

                    foreach (var z in plugin.ZoneSettings.Values)
                    {
                        PrintZoneRow(z);
                    }
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Weather blacklist"))
                {
                    ImGui.BeginChild("##wblacklist");
                    ImGui.Text("Select weathers which you do not want to ever see again in any zone.");
                    ImGui.Text("This setting is not effective for zones that have their weathers overriden in zone-specific settings.");
                    ImGui.Text("First normally occurring non-blacklisted weather will be selected to replace blacklisted one.");
                    ImGui.Text("If there will be no non-blacklisted weather left to choose from, original weather will be kept.");
                    ImGui.TextColored(colorGreen, "Normally ocurring weathers in current zone are highlighted green.");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Current weather is red.");
                    ImGui.Separator();
                    if (ImGui.Button("Apply weather changes"))
                    {
                        plugin.OnZoneChange(null, plugin._pi.ClientState.TerritoryType);
                    }
                    ImGui.SameLine();
                    ImGui.Text("Either click this button or change your zone for settings to become effective.");
                    ImGui.Separator();
                    //fucked shit begins, sorry GC
                    var temparr = plugin.configuration.BlacklistedWeathers.ToDictionary(entry => entry.Key, entry => entry.Value);
                    foreach (var w in temparr)
                    {
                        var v = temparr[w.Key];
                        var normal = plugin.IsWeatherNormal(w.Key, plugin._pi.ClientState.TerritoryType);
                        var current = *plugin.CurrentWeatherPtr == w.Key;
                        if (normal || current) ImGui.PushStyleColor(ImGuiCol.Text, current?(normal? new Vector4(1, 1, 0, 1):new Vector4(1,0,0,1)): colorGreen);
                        ImGui.Checkbox(w.Key + " / " + plugin.weathers[w.Key], ref v);
                        if (normal || current) ImGui.PopStyleColor();
                        plugin.configuration.BlacklistedWeathers[w.Key] = v;
                    }
                    ImGui.EndChild();
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
                        var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                        ImGui.Text("True ET: " + et + " / " + DateTimeOffset.FromUnixTimeSeconds(et).ToString());
                        ImGui.Text("Current zone: " + plugin._pi.ClientState.TerritoryType + " / " +
                            plugin.zones[plugin._pi.ClientState.TerritoryType].PlaceName.Value.Name);
                        ImGui.Text("Unblacklisted weather: " + plugin.UnblacklistedWeather);
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

        void PrintZoneRow(ZoneSettings z, bool filtering = true)
        {
            if (filtering)
            {
                if (filter != ""
                    && !z.ZoneId.ToString().ToLower().Contains(filter.ToLower())
                    && !z.terr.PlaceNameZone.Value.Name.ToLower().Contains(filter.ToLower())
                    && !z.ZoneName.ToLower().Contains(filter.ToLower())) return;
                if (displayOnlyReal && !plugin.IsWorldTerritory(z.ZoneId)) return;
                if (displayOnlyModified)
                {
                    var sel = new List<string>();
                    foreach (var zz in z.SupportedWeathers)
                    {
                        if (zz.Selected) sel.Add(zz.Id.ToString());
                    }
                    if (z.WeatherControl == false && z.TimeFlow == 0 && z.FixedTime == 0 && sel.Count == 0) return;
                }
            }
            ImGui.Text(z.ZoneId.ToString());
            ImGui.NextColumn();
            ImGui.Text(z.terr.PlaceNameZone.Value.Name);
            ImGui.NextColumn();
            ImGui.Text(z.ZoneName);
            ImGui.NextColumn();
            ImGui.PushItemWidth(150f);
            ImGui.Combo("##timecombo" + ++uid, ref z.TimeFlow, timeflowcombo, timeflowcombo.Length);
            ImGui.PopItemWidth();
            if (z.TimeFlow == 2)
            {
                ImGui.PushItemWidth(70f);
                ImGui.DragInt("##timecontrol" + ++uid, ref z.FixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                if (z.FixedTime > Weatherman.SecondsInDay || z.FixedTime < 0) z.FixedTime = 0;
                ImGui.PopItemWidth();
                ImGui.SameLine();
                ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(z.FixedTime).ToString("HH:mm:ss"));
            }
            ImGui.NextColumn();
            ImGui.Checkbox("Override weather##wcontrol" + ++uid, ref z.WeatherControl);
            if (z.WeatherControl)
            {
                if (z.SupportedWeathers.Count == 0)
                {
                    ImGui.Text("Zone has no supported weathers");
                }
                else
                {
                    foreach (var weather in z.SupportedWeathers)
                    {
                        if (weather.IsNormal) ImGui.PushStyleColor(ImGuiCol.Text, colorGreen);
                        ImGui.Checkbox(weather.Id + " | " + plugin.weathers[weather.Id] + "##" + ++uid, ref weather.Selected);
                        if (weather.IsNormal) ImGui.PopStyleColor();
                    }
                }
            }
            ImGui.NextColumn();
            if (ImGui.Button("X##"+ ++uid)){
                foreach (var zz in z.SupportedWeathers)
                {
                    zz.Selected = false;
                }
                z.WeatherControl = false; 
                z.TimeFlow = 0; 
                z.FixedTime = 0;
            }
            ImGui.NextColumn();
            ImGui.Separator();
        }

        static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }
    }
}
