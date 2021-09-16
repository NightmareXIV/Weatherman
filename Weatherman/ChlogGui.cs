using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    class ChlogGui
    {
        public const int ChlogVersion = 3;
        readonly Weatherman p;
        bool open = true;
        public ChlogGui(Weatherman p)
        {
            this.p = p;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
        }

        public void Dispose()
        {
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
        }

        void Draw()
        {
            if (!open) return;
            if (!Svc.ClientState.IsLoggedIn) return;
            ImGui.Begin("Weatherman has been updated", ref open, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.TextUnformatted("Meet Weatherman 2.0!\n" +
                "Fundamental principle of the plugin has been changed.\n" +
                "Instead of setting time and weather variables in memory, Weatherman 2.0 patches code directly, \n" +
                "   allowing game to preserve it's true variables while changing only visuals.\n" +
                "- Eorzean clock is no longer affected by Weatherman\n" +
                "- BGM no longer affected by Weatherman\n" +
                "- Vistas no longer affected by Weatherman\n" +
                "- DOL pause was removed and no longer needed as game will always have it's true information\n" +
                "- Weather will no longer jump back and forth on quest completion or just randomly\n" +
                "- Weather changes are now always instant\n" +
                "- Anamnesis program no longer affected by Weatherman\n" +
                "\n" +
                "Big updates, however, means that big testing is needed. \n" +
                "Please report about all encountered errors and unexpected behavior of plugin.\n" +
                "Thank you for using Weatherman 2.0!");
            if (ImGui.Button("Close this window"))
            {
                open = false;
            }
            ImGui.End();
            if (!open) Close();
        }

        void Close()
        {
            p.configuration.ChlogReadVer = ChlogVersion;
            p.configuration.Save();
            this.Dispose();
        }
    }
}
