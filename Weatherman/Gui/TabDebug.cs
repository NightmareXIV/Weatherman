using Dalamud;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using ECommons.DalamudServices.Legacy;
using Lumina.Excel.Sheets;
using System.IO;
using System.Text.RegularExpressions;
using Weatherman.Services;

namespace Weatherman;

internal unsafe partial class Gui
{
    private float newMult = (float)DataProvider.ETMult;
    private void DrawTabDebug()
    {
        try
        {
            ImGui.BeginChild("##debugscreen");
            if(ImGui.Button("Print configuration string"))
            {
                PluginLog.Information(p.Config.GetConfigurationString());
            }
            ImGui.Checkbox("Pause plugin execution", ref p.PausePlugin);
            ImGui.Checkbox("Profiling", ref p.profiling);
            if(p.profiling)
            {
                ImGui.Text("Total time: " + p.totalTime);
                ImGui.Text("Total ticks: " + p.totalTicks);
                ImGui.Text("Tick avg: " + (float)p.totalTime / (float)p.totalTicks);
                ImGui.Text("MS avg: " + ((float)p.totalTime / (float)p.totalTicks) / (float)Stopwatch.Frequency * 1000 + " ms");
                if(ImGui.Button("Reset##SW"))
                {
                    p.totalTicks = 0;
                    p.totalTime = 0;
                }
            }
            if(SafeMemory.ReadBytes(S.MemoryManager.RenderTimePatch.Address, S.MemoryManager.RenderTimePatch.PatchData.PatchData.Count, out var timeAsm))
            {
                ImGui.TextUnformatted("Time asm: " + timeAsm.ToHexString());
            }
            if(S.MemoryManager.IsTimeCustom())
            {
                ImGui.SameLine();
                ImGui.TextUnformatted("[custom]");
            }
            if(SafeMemory.ReadBytes(S.MemoryManager.RenderWeatherPatch.Address, S.MemoryManager.RenderWeatherPatch.PatchData.PatchData.Count, out var weatherAsm))
            {
                ImGui.TextUnformatted("Weather asm: " + weatherAsm.ToHexString());
            }
            if(S.MemoryManager.IsWeatherCustom())
            {
                ImGui.SameLine();
                ImGui.TextUnformatted("[custom]");
            }
            if(SafeMemory.ReadBytes(S.MemoryManager.RenderSunlightShadowPatch.Address, S.MemoryManager.RenderWeatherPatch.PatchData.PatchData.Count, out var sunlightAsm))
            {
                ImGui.TextUnformatted("Sunlight shadow asm: " + sunlightAsm.ToHexString());
            }
            ImGui.TextUnformatted($"Mult: {DataProvider.ETMult}");
            ImGui.SetNextItemWidth(100f);
            ImGui.DragFloat("New mult", ref newMult, float.Epsilon);
            ImGui.SameLine();
            if(ImGui.Button("Set##mult"))
            {
                DataProvider.ETMult = newMult;
            }
            ImGui.SameLine();
            if(ImGui.Button("Reset##mult"))
            {
                DataProvider.ETMult = 144D / 7D;
            }
            ImGui.TextUnformatted("True weather: " + *S.MemoryManager.TrueWeather + " / " + S.DataProvider.Weathers[*S.MemoryManager.TrueWeather]);
            ImGui.TextUnformatted("Displayed weather: " + S.MemoryManager.GetDisplayedWeather() + " / " + S.DataProvider.Weathers[S.MemoryManager.GetDisplayedWeather()]);
            ImGui.TextUnformatted("True time: " + S.MemoryManager.TrueTime + " / " + DateTimeOffset.FromUnixTimeSeconds(S.MemoryManager.TrueTime).ToString());
            var et = p.GetET();
            ImGui.TextUnformatted("Calculated time: " + et + " / " + DateTimeOffset.FromUnixTimeSeconds(et).ToString());
            var diff = Math.Abs(S.MemoryManager.TrueTime - et);
            ImGui.TextColored(diff < 50 ? ImGuiColors.HealerGreen : (diff < 200 ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudRed), $"Difference: {diff}");
            if(S.MemoryManager.IsTimeCustom()) ImGui.TextUnformatted("Time from asm: " + S.MemoryManager.GetTime() + " / " +
                DateTimeOffset.FromUnixTimeSeconds(S.MemoryManager.GetTime()).ToLocalTime().AlreadyLocal().ToString());
            ImGui.TextUnformatted("Current zone: " + Svc.ClientState.TerritoryType + " / " +
                S.DataProvider.Zones[Svc.ClientState.TerritoryType].PlaceName.ValueNullable?.Name.ToString());
            ImGui.TextUnformatted("Unblacklisted weather: " + p.UnblacklistedWeather);
            List<string> wGui = [];
            foreach(var w in S.DataProvider.Weathers)
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
            if(ImGui.SmallButton("Reset##weather"))
            {
                p.SelectedWeather = 255;
            }
            ImGui.TextUnformatted("Supported weathers:");
            foreach(var i in S.DataProvider.GetWeathers(Svc.ClientState.TerritoryType))
            {
                var colored = false;
                if(S.MemoryManager.GetDisplayedWeather() == i)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                    colored = true;
                }
                if(S.DataProvider.WeatherAllowedZones.Contains(Svc.ClientState.TerritoryType))
                {
                    if(ImGui.SmallButton(i + " / " + S.DataProvider.Weathers[i]))
                    {
                        p.SelectedWeather = i;
                    }
                }
                else
                {
                    ImGui.TextUnformatted(i + " / " + S.DataProvider.Weathers[i]);
                }
                if(colored) ImGui.PopStyleColor(1);
                if(S.DataProvider.IsWeatherNormal(i, Svc.ClientState.TerritoryType))
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Occurs normally");
                }
            }
            if(ImGui.CollapsingHeader("Weather allowed zones"))
            {
                foreach(var a in S.DataProvider.WeatherAllowedZones)
                {
                    ImGui.TextUnformatted($"{a} / {S.DataProvider.Zones[a].PlaceName.Value.Name} ({S.DataProvider.Zones[a].ContentFinderCondition.Value.Name} | {Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault((uint)S.DataProvider.Zones[a].QuestBattle.ValueNullable?.Quest.RowId)?.Name})");
                }
            }
            if(ImGui.CollapsingHeader("Time allowed zones"))
            {
                foreach(var a in S.DataProvider.TimeAllowedZones)
                {
                    ImGui.TextUnformatted($"{a} / {S.DataProvider.Zones[a].PlaceName.Value.Name} ({S.DataProvider.Zones[a].ContentFinderCondition.Value.Name} | {Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault((uint)S.DataProvider.Zones[a].QuestBattle.ValueNullable?.Quest.RowId)?.Name})");
                }
            }
            if(ImGui.CollapsingHeader("envb files"))
            {
                foreach(var a in S.DataProvider.WeatherList)
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
                    foreach(var a in S.DataProvider.WeatherList)
                    {
                        if(a.Value.EnvbFile != null)
                        {
                            try
                            {
                                var path = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "envbdump");
                                foreach(var s in a.Value.EnvbFile.Split("/"))
                                {
                                    if(s.EndsWith(".envb"))
                                    {
                                        Directory.CreateDirectory(path);
                                        Svc.Data.GetFile(a.Value.EnvbFile).SaveFile(Path.Combine(path, s));
                                        File.Create(Path.Combine(path, $"{s}.Terr.{a.Key}.{rgx.Replace(S.DataProvider.Zones[a.Key].PlaceName.Value.Name.ToString(), "")}")).Close();
                                        break;
                                    }
                                    path = Path.Combine(path, s);
                                }
                            }
                            catch(Exception e)
                            {
                                PluginLog.Error($"{e.Message}\n{e.StackTrace}");
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();
        }
        catch(Exception e)
        {
            ImGui.TextUnformatted(e.Message);
        }
    }
}
