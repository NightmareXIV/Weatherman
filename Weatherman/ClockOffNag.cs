using Dalamud.Game;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices.Legacy;
using Weatherman.Services;

namespace Weatherman;

internal unsafe class ClockOffNag : IDisposable
{
    private Weatherman p;
    private bool disposedValue;

    internal ClockOffNag(Weatherman p)
    {
        this.p = p;
        Svc.Framework.Update += ClockCheck;
    }

    private void ClockCheck(IFramework framework)
    {
        if(Svc.ClientState.LocalPlayer != null)
        {
            var tt = S.MemoryManager.TrueTime;
            var et = p.GetET();
            if(Math.Abs(tt - et) > 500 && !p.Config.NoClockNag)
            {
                Svc.PluginInterface.UiBuilder.AddNotification("Your clock appears to be out of sync. \nPlease synchronize your time for correct plugin's functioning. \n\nYou can disable this check in settings.", "Weatherman", NotificationType.Warning, 20000);
                PluginLog.Warning($"Clock out of sync: local: {tt}, calculated: {et}, diff: {tt - et}");
            }
            Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposedValue)
        {
            if(disposing)
            {
                Svc.Framework.Update -= ClockCheck;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ClockOffNag()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
