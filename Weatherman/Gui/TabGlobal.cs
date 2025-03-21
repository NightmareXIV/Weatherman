using ECommons.ImGuiMethods;
using Weatherman.Services;

namespace Weatherman;

internal partial class Gui
{
    private void DrawTabGlobal()
    {
        ImGui.TextUnformatted("Global time control: ");
        ImGui.SameLine();
        HelpMarker("No override - time controlled by game;\n" +
            "Normal - time controlled by plugin, normal flow; \nFixed - time is fixed to specified value;\n"
            + "InfiniDay - instead of night, another day cycle will begin\n"
            + "InfiniDay reversed - instead of night, day cycle rewinds backward\n"
            + "InfiniNight/InfiniNight reversed - same as day options");
        ImGui.PushItemWidth(150f);
        ImGui.Combo("##timecomboglobal", ref p.Config.GlobalTimeFlowControl, timeflowcombo, timeflowcombo.Length);
        ImGui.PopItemWidth();
        if(p.Config.GlobalTimeFlowControl == 2)
        {
            ImGui.TextUnformatted("Set desired time of day in seconds. Double-click to edit field manually.");
            ImGui.PushItemWidth(150f);
            ImGui.DragInt("##timecontrolfixedglobal", ref p.Config.GlobalFixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
            if(p.Config.GlobalFixedTime > Weatherman.SecondsInDay
                || p.Config.GlobalFixedTime < 0) p.Config.GlobalFixedTime = 0;
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.TextUnformatted(DateTimeOffset.FromUnixTimeSeconds(p.Config.GlobalFixedTime).ToString("HH:mm:ss"));
        }
        if(p.Config.GlobalTimeFlowControl == 7)
        {
            if(ImGui.RadioButton("Use local PC time", !p.Config.UseGMTForRealTime)) p.Config.UseGMTForRealTime = false;
            if(ImGui.RadioButton("Use server time (GMT time)", p.Config.UseGMTForRealTime)) p.Config.UseGMTForRealTime = true;
            ImGui.SetNextItemWidth(150);
            ImGui.InputInt("Additional offset, hours: ", ref p.Config.Offset);
            if(p.Config.Offset < -12) p.Config.Offset = -12;
            if(p.Config.Offset > 12) p.Config.Offset = 12;
        }
        ImGui.Checkbox("Enable music control", ref p.Config.MusicEnabled);
        ImGui.TextUnformatted("Requires Orchestrion plugin installed and enabled.");
        ImGui.Checkbox("Disable plugin in cutscenes", ref p.Config.DisableInCutscene);
        ImGui.Checkbox("Enable time control", ref p.Config.EnableTimeControl);
        ImGui.Checkbox("Enable weather control", ref p.Config.EnableWeatherControl);
        ImGui.Checkbox("Disable clock out of sync check", ref p.Config.NoClockNag);
        ImGui.Checkbox("Change time flow speed", ref p.Config.ChangeTimeFlowSpeed);
        if(p.Config.ChangeTimeFlowSpeed)
        {
            ImGui.SetNextItemWidth(100f);
            ImGui.DragFloat("Time flow speed multiplier", ref p.Config.TimeFlowSpeed, 0.01f, 0f, 100f);
            ValidateRange(ref p.Config.TimeFlowSpeed, 0f, 1000f);
        }
        if(ImGui.Checkbox("Always show plugin interface in gpose", ref p.Config.DisplayInGpose))
        {
            Svc.PluginInterface.UiBuilder.DisableGposeUiHide = p.Config.DisplayInGpose;
        }
        ImGui.Separator();
        var mustUpdateDtr = false;
        ImGuiEx.TextV($"Server info bar:");
        ImGui.Indent();
        mustUpdateDtr |= ImGui.Checkbox("Enable", ref C.DTRBarEnable);
        mustUpdateDtr |= ImGui.Checkbox("Always display real weather", ref C.DTRBarRealAlways);
        ImGuiEx.Text("Click action:");
        ImGui.Indent();
        ImGuiEx.RadioButtonBool("Pause plugin", "Open plugin UI", ref C.DTRBarClickToggle);
        ImGui.Unindent();
        ImGui.Unindent();
        if(mustUpdateDtr) S.ServerBarManager.Reload();
    }
}
