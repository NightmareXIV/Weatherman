using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows.Forms;
using System.Runtime.ExceptionServices;

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
        bool autoscrollLog = true;
        string musicFilter = "";

        public Gui(Weatherman plugin)
        {
            this.plugin = plugin;
        }

        [HandleProcessCorruptedStateExceptions]
        public void Draw()
        {
            try
            {
                if (!configOpen)
                {
                    if (configWasOpen)
                    {
                        plugin.configuration.Save();
                        plugin.WriteLog("Configuration saved");
                    }
                    configWasOpen = false;
                    return;
                }
                uid = 0;
                configWasOpen = true;
                if (!plugin.configuration.ConfigurationString.Equals(plugin.GetConfigurationString()))
                {
                    plugin.configuration.Save();
                    plugin.WriteLog("Configuration saved");
                }
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(900, 350));
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
                        ImGui.Checkbox("Enable music control", ref plugin.configuration.MusicEnabled);
                        ImGui.Text("Requires Orchestrion plugin installed and enabled.");
                        if (ImGui.Button("TrySetup"))
                        {
                            plugin.GetSongList();
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
                        ImGui.Checkbox("Only modified", ref plugin.configuration.ShowOnlyModified);
                        ImGui.SameLine();
                        ImGui.Checkbox("Only world zones", ref plugin.configuration.ShowOnlyWorldZones);
                        ImGui.SameLine();
                        ImGui.Checkbox("Current zone on top", ref plugin.configuration.ShowCurrentZoneOnTop);
                        ImGui.SameLine();
                        ImGui.Checkbox("Show unnamed zones", ref plugin.configuration.ShowUnnamedZones);
                        if (!plugin.configuration.ShowOnlyWorldZones)
                        {
                            ImGui.TextColored(new Vector4(1, 0, 0, 1),
                                "Warning: non-world zones only support music changes.");
                        }
                        if (ImGui.Button("Apply weather changes"))
                        {
                            plugin.ApplyWeatherChanges(plugin._pi.ClientState.TerritoryType);
                        }
                        ImGui.SameLine();
                        ImGui.Text("Either click this button or change your zone for weather settings to become effective.");
                        ImGui.BeginChild("##zonetable");
                        ImGui.Columns(7);
                        ImGui.SetColumnWidth(0, 35);
                        ImGui.SetColumnWidth(1, 140);
                        ImGui.SetColumnWidth(2, ImGui.GetWindowWidth() - 680);
                        ImGui.SetColumnWidth(3, 140);
                        ImGui.SetColumnWidth(4, 170);
                        ImGui.SetColumnWidth(5, 150);
                        ImGui.SetColumnWidth(6, 30);
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
                        ImGui.Text("BGM override");
                        ImGui.NextColumn();
                        ImGui.Text("");
                        ImGui.NextColumn();
                        ImGui.Separator();

                        //current zone
                        if (plugin.configuration.ShowCurrentZoneOnTop && plugin.ZoneSettings.ContainsKey(plugin._pi.ClientState.TerritoryType))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 1, 1));
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
                        ImGui.TextColored(colorGreen, "Normally occurring weathers in current zone are highlighted green.");
                        ImGui.Text("To unblacklist specific zone without overriding it's weather, go to zone-specific settings and check \"Weather control\"");
                        ImGui.Text("checkbox on chosen zone without selecting any weathers for it.");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1, 1, 0, 1), "Current weather is yellow (normal)");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "or red (abnormal).");
                        ImGui.Separator();
                        if (ImGui.Button("Apply weather changes"))
                        {
                            plugin.ApplyWeatherChanges(plugin._pi.ClientState.TerritoryType);
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
                            if (normal || current) ImGui.PushStyleColor(ImGuiCol.Text, current ? (normal ? new Vector4(1, 1, 0, 1) : new Vector4(1, 0, 0, 1)) : colorGreen);
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
                            ImGui.BeginChild("##debugscreen");
                            ImGui.Columns(2);
                            ImGui.BeginChild("##debug1");
                            if (ImGui.Button("Print configuration string"))
                            {
                                plugin.WriteLog(plugin.GetConfigurationString());
                            }
                            if (plugin.configuration.Unsafe && ImGui.Button("Unsafe options unlocked. Disable."))
                            {
                                plugin.configuration.Unsafe = false;
                            }
                            ImGui.Checkbox("Pause plugin execution", ref plugin.PausePlugin);
                            ImGui.Text("Current weather: " + *plugin.CurrentWeatherPtr + " / " + plugin.weathers[*plugin.CurrentWeatherPtr]);
                            ImGui.Text("Current time: " + *plugin.TimePtr + " / " + DateTimeOffset.FromUnixTimeSeconds(*plugin.TimePtr).ToString());
                            var et = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 144D / 7D / 1000D);
                            ImGui.Text("True ET: " + et + " / " + DateTimeOffset.FromUnixTimeSeconds(et).ToString());
                            var tw = plugin.WeatherSvc.GetCurrentWeather(plugin._pi.ClientState.TerritoryType, 0);
                            if (tw.Item1 != null) ImGui.Text("True weather: " + tw.Item1.RowId + " / " + tw.Item1.Name);
                            if ((plugin.configuration.Unsafe || plugin.IsWorldTerritory(plugin._pi.ClientState.TerritoryType))
                                    && !plugin.AtVista)
                            {
                                ImGui.SameLine();
                                if (ImGui.SmallButton("Set")) plugin.RestoreOriginalWeather();
                            }
                            ImGui.Text("Current zone: " + plugin._pi.ClientState.TerritoryType + " / " +
                                plugin.zones[plugin._pi.ClientState.TerritoryType].PlaceName.Value.Name);
                            ImGui.Text("Unblacklisted weather: " + plugin.UnblacklistedWeather);
                            List<string> wGui = new List<string>();
                            foreach (var w in plugin.weathers)
                            {
                                wGui.Add(w.Key + " / " + w.Value);
                            }
                            if (plugin.WeatherWasChanged) ImGui.Text("Weather in this area has been modified");
                            ImGui.Text("Weather list");
                            ImGui.SameLine();
                            ImGui.PushItemWidth(200f);
                            ImGui.Combo("##allweathers", ref curW, wGui.ToArray(), wGui.Count);
                            ImGui.PopItemWidth();
                            if (plugin.configuration.Unsafe && !plugin.AtVista)
                            {
                                ImGui.SameLine();
                                if (ImGui.Button("Set"))
                                {
                                    *plugin.CurrentWeatherPtr = (byte)curW;
                                    plugin.WeatherWasChanged = true;
                                }
                            }
                            ImGui.Text("Supported weathers:");
                            foreach (byte i in plugin.GetWeathers(plugin._pi.ClientState.TerritoryType))
                            {
                                var colored = false;
                                if (*plugin.CurrentWeatherPtr == i)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                                    colored = true;
                                }
                                if ((plugin.configuration.Unsafe || plugin.IsWorldTerritory(plugin._pi.ClientState.TerritoryType))
                                    && !plugin.AtVista)
                                {
                                    if (ImGui.SmallButton(i + " / " + plugin.weathers[i]))
                                    {
                                        *plugin.CurrentWeatherPtr = i;
                                        plugin.WeatherWasChanged = true;
                                    }
                                }
                                else
                                {
                                    ImGui.Text(i + " / " + plugin.weathers[i]);
                                }
                                if (colored) ImGui.PopStyleColor(1);
                                if (plugin.IsWeatherNormal(i, plugin._pi.ClientState.TerritoryType))
                                {
                                    ImGui.SameLine();
                                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Occurs normally");
                                }
                            }
                            ImGui.EndChild();
                            ImGui.NextColumn();
                            ImGui.Text("Log:");
                            ImGui.SameLine();
                            /*ImGui.Checkbox("Enable##log", ref plugin.configuration.EnableLogging);
                            ImGui.SameLine();*/ //why would you want to disable logging?
                            ImGui.Checkbox("Autoscroll##log", ref autoscrollLog);
                            ImGui.SameLine();
                            if (ImGui.Button("Copy all"))
                            {
                                var s = new StringBuilder();
                                for (int i = 0; i < plugin.Log.Length; i++)
                                {
                                    if (plugin.Log[i] != null)
                                    {
                                        s.AppendLine(plugin.Log[i]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                Clipboard.SetText(s.ToString());
                            }
                            ImGui.BeginChild("##logtext");
                            for (var i = 0; i < plugin.Log.Length; i++)
                            {
                                if (plugin.Log[i] != null) ImGui.TextWrapped(plugin.Log[i]);
                            }
                            if (autoscrollLog) ImGui.SetScrollHereY();
                            ImGui.EndChild();
                            ImGui.Columns(1);
                            ImGui.EndChild();
                        }
                        catch (Exception e)
                        {
                            ImGui.Text(e.Message);
                        }
                    }
                    ImGui.EndTabBar();
                }
                ImGui.PopStyleVar();
            }
            catch(Exception e)
            {
                plugin.WriteLog("Error in weatherman: "+e);
            }
        }

        void PrintZoneRow(ZoneSettings z, bool filtering = true)
        {
            var grayed = false;
            if (filtering)
            {
                if (filter != ""
                    && !z.ZoneId.ToString().ToLower().Contains(filter.ToLower())
                    && !z.terr.PlaceNameZone.Value.Name.ToString().ToLower().Contains(filter.ToLower())
                    && !z.ZoneName.ToLower().Contains(filter.ToLower())) return;
                //if (displayOnlyReal && !plugin.IsWorldTerritory(z.ZoneId)) return;
                if (plugin.configuration.ShowOnlyModified)
                {
                    var sel = new List<string>();
                    foreach (var zz in z.SupportedWeathers)
                    {
                        if (zz.Selected) sel.Add(zz.Id.ToString());
                    }
                    if (z.IsUntouched()) return;
                }
                if (!plugin.configuration.ShowUnnamedZones && z.ZoneName.Length == 0) return;
                if (plugin.configuration.ShowOnlyWorldZones && !plugin.IsWorldTerritory(z.ZoneId)) return;
                if (!plugin.IsWorldTerritory(z.ZoneId))
                {
                    grayed = true;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1));
                }
            }
            ImGui.Text(z.ZoneId.ToString());
            ImGui.NextColumn();
            ImGui.Text(z.terr.PlaceNameZone.Value.Name);
            ImGui.NextColumn();
            ImGui.Text(z.ZoneName);
            ImGui.NextColumn();
            if (plugin.IsWorldTerritory(z.ZoneId))
            {
                ImGui.PushItemWidth(120f);
                ImGui.Combo("##timecombo" + ++uid, ref z.TimeFlow, timeflowcombo, timeflowcombo.Length);
                ImGui.PopItemWidth();
                if (z.TimeFlow == 2)
                {
                    ImGui.PushItemWidth(50f);
                    ImGui.DragInt("##timecontrol" + ++uid, ref z.FixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                    if (z.FixedTime > Weatherman.SecondsInDay || z.FixedTime < 0) z.FixedTime = 0;
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(z.FixedTime).ToString("HH:mm:ss"));
                }
            }
            else
            {
                ImGui.Text("Unsupported");
            }
            ImGui.NextColumn();
            if (plugin.IsWorldTerritory(z.ZoneId))
            {
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
            }
            else
            {
                ImGui.Text("Unsupported");
            }
            ImGui.NextColumn();
            if (plugin.configuration.MusicEnabled)
            {
                var songs = plugin.GetSongList();
                if (songs != null)
                {
                    ImGui.PushItemWidth(130f);
                    if(ImGui.BeginCombo("##SelectSong" + ++uid, plugin.GetSongById(z.Music).ToString()))
                    {
                        ImGui.Text("Filter:");
                        ImGui.SameLine();
                        ImGui.InputText("##musicfilter"+ ++uid, ref musicFilter, 100);
                        foreach (var s in plugin.GetSongList())
                        if (s.Value.ToString().ToLower().Contains(musicFilter.ToLower()) && ImGui.Selectable(s.Value.ToString()))
                        {
                            z.Music = s.Value.Id;
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.Text("Orchestrion not found");
                }
            }
            else
            {
                ImGui.Text("Music is not enabled");
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
                z.Music = 0;
            }
            if (grayed) ImGui.PopStyleColor();
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
