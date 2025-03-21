using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman.Services;
public unsafe class EventWatcher : IDisposable
{
    private EventWatcher()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "_NaviMap", OnReceiveEvent);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "_NaviMap", OnReceiveEvent);
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var evt = (AddonReceiveEventArgs)args;
        PluginLog.Information($"Event!");
    }
}
