using Dalamud;
using Dalamud.Hooking;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

namespace Weatherman.Services;

public unsafe class MemoryManager : IDisposable
{
    internal long TrueTime => FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->ClientTime.EorzeaTime;

    internal byte* TrueWeather = (byte*)(((nint)EnvManager.Instance()) + 0x26);

    internal EzPatchWithPointer<uint> RenderSunlightShadowPatch = new("49 0F BE 40 ?? 84 C0", 0, 
        new("49 0F BE 40 24", "B8 00 00 00 00"), 1, autoEnable: false);
    internal EzPatchWithPointer<byte> RenderWeatherPatch = new("48 89 5C 24 ?? 57 48 83 EC 30 80 B9 ?? ?? ?? ?? ?? 49 8B F8 0F 29 74 24 ?? 48 8B D9 0F 28 F1", 0x55, 
        new("0F B6 50 26", "B2 00 90 90"), 1, autoEnable:false);
    internal EzPatchWithPointer<uint> RenderTimePatch = new("48 89 5C 24 ?? 57 48 83 EC 30 4C 8B 15", 0x19, 
        new("4D 8B 8A 78 17 00 00", "49 C7 C1 00 00 00 00"), 3, autoEnable: false);

    private delegate nint PlayWeatherSound(nint a1, byte weatherId, float a3, nint a4);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 30 45 33 F6 0F 29 74 24")]
    private EzHook<PlayWeatherSound> PlayWeatherSoundHook;
    nint PlayWeatherSoundDetour(nint a1, byte weatherId, float a3, nint a4)
    {
        //PluginLog.Debug($"Called PlayWeatherSoundDetour {weatherId}");
        if(IsWeatherCustom())
        {
            weatherId = GetDisplayedWeather();
            //PluginLog.Debug($"Weather ID was replaced to {weatherId}");
        }
        return PlayWeatherSoundHook.Original(a1, weatherId, a3, a4);
    }

    delegate byte UpdateBgmSituation(nint a1, ushort bgmSituationId, int column, nint a4, nint a5);
    [EzHook("48 89 5C 24 ?? 57 48 83 EC 20 B8 ?? ?? ?? ?? 49 8B F9 41 8B D8")]
    EzHook<UpdateBgmSituation> UpdateBgmSituationHook;
    byte UpdateBgmSituationDetour(nint a1, ushort bgmSituationId, int column, nint a4, nint a5)
    {
        //PluginLog.Information($"{column}");
        if(IsTimeCustom() && column != 3)
        {
            var seconds = GetTime() % 86400;
            var isDay = seconds >= 21600 && seconds < 64800;
            column = isDay ? 1 : 2;
            //PluginLog.Information($"{column}");
        }
        return UpdateBgmSituationHook.Original(a1, bgmSituationId, column, a4, a5);
    }

    internal bool SetWeather(byte newValue)
    {
        if(IsWeatherCustom())
        {
            RenderWeatherPatch.PointerValue = newValue;
            if(RenderSunlightShadowPatch.Enabled)
            {
                RenderSunlightShadowPatch.PointerValue = S.DataProvider.ZoneToWeatherIndexMap[Svc.ClientState.TerritoryType][newValue];
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    internal byte GetCustomWeather()
    {
        return RenderWeatherPatch.PointerValue;
    }

    internal byte GetDisplayedWeather()
    {
        return IsWeatherCustom() ? GetCustomWeather() : *TrueWeather;
    }

    internal void EnableCustomWeather()
    {
        if(!IsWeatherCustom())
        {
            RenderWeatherPatch.Enable();
            RenderSunlightShadowPatch.Enable();
        }
    }

    internal void DisableCustomWeather()
    {
        if(IsWeatherCustom())
        {
            RenderWeatherPatch.Disable();
            RenderSunlightShadowPatch.Disable();
        }
    }

    internal bool IsWeatherCustom()
    {
        return RenderWeatherPatch.Enabled;
    }

    internal bool SetTime(uint newValue)
    {
        if(IsTimeCustom())
        {
            RenderTimePatch.PointerValue = newValue;
            return true;
        }
        else
        {
            return false;
        }
    }

    internal uint GetTime()
    {
        return RenderTimePatch.PointerValue;
    }
    internal void EnableCustomTime()
    {
        if(!IsTimeCustom())
        {
            RenderTimePatch.Enable();
        }
    }

    internal void DisableCustomTime()
    {
        if(IsTimeCustom())
        {
            RenderTimePatch.Disable();
        }
    }

    internal bool IsTimeCustom()
    {
        return RenderTimePatch.Enabled;
    }

    public MemoryManager()
    {
        EzSignatureHelper.Initialize(this);
    }

    public void Dispose()
    {
    }
}
