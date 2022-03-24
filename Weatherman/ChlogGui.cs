namespace Weatherman
{
    class ChlogGui
    {
        public const int ChlogVersion = 7;
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
            ImGui.TextUnformatted("Fixed occasional crash on login/teleport with music control enabled\n" +
                "and implemented measures against crashes on errors.");
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
