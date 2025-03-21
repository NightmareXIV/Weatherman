using ECommons.ImGuiMethods;

namespace Weatherman;

internal unsafe partial class Gui
{
    private void DrawTabBlacklist()
    {
        ImGui.BeginChild("##wblacklist");
        ImGui.TextUnformatted("Select weathers which you do not want to ever see again in any zone.");
        ImGui.TextUnformatted("This setting is not effective for zones that have their weathers overriden in zone-specific settings.");
        ImGui.TextUnformatted("Random normally occurring non-blacklisted weather will be selected to replace blacklisted one.");
        ImGui.TextUnformatted("If there will be no non-blacklisted weather left to choose from, original weather will be kept.");
        ImGui.TextColored(colorGreen, "Normally occurring weathers in current zone are highlighted green.");
        ImGui.TextUnformatted("To unblacklist specific zone without overriding it's weather, go to zone-specific settings and check \"Weather control\"");
        ImGui.TextUnformatted("checkbox on chosen zone without selecting any weathers for it.");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1, 1, 0, 1), "Current weather is yellow (normal)");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1, 0, 0, 1), "or red (abnormal).");
        ImGui.Separator();
        ImGuiEx.TextV($"Weather blacklist effective:");
        ImGui.Indent();
        if(ImGui.RadioButton("Everywhere", p.configuration.BlacklistCS == null))
        {
            p.configuration.BlacklistCS = null;
        }
        if(ImGui.RadioButton("Cutscenes only", p.configuration.BlacklistCS == null))
        {
            p.configuration.BlacklistCS = true;
        }
        if(ImGui.RadioButton("Everywhere except cutscenes", p.configuration.BlacklistCS == null))
        {
            p.configuration.BlacklistCS = null;
        }
        ImGui.Unindent();

        ImGui.Separator();
        if(ImGui.Button("Apply weather changes"))
        {
            p.ApplyWeatherChanges(Svc.ClientState.TerritoryType);
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("Either click this button or change your zone for settings to become effective.");
        ImGui.Separator();

        var temparr = p.configuration.BlacklistedWeathers.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach(var w in temparr)
        {
            var v = temparr[w.Key];
            var normal = p.IsWeatherNormal(w.Key, Svc.ClientState.TerritoryType);
            var current = *p.memoryManager.TrueWeather == w.Key;
            if(normal || current) ImGui.PushStyleColor(ImGuiCol.Text, current ? (normal ? new Vector4(1, 1, 0, 1) : new Vector4(1, 0, 0, 1)) : colorGreen);
            ImGui.Checkbox(w.Key + " / " + p.weathers[w.Key], ref v);
            if(normal || current) ImGui.PopStyleColor();
            p.configuration.BlacklistedWeathers[w.Key] = v;
        }
        ImGui.EndChild();
    }
}
