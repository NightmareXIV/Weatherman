namespace Weatherman
{
    class OrchestrionController : IDisposable
    {
        private Weatherman plugin;
        internal bool BGMModified = false;
        internal List<Song> SongList = new()
        {
            new Song(0, "Default")
        };

        public OrchestrionController(Weatherman p)
        {
            this.plugin = p;
        }

        public void Dispose()
        {
            
        }

        /*public IDalamudPlugin GetOrchestrionPlugin()
        {
            try
            {
                var pluginManager = Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, new object[] { }, null);
                var installedPlugins = (System.Collections.IList)pluginManager.GetType().GetProperty("InstalledPlugins").GetValue(pluginManager);

                foreach (var t in installedPlugins)
                {
                    if ((string)t.GetType().GetProperty("Name").GetValue(t) == "Orchestrion")
                    {
                        return (IDalamudPlugin)t.GetType().GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(t);
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                PluginLog.Error("Can't find orchestrion plugin: " + e.Message);
                PluginLog.Error(e.StackTrace);
                return null;
            }
        }*/

        public void PlaySong(int id)
        {
            try
            {
                PluginLog.Debug("Invoked PlaySong " + id);
                Svc.PluginInterface.GetIpcSubscriber<int, bool>("Orch.PlaySong").InvokeFunc(id);
            }
            catch (Exception e)
            {
                PluginLog.Error("Failed to play song:" + e.Message);
            }
        }

        public void StopSong()
        {
            try
            {
                PluginLog.Debug("Invoked StopSong");
                Svc.PluginInterface.GetIpcSubscriber<int, bool>("Orch.PlaySong").InvokeFunc(0);
            }
            catch (Exception e)
            {
                PluginLog.Error("Failed to stop song:" + e.Message);
            }
        }

        public List<Song> GetSongList()
        {
            if (SongList.Count > 1) return SongList;
            try
            {
                SongList.AddRange(Svc.PluginInterface.GetIpcSubscriber<List<Song>>("Orch.AllSongInfo").InvokeFunc().Where(x => x.Id != 0));
            }
            catch (Exception e)
            {
                //PluginLog.Error("Failed to retrieve song list:" + e.Message + "\n" + e.StackTrace ?? "");
            }
            return SongList;
        }

        public Song GetSongById(int id)
        {
            var a = SongList.Find(x => x.Id == id);
            if (a != null) return a;
            return new Song(id, id.ToString());
        }
    }
}
