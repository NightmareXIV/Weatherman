using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    internal unsafe class ClockOffNag : IDisposable
    {
        Weatherman p;
        private bool disposedValue;

        internal ClockOffNag(Weatherman p)
        {
            this.p = p;
            Svc.Framework.Update += ClockCheck;
        }

        private void ClockCheck(Framework framework)
        {
            if(Svc.ClientState.LocalPlayer != null)
            {
                if(Math.Abs(*p.memoryManager.TrueTime - p.GetET()) > 100 && !p.configuration.NoClockNag)
                {
                    Svc.PluginInterface.UiBuilder.AddNotification("Your clock appears to be out of sync. \nPlease synchronize your time for correct plugin's functioning. \n\nYou can disable this check in settings.", "Weatherman", NotificationType.Warning, 20000);
                }
                Dispose();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
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
}
