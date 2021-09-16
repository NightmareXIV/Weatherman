using Dalamud;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    unsafe class MemoryManager:IDisposable
    {
        Weatherman p;
        internal byte[] NewTimeAsm = new byte[] { 0x49, 0xC7, 0xC1, 0x00, 0x00, 0x00, 0x00 };
        private byte[] OldTimeAsm = new byte[] { 0x4D, 0x8B, 0x8A, 0x08, 0x16, 0x00, 0x00 };
        internal IntPtr TimeAsmPtr;
        private byte* FirstByteTimeAsm;
        private uint* Time;
        internal long* TrueTime;

        internal byte[] NewWeatherAsm = new byte[] { 0xB2, 0x00, 0x90, 0x90 };
        private byte[] OldWeatherAsm = new byte[] { 0x0F, 0xB6, 0x50, 0x26 };
        internal IntPtr WeatherAsmPtr;
        private byte* FirstByteWeatherAsm;
        private byte* Weather;
        internal byte* TrueWeather;

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
            if(!IsTimeCustom()) SafeMemory.WriteBytes(TimeAsmPtr, NewTimeAsm);
        }

        internal void DisableCustomTime()
        {
            if(IsTimeCustom()) SafeMemory.WriteBytes(TimeAsmPtr, OldTimeAsm);
        }

        internal bool IsTimeCustom()
        {
            return *FirstByteTimeAsm == NewTimeAsm[0];
        }

        public MemoryManager(Weatherman p)
        {
            this.p = p;

            //setup time
            TrueTime = (long*)(Svc.Framework.Address.BaseAddress + 0x1608);
            TimeAsmPtr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 30 4C 8B 15") + 0x16;
            if (Static.VirtualProtect(
                (UIntPtr)(TimeAsmPtr + 0x3).ToPointer(), (IntPtr)0x4,
                Static.MemoryProtection.ExecuteReadWrite, out var oldProtection) == false)
            {
                throw new Exception("VirtualProtextEx failed");
            }
            if (!SafeMemory.ReadBytes(TimeAsmPtr, 7, out var readOldTimeAsm))
            {
                throw new Exception("Could not read memory");
            }
            else
            {
                if (!readOldTimeAsm.SequenceEqual(OldTimeAsm))
                {
                    throw new Exception("Time memory is different from expected. If the game have just updated, " +
                        "you might have to wait for newer version of plugin.");
                }
            }
            Time = (uint*)(TimeAsmPtr + 0x3);
            FirstByteTimeAsm = (byte*)TimeAsmPtr;

            //setup weather
            TrueWeather = (byte*)(*(IntPtr*)Svc.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 0F B6 EA 48 8B F9 41 8B DE 48 8B 70 08 48 85 F6 0F 84 ?? ?? ?? ??") + 0x27);
            WeatherAsmPtr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 30 80 B9 ?? ?? ?? ?? ?? 49 8B F8 0F 29 74 24") + 0x55;
            if (Static.VirtualProtect(
                (UIntPtr)(WeatherAsmPtr + 0x1).ToPointer(), (IntPtr)0x1,
                Static.MemoryProtection.ExecuteReadWrite, out var oldProtectionWeather) == false)
            {
                throw new Exception("VirtualProtextEx failed");
            }
            if (!SafeMemory.ReadBytes(WeatherAsmPtr, 4, out var readOldWeatherAsm))
            {
                throw new Exception("Could not read memory");
            }
            else
            {
                if (!readOldWeatherAsm.SequenceEqual(OldWeatherAsm))
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
        }
    }
}
