using Dalamud;
using Dalamud.Hooking;
using PInvoke;

namespace Weatherman;

internal unsafe class MemoryManager : IDisposable
{
    private Weatherman p;
    internal byte[] NewTimeAsm = [0x49, 0xC7, 0xC1, 0x00, 0x00, 0x00, 0x00];
    private byte[] OldTimeAsm = [0x4D, 0x8B, 0x8A, 0x78, 0x17, 0x00, 0x00];
    internal IntPtr TimeAsmPtr;
    private byte* FirstByteTimeAsm;
    private uint* Time;
    internal long TrueTime => FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->ClientTime.EorzeaTime;

    internal byte[] NewWeatherAsm = [0xB2, 0x00, 0x90, 0x90];
    private byte[] OldWeatherAsm = [0x0F, 0xB6, 0x50, 0x26];
    internal IntPtr WeatherAsmPtr;
    private byte* FirstByteWeatherAsm;
    private byte* Weather;
    internal byte* TrueWeather;

    private delegate IntPtr PlayWeatherSound(IntPtr a1, byte weatherId, float a3);
    private Hook<PlayWeatherSound> PlayWeatherSoundHook;

    internal bool SetWeather(byte newValue)
    {
        if(IsWeatherCustom())
        {
            *Weather = newValue;
            return true;
        }
        else
        {
            return false;
        }
    }

    internal byte GetWeather()
    {
        return *Weather;
    }

    internal byte GetDisplayedWeather()
    {
        return IsWeatherCustom() ? *Weather : *TrueWeather;
    }

    internal void EnableCustomWeather()
    {
        if(!IsWeatherCustom()) SafeMemory.WriteBytes(WeatherAsmPtr, NewWeatherAsm);
    }

    internal void DisableCustomWeather()
    {
        if(IsWeatherCustom()) SafeMemory.WriteBytes(WeatherAsmPtr, OldWeatherAsm);
    }

    internal bool IsWeatherCustom()
    {
        return *FirstByteWeatherAsm == NewWeatherAsm[0];
    }

    internal bool SetTime(uint newValue)
    {
        if(IsTimeCustom())
        {
            *Time = newValue;
            return true;
        }
        else
        {
            return false;
        }
    }

    internal uint GetTime()
    {
        return *Time;
    }
    internal void EnableCustomTime()
    {
        if(!IsTimeCustom())
        {
            SafeMemory.WriteBytes(TimeAsmPtr, NewTimeAsm);
            var result = Kernel32.GetLastError();
            PluginLog.Debug($"EnableCustomTime result: {result}");
        }
    }

    internal void DisableCustomTime()
    {
        if(IsTimeCustom())
        {
            SafeMemory.WriteBytes(TimeAsmPtr, OldTimeAsm);
            var result = Kernel32.GetLastError();
            PluginLog.Debug($"DisableCustomTime result: {result}");
        }
    }

    internal bool IsTimeCustom()
    {
        return *FirstByteTimeAsm == NewTimeAsm[0];
    }

    internal IntPtr PlayWeatherSoundDetour(IntPtr a1, byte weatherId, float a3)
    {
        //PluginLog.Debug($"Called PlayWeatherSoundDetour {weatherId}, {a3}");
        if(IsWeatherCustom())
        {
            weatherId = GetDisplayedWeather();
            //PluginLog.Debug($"Weather ID was replaced to {weatherId}");
        }
        return PlayWeatherSoundHook.Original(a1, weatherId, a3);
    }

    public MemoryManager(Weatherman p)
    {
        this.p = p;

        if(Svc.SigScanner.TryScanText("48 89 5C 24 ?? 56 57 41 57 48 83 EC 40 45 33 FF", out var ptr))
        {
            PluginLog.Information($"PlayWeatherSound ptr: {ptr:X16}");
            PlayWeatherSoundHook = Svc.Hook.HookFromAddress<PlayWeatherSound>(ptr, PlayWeatherSoundDetour);
            PlayWeatherSoundHook.Enable();
        }
        else
        {
            PluginLog.Warning("PlayWeatherSound function was not found, sound changing will be disabled");
        }

        //setup time
        TimeAsmPtr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 30 4C 8B 15") + 0x19;
        if(Static.VirtualProtect(
            (UIntPtr)(TimeAsmPtr + 0x3).ToPointer(), (IntPtr)0x4,
            Static.MemoryProtection.ExecuteReadWrite, out var oldProtection) == false)
        {
            throw new Exception("VirtualProtextEx failed");
        }
        if(!SafeMemory.ReadBytes(TimeAsmPtr, 7, out var readOldTimeAsm))
        {
            throw new Exception("Could not read memory");
        }
        else
        {
            if(!readOldTimeAsm.SequenceEqual(OldTimeAsm))
            {
                throw new Exception("Time memory is different from expected. If the game have just updated, " +
                    "you might have to wait for newer version of plugin.");
            }
        }
        Time = (uint*)(TimeAsmPtr + 0x3);
        FirstByteTimeAsm = (byte*)TimeAsmPtr;

        //setup weather
        TrueWeather = (byte*)(*(IntPtr*)Svc.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 83 C1 10 48 89 74 24") + 0x26);
        PluginLog.Information($"Weather ptr: {(IntPtr)TrueWeather:X16}");
        WeatherAsmPtr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 30 80 B9 ?? ?? ?? ?? ?? 49 8B F8 0F 29 74 24") + 0x55;
        PluginLog.Information($"Weather asm ptr: {(IntPtr)WeatherAsmPtr:X16}");
        if(Static.VirtualProtect(
            (UIntPtr)(WeatherAsmPtr + 0x1).ToPointer(), (IntPtr)0x1,
            Static.MemoryProtection.ExecuteReadWrite, out var oldProtectionWeather) == false)
        {
            throw new Exception("VirtualProtextEx failed");
        }
        if(!SafeMemory.ReadBytes(WeatherAsmPtr, 4, out var readOldWeatherAsm))
        {
            throw new Exception("Could not read memory");
        }
        else
        {
            if(!readOldWeatherAsm.SequenceEqual(OldWeatherAsm))
            {
                throw new Exception("Weather memory is different from expected. If the game have just updated, " +
                    "you might have to wait for newer version of plugin.");
            }
        }
        Weather = (byte*)(WeatherAsmPtr + 0x1);
        FirstByteWeatherAsm = (byte*)(WeatherAsmPtr);
    }

    public void Dispose()
    {
        SafeMemory.WriteBytes(TimeAsmPtr, OldTimeAsm);
        SafeMemory.WriteBytes(WeatherAsmPtr, OldWeatherAsm);
        PlayWeatherSoundHook?.Disable();
        PlayWeatherSoundHook?.Dispose();
    }
}
