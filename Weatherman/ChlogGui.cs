/*namespace Weatherman
{
    class ChlogGui
    {
        public const int ChlogVersion = 9;
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
            ImGui.TextUnformatted("This update contains more code optimizations as well as new feature - adjustable time flow rate.\nIt also enables time change in solo duties and certain instances which have changeable time.");
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
*/