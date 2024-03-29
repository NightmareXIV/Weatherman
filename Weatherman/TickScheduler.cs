﻿using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Weatherman
{
    internal class TickScheduler
    {
        long executeAt;
        Action function;
        IFramework framework;
        bool disposed = false;

        public TickScheduler(Action function, IFramework framework, long delayMS = 0)
        {
            this.executeAt = Environment.TickCount64 + delayMS;
            this.function = function;
            this.framework = framework;
            framework.Update += Execute;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                framework.Update -= Execute;
            }
            disposed = true;
        }

        void Execute(object _)
        {
            if (Environment.TickCount64 < executeAt) return;
            try
            {
                function();
            }
            catch (Exception e)
            {
                PluginLog.Error(e.Message + "\n" + e.StackTrace ?? "");
            }
            this.Dispose();
        }
    }
}
