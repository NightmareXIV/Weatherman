using ECommons.Funding;
using ECommons.ImGuiMethods;

namespace Weatherman
{
    unsafe partial class Gui
    {
        private Weatherman p;
        private int curW = 0;
        private Vector4 colorGreen = new(0,1,0,1);
        internal bool configOpen = false;
        static string[] timeflowcombo = ["No override", "Normal", "Fixed", "InfiniDay", "InfiniDay reversed", "InfiniNight", "InfiniNight reversed", "Real world time"];
        bool configWasOpen = false;
        int uid = 0;
        string filter = "";
        string musicFilter = "";

        public Gui(Weatherman p)
        {
            this.p = p;
        }

        public void Draw()
        {
            try
            {
                if (!configOpen)
                {
                    if (configWasOpen)
                    {
                        p.configuration.Save();
                        PluginLog.Debug("Configuration saved");
                    }
                    configWasOpen = false;
                    return;
                }
                uid = 0;
                configWasOpen = true;
                if (!p.configuration.ConfigurationString.Equals(p.configuration.GetConfigurationString()))
                {
                    p.configuration.Save();
                    PluginLog.Debug("Configuration saved");
                }
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(100, 100));
                if (ImGui.Begin("Weatherman 2.0", ref configOpen))
                {
                    PatreonBanner.DrawRight();
                    ImGui.BeginTabBar("weatherman_settings");
                    if (ImGui.BeginTabItem("Quick control"))
                    {
                        DrawTabQuickControl();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Global setting"))
                    {
                        DrawTabGlobal();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Zone-specific settings"))
                    {
                        DrawTabZone();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Weather blacklist"))
                    {
                        DrawTabBlacklist();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Debug"))
                    {
                        DrawTabDebug();
                        ImGui.EndTabItem();
                    }
                    PatreonBanner.RightTransparentTab();
                    ImGui.EndTabBar();
                }
                ImGui.PopStyleVar();
                ImGui.End();
            }
            catch(Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
            }
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
