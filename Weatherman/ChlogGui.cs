namespace Weatherman
{
    class ChlogGui
    {
        public const int ChlogVersion = 8;
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
            ImGui.TextUnformatted("This update is mainly code cleanup and optimizations.\n" +
                "It also brings Quick control tab, which allows you to temporarily change time and weather without need to change global or zone settings.");
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
