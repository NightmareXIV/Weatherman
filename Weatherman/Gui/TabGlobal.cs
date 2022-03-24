namespace Weatherman
{
    internal partial class Gui
    {
        void DrawTabGlobal()
        {
            ImGui.TextUnformatted("Global time control: ");
            ImGui.SameLine();
            HelpMarker("No override - time controlled by game;\n" +
                "Normal - time controlled by plugin, normal flow; \nFixed - time is fixed to specified value;\n"
                + "InfiniDay - instead of night, another day cycle will begin\n"
                + "InfiniDay reversed - instead of night, day cycle rewinds backward\n"
                + "InfiniNight/InfiniNight reversed - same as day options");
            ImGui.PushItemWidth(150f);
            ImGui.Combo("##timecomboglobal", ref p.configuration.GlobalTimeFlowControl, timeflowcombo, timeflowcombo.Length);
            ImGui.PopItemWidth();
            if (p.configuration.GlobalTimeFlowControl == 2)
            {
                ImGui.TextUnformatted("Set desired time of day in seconds. Double-click to edit field manually.");
                ImGui.PushItemWidth(150f);
                ImGui.DragInt("##timecontrolfixedglobal", ref p.configuration.GlobalFixedTime, 100.0f, 0, Weatherman.SecondsInDay - 1);
                if (p.configuration.GlobalFixedTime > Weatherman.SecondsInDay
                    || p.configuration.GlobalFixedTime < 0) p.configuration.GlobalFixedTime = 0;
                ImGui.PopItemWidth();
                ImGui.SameLine();
                ImGui.TextUnformatted(DateTimeOffset.FromUnixTimeSeconds(p.configuration.GlobalFixedTime).ToString("HH:mm:ss"));
            }
            ImGui.Checkbox("Enable music control", ref p.configuration.MusicEnabled);
            ImGui.TextUnformatted("Requires Orchestrion plugin installed and enabled.");
            ImGui.Checkbox("Disable plugin in cutscenes", ref p.configuration.DisableInCutscene);
            ImGui.Checkbox("Enable time control", ref p.configuration.EnableTimeControl);
            ImGui.Checkbox("Enable weather control", ref p.configuration.EnableWeatherControl);
        }
    }
}
