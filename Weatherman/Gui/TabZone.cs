using ECommons;
using ECommons.ImGuiMethods;

namespace Weatherman;

internal partial class Gui
{
    private void DrawTabZone()
    {
        ImGui.TextUnformatted("Filter:");
        ImGui.SameLine();
        ImGui.PushItemWidth(200f);
        ImGui.InputTextWithHint("##filter", "ID, partial area or zone name", ref filter, 1000);
        ImGui.PopItemWidth();
        ImGui.SameLine();
        ImGui.Checkbox("Only modified", ref p.configuration.ShowOnlyModified);
        ImGui.SameLine();
        ImGui.Checkbox("Only fully customizable zones", ref p.configuration.ShowOnlyWorldZones);
        ImGui.SameLine();
        ImGui.Checkbox("Current zone on top", ref p.configuration.ShowCurrentZoneOnTop);
        ImGui.SameLine();
        ImGui.Checkbox("Show unnamed zones", ref p.configuration.ShowUnnamedZones);
        if(ImGui.Button("Apply weather changes"))
        {
            p.ApplyWeatherChanges(Svc.ClientState.TerritoryType);
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("Either click this button or change your zone for weather settings to become effective.");
        ImGui.BeginChild("##zonetable");
        ImGui.Columns(7);
        ImGui.SetColumnWidth(0, 35);
        ImGui.SetColumnWidth(1, 140);
        ImGui.SetColumnWidth(2, ImGui.GetWindowWidth() - 680);
        ImGui.SetColumnWidth(3, 140);
        ImGui.SetColumnWidth(4, 170);
        ImGui.SetColumnWidth(5, 150);
        ImGui.SetColumnWidth(6, 30);
        ImGui.TextUnformatted("ID");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Area");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Location");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Time control");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Weather control");
        ImGui.NextColumn();
        ImGui.TextUnformatted("BGM override");
        ImGui.NextColumn();
        ImGui.TextUnformatted("");
        ImGui.NextColumn();
        ImGui.Separator();

        //current zone
        if(p.configuration.ShowCurrentZoneOnTop && p.ZoneSettings.ContainsKey(Svc.ClientState.TerritoryType))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 1, 1));
            PrintZoneRow(p.ZoneSettings[Svc.ClientState.TerritoryType], false);
            ImGui.PopStyleColor();
        }

        foreach(var z in p.ZoneSettings.Values)
        {
            PrintZoneRow(z);
        }
        ImGui.EndChild();
    }



    private void PrintZoneRow(ZoneSettings z, bool filtering = true)
    {
        var modAllowed = p.weatherAllowedZones.Contains(z.ZoneId) || p.timeAllowedZones.Contains(z.ZoneId);
        var bothModAllowed = p.weatherAllowedZones.Contains(z.ZoneId) && p.timeAllowedZones.Contains(z.ZoneId);
        var grayed = false;
        if(filtering)
        {
            if(filter != ""
                && !z.ZoneId.ToString().ToLower().Contains(filter.ToLower())
                && !z.terr.PlaceNameZone.Value.Name.ToString().ToLower().Contains(filter.ToLower())
                && !z.ZoneName.ToLower().Contains(filter.ToLower())) return;
            //if (displayOnlyReal && !p.IsWorldTerritory(z.ZoneId)) return;
            if(p.configuration.ShowOnlyModified)
            {
                var sel = new List<string>();
                foreach(var zz in z.SupportedWeathers)
                {
                    if(zz.Selected) sel.Add(zz.Id.ToString());
                }
                if(z.IsUntouched()) return;
            }
            if(!p.configuration.ShowUnnamedZones && z.ZoneName.Length == 0) return;
            if(p.configuration.ShowOnlyWorldZones && !bothModAllowed) return;
            if(!modAllowed)
            {
                grayed = true;
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1));
            }
        }
        ImGui.TextUnformatted(z.ZoneId.ToString());
        ImGui.NextColumn();
        ImGui.TextUnformatted(z.terr.PlaceNameZone.Value.Name.ToString());
        ImGui.NextColumn();
        ImGui.TextUnformatted(z.ZoneName);
        ImGui.NextColumn();
        if(p.timeAllowedZones.Contains(z.ZoneId))
        {
            ImGui.PushItemWidth(120f);
            ImGui.Combo("##timecombo" + ++uid, ref z.TimeFlow, timeflowcombo, timeflowcombo.Length);
            ImGui.PopItemWidth();
            if(z.TimeFlow == 2)
            {
                ImGui.PushItemWidth(50f);
                ImGui.DragInt("##timecontrol" + ++uid, ref z.FixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                if(z.FixedTime > Weatherman.SecondsInDay || z.FixedTime < 0) z.FixedTime = 0;
                ImGui.PopItemWidth();
                ImGui.SameLine();
                ImGui.TextUnformatted(DateTimeOffset.FromUnixTimeSeconds(z.FixedTime).ToString("HH:mm:ss"));
            }
        }
        else
        {
            ImGui.TextUnformatted("Unsupported");
        }
        ImGui.NextColumn();
        if(p.weatherAllowedZones.Contains(z.ZoneId))
        {
            if(z.WeatherControl)
            {
                ImGui.Checkbox("##wcontrol" + ++uid, ref z.WeatherControl);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var wCount = z.SupportedWeathers.Where(w => w.Selected).Count();
                if(ImGui.BeginCombo($"##wcontrolc{uid}", wCount == 0 ? "Original" : $"{wCount} weathers"))
                {
                    if(z.SupportedWeathers.Count == 0)
                    {
                        ImGui.TextUnformatted("Zone has no supported weathers");
                    }
                    else
                    {
                        foreach(var weather in z.SupportedWeathers)
                        {
                            if(weather.IsNormal) ImGui.PushStyleColor(ImGuiCol.Text, colorGreen);
                            ImGui.Checkbox(weather.Id + " | " + p.weathers[weather.Id] + "##" + ++uid, ref weather.Selected);
                            if(weather.IsNormal) ImGui.PopStyleColor();
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                ImGui.Checkbox("Override weather##wcontrol" + ++uid, ref z.WeatherControl);
            }
        }
        else
        {
            ImGui.TextUnformatted("Unsupported");
        }
        ImGui.NextColumn();
        if(p.configuration.MusicEnabled && Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "orchestrion" && x.IsLoaded))
        {
            var songs = p.orchestrionController.GetSongList();
            if(songs != null)
            {
                ImGui.PushItemWidth(130f);
                if(ImGui.BeginCombo("##SelectSong" + ++uid, p.orchestrionController.GetSongById(z.Music).ToString(), ImGuiComboFlags.HeightLarge))
                {
                    ImGui.TextUnformatted("Filter:");
                    ImGui.SameLine();
                    ImGui.InputText("##musicfilter" + ++uid, ref musicFilter, 100);
                    foreach(var s in p.orchestrionController.GetSongList())
                    {
                        if((s.Id.ToString() + s.Name).Contains(musicFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            if(ImGui.Selectable($"{s.Name.Cut(40)}##{s.Id}"))
                            {
                                z.Music = s.Id;
                            }
                            if(s.Name.Length > 40) ImGuiEx.Tooltip(s.Name);
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
            }
            else
            {
                ImGui.TextUnformatted("Orchestrion not found");
            }
        }
        else
        {
            ImGui.TextUnformatted("Music is not enabled");
        }
        ImGui.NextColumn();
        if(ImGui.Button("X##" + ++uid))
        {
            foreach(var zz in z.SupportedWeathers)
            {
                zz.Selected = false;
            }
            z.WeatherControl = false;
            z.TimeFlow = 0;
            z.FixedTime = 0;
            z.Music = 0;
        }
        if(grayed) ImGui.PopStyleColor();
        ImGui.NextColumn();
        ImGui.Separator();
    }
}
