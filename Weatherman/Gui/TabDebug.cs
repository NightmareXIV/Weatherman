using Dalamud;
using Dalamud.Interface.Colors;
using Lumina.Excel.Sheets;
using System.IO;
using System.Text.RegularExpressions;
using ECommons.DalamudServices.Legacy;
using Dalamud.Interface.ImGuiNotification;

namespace Weatherman
{
    internal unsafe partial class Gui
    {
        float newMult = (float)Weatherman.ETMult;
        void DrawTabDebug()
        {
            try
            {
                ImGui.BeginChild("##debugscreen");
                if (ImGui.Button("Print configuration string"))
                {
                    PluginLog.Information(p.configuration.GetConfigurationString());
                }
                ImGui.Checkbox("Pause plugin execution", ref p.PausePlugin);
                ImGui.Checkbox("Profiling", ref p.profiling);
                if (p.profiling)
                {
                    ImGui.Text("Total time: " + p.totalTime);
                    ImGui.Text("Total ticks: " + p.totalTicks);
                    ImGui.Text("Tick avg: " + (float)p.totalTime / (float)p.totalTicks);
                    ImGui.Text("MS avg: " + ((float)p.totalTime / (float)p.totalTicks) / (float)Stopwatch.Frequency * 1000 + " ms");
                    if (ImGui.Button("Reset##SW"))
                    {
                        p.totalTicks = 0;
                        p.totalTime = 0;
                    }
                }
                if (SafeMemory.ReadBytes(p.memoryManager.TimeAsmPtr, p.memoryManager.NewTimeAsm.Length, out var timeAsm))
                {
                    ImGui.TextUnformatted("Time asm: " + timeAsm.ToHexString());
                }
                if (p.memoryManager.IsTimeCustom())
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted("[custom]");
                }
                if (SafeMemory.ReadBytes(p.memoryManager.WeatherAsmPtr, p.memoryManager.NewWeatherAsm.Length, out var weatherAsm))
                {
                    ImGui.TextUnformatted("Weather asm: " + weatherAsm.ToHexString());
                }
                if (p.memoryManager.IsWeatherCustom())
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted("[custom]");
                }
                ImGui.TextUnformatted($"Mult: {Weatherman.ETMult}");
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("New mult", ref newMult, float.Epsilon);
                ImGui.SameLine();
                if (ImGui.Button("Set##mult"))
                {
                    Weatherman.ETMult = newMult;
                }
                ImGui.SameLine();
                if (ImGui.Button("Reset##mult"))
                {
                    Weatherman.ETMult = 144D/7D;
                }
                ImGui.TextUnformatted("True weather: " + *p.memoryManager.TrueWeather + " / " + p.weathers[*p.memoryManager.TrueWeather]);
                ImGui.TextUnformatted("Displayed weather: " + p.memoryManager.GetDisplayedWeather() + " / " + p.weathers[p.memoryManager.GetDisplayedWeather()]);
                ImGui.TextUnformatted("True time: " + p.memoryManager.TrueTime + " / " + DateTimeOffset.FromUnixTimeSeconds(p.memoryManager.TrueTime).ToString());
                var et = p.GetET();
                ImGui.TextUnformatted("Calculated time: " + et + " / " + DateTimeOffset.FromUnixTimeSeconds(et).ToString());
                var diff = Math.Abs(p.memoryManager.TrueTime - et);
                ImGui.TextColored(diff < 50?ImGuiColors.HealerGreen:(diff<200?ImGuiColors.DalamudOrange:ImGuiColors.DalamudRed), $"Difference: {diff}");
                if (p.memoryManager.IsTimeCustom()) ImGui.TextUnformatted("Time from asm: " + p.memoryManager.GetTime() + " / " +
                    DateTimeOffset.FromUnixTimeSeconds(p.memoryManager.GetTime()).ToLocalTime().AlreadyLocal().ToString());
                ImGui.TextUnformatted("Current zone: " + Svc.ClientState.TerritoryType + " / " +
                    p.zones[Svc.ClientState.TerritoryType].PlaceName.ValueNullable?.Name.ToString());
                ImGui.TextUnformatted("Unblacklisted weather: " + p.UnblacklistedWeather);
                List<string> wGui = new();
                foreach (var w in p.weathers)
                {
                    wGui.Add(w.Key + " / " + w.Value);
                }
                ImGui.TextUnformatted("Weather list");
                ImGui.SameLine();
                ImGui.PushItemWidth(200f);
                ImGui.Combo("##allweathers", ref curW, wGui.ToArray(), wGui.Count);
                ImGui.PopItemWidth();
                /*if (p.configuration.Unsafe)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Set##setweather"))
                    {
                        p.SelectedWeather = (byte)curW;
                    }
                }*/
                ImGui.TextUnformatted("Selected weather: " + p.SelectedWeather);
                ImGui.SameLine();
                if (ImGui.SmallButton("Reset##weather"))
                {
                    p.SelectedWeather = 255;
                }
                ImGui.TextUnformatted("Supported weathers:");
                foreach (byte i in p.GetWeathers(Svc.ClientState.TerritoryType))
                {
                    var colored = false;
                    if (p.memoryManager.GetDisplayedWeather() == i)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                        colored = true;
                    }
                    if (p.weatherAllowedZones.Contains(Svc.ClientState.TerritoryType))
                    {
                        if (ImGui.SmallButton(i + " / " + p.weathers[i]))
                        {
                            p.SelectedWeather = i;
                        }
                    }
                    else
                    {
                        ImGui.TextUnformatted(i + " / " + p.weathers[i]);
                    }
                    if (colored) ImGui.PopStyleColor(1);
                    if (p.IsWeatherNormal(i, Svc.ClientState.TerritoryType))
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Occurs normally");
                    }
                }
                if (ImGui.CollapsingHeader("Weather allowed zones"))
                {
                    foreach (var a in p.weatherAllowedZones)
                    {
                        ImGui.TextUnformatted($"{a} / {p.zones[a].PlaceName.Value.Name} ({p.zones[a].ContentFinderCondition.Value.Name} | {Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault((uint)p.zones[a].QuestBattle.ValueNullable?.Quest.RowId)?.Name})");
                    }
                }
                if (ImGui.CollapsingHeader("Time allowed zones"))
                {
                    foreach (var a in p.timeAllowedZones)
                    {
                        ImGui.TextUnformatted($"{a} / {p.zones[a].PlaceName.Value.Name} ({p.zones[a].ContentFinderCondition.Value.Name} | {Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault((uint)p.zones[a].QuestBattle.ValueNullable?.Quest.RowId)?.Name})");
                    }
                }
                if(ImGui.CollapsingHeader("envb files"))
                {
                    foreach(var a in p.weatherList)
                    {
                        if(ImGui.Selectable($"{a.Key}: {a.Value.EnvbFile}"))
                        {
                            ImGui.SetClipboardText(a.Value.EnvbFile);
                            Svc.PluginInterface.UiBuilder.AddNotification("Copied to clipboard", p.Name, NotificationType.Success);
                        }
                    }
                    if(ImGui.Button("Dump all"))
                    {
                        var rgx = new Regex("[^a-zA-Z0-9 -]");
                        foreach (var a in p.weatherList)
                        {
                            if (a.Value.EnvbFile != null)
                            {
                                try
                                {
                                    var path = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "envbdump");
                                    foreach (var s in a.Value.EnvbFile.Split("/"))
                                    {
                                        if (s.EndsWith(".envb"))
                                        {
                                            Directory.CreateDirectory(path);
                                            Svc.Data.GetFile(a.Value.EnvbFile).SaveFile(Path.Combine(path, s));
                                            File.Create(Path.Combine(path, $"{s}.Terr.{a.Key}.{rgx.Replace(p.zones[a.Key].PlaceName.Value.Name.ToString(), "")}")).Close();
                                            break;
                                        }
                                        path = Path.Combine(path, s);
                                    }
                                }
                                catch (Exception e)
                                {
                                    PluginLog.Error($"{e.Message}\n{e.StackTrace}");
                                }
                            }
                        }
                    }
                }
                ImGui.EndChild();
            }
            catch (Exception e)
            {
                ImGui.TextUnformatted(e.Message);
            }
        }
    }
}
