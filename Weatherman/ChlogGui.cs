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
        public const int ChlogVersion = 4;
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
            ImGui.TextUnformatted("API 4 version.\n" +
                "Orchestrion functions are temporary unavailable and will be updated later.\n" +
                "(or maybe removed at all since this function gets built into orchestrion plugin itself some time soon)");
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
