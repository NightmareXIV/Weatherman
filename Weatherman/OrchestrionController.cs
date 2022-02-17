﻿using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    class OrchestrionController : IDisposable
    {
        private Weatherman plugin;
        internal bool BGMModified = false;
        internal Dictionary<int, Song> SongList = new Dictionary<int, Song>
        {
            [0] = new Song(0, "Default")
        };

        public OrchestrionController(Weatherman p)
        {
            this.plugin = p;
        }

        public void Dispose()
        {
            
        }

        public IDalamudPlugin GetOrchestrionPlugin()
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
        }

        public void PlaySong(int id)
        {
            try
            {
                var p = GetOrchestrionPlugin();
                if (p == null) return;
                PluginLog.Debug($"Calling PlaySong id={id}");
                p.GetType().GetMethod("PlaySong").Invoke(p, new object[] { id, false });
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
                var p = GetOrchestrionPlugin();
                if (p == null) return;
                PluginLog.Debug($"Calling StopSong");
                p.GetType().GetMethod("StopSong").Invoke(p, new object[] { });
            }
            catch (Exception e)
            {
                PluginLog.Error("Failed to stop song:" + e.Message);
            }
        }

        public Dictionary<int, Song> GetSongList()
        {
            if (SongList.Count > 1) return SongList;
            try
            {
                var p = GetOrchestrionPlugin();
                if (p == null) return null;
                var flags = BindingFlags.NonPublic | BindingFlags.Static;
                var slist = p.GetType().Assembly.GetType("Orchestrion.SongList", true);
                var songlist = (IDictionary)slist.GetField("_songs", flags).GetValue(slist);
                PluginLog.Debug("Songs found: " + songlist.Count);
                int i = 0;
                foreach (var o in songlist.Keys)
                {
                    SongList.Add(++i, new Song(
                        (int)songlist[o].GetType().GetField("Id").GetValue(songlist[o]),
                        (string)songlist[o].GetType().GetField("Name").GetValue(songlist[o])
                        ));
                }
                PluginLog.Debug("Song list contains " + SongList.Count + " entries / " + i);
                //if (SongList.Count > 1) return SongList;
            }
            catch (Exception e)
            {
                PluginLog.Error("Failed to retrieve song list:" + e.Message + "\n" + e.StackTrace ?? "");
            }
            return SongList;
        }

        public Song GetSongById(int id)
        {
            foreach (var i in SongList)
            {
                if (i.Value.Id == id) return i.Value;
            }
            return new Song(id, id.ToString());
        }
    }
}
