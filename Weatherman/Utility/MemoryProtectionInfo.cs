using ECommons;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.PAGE;
using static TerraFX.Interop.Windows.MEM;

namespace Weatherman.Utility;

public static unsafe class MemoryProtectionInfo
{
    public record MemoryRegionFlags(nuint BaseAddress, nuint RegionSize, string State, string BaseProtection, bool IsReadable, bool IsWritable, bool IsExecutable, bool HasGuard, bool HasNoCache, bool HasWriteCombine, bool IsCommitted, List<string> ActiveFlags)
    {
        public override string ToString()
        {
            return $"""
                Base address : 0x{this.BaseAddress:X}
                Region size  : {this.RegionSize} bytes
                State        : {this.State}
                Protection   : {this.BaseProtection}
                Readable     : {this.IsReadable}
                Writable     : {this.IsWritable}
                Executable   : {this.IsExecutable}
                Guard        : {this.HasGuard}
                Active flags : {this.ActiveFlags.Print()}
                """;
        }
    }

    public static MemoryRegionFlags? QueryRegionFlags(void* address)
    {
        MEMORY_BASIC_INFORMATION mbi = new();
        var result = TerraFX.Interop.Windows.Windows.VirtualQuery(address, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION));

        if(result == nuint.Zero)
        {
            return null;
        }

        const uint modifiers = PAGE_GUARD | PAGE_NOCACHE | PAGE_WRITECOMBINE;
        var baseProtect = mbi.Protect & ~modifiers;

        var readable = IsReadable(baseProtect);
        var writable = IsWritable(baseProtect);
        var executable = IsExecutable(baseProtect);
        var guard = (mbi.Protect & PAGE_GUARD) != 0;
        var noCache = (mbi.Protect & PAGE_NOCACHE) != 0;
        var writeComb = (mbi.Protect & PAGE_WRITECOMBINE) != 0;
        var committed = mbi.State == MEM_COMMIT;

        return new MemoryRegionFlags((nuint)mbi.BaseAddress, mbi.RegionSize, GetStateString(mbi.State), GetProtectionString(baseProtect), readable, writable, executable, guard, noCache, writeComb, committed, MakeFlagList(baseProtect, guard, noCache, writeComb));
    }

    private static bool IsReadable(uint p)
    {
        return (p & (PAGE_READONLY | PAGE_READWRITE | PAGE_WRITECOPY | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;
    }

    private static bool IsWritable(uint p)
    {
        return (p & (PAGE_READWRITE | PAGE_WRITECOPY | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;
    }

    private static bool IsExecutable(uint p)
    {
        return (p & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;
    }

    private static string GetProtectionString(uint p)
    {
        return p switch
        {
            PAGE_NOACCESS => "PAGE_NOACCESS",
            PAGE_READONLY => "PAGE_READONLY",
            PAGE_READWRITE => "PAGE_READWRITE",
            PAGE_WRITECOPY => "PAGE_WRITECOPY",
            PAGE_EXECUTE => "PAGE_EXECUTE",
            PAGE_EXECUTE_READ => "PAGE_EXECUTE_READ",
            PAGE_EXECUTE_READWRITE => "PAGE_EXECUTE_READWRITE",
            PAGE_EXECUTE_WRITECOPY => "PAGE_EXECUTE_WRITECOPY",
            0 => "NONE / FREE",
            _ => $"UNKNOWN (0x{p:X})"
        };
    }

    private static string GetStateString(uint s)
    {
        return s switch
        {
            MEM_COMMIT => "MEM_COMMIT",
            MEM_RESERVE => "MEM_RESERVE",
            MEM_FREE => "MEM_FREE",
            _ => $"UNKNOWN (0x{s:X})"
        };
    }

    private static List<string> MakeFlagList(uint baseProtect, bool guard, bool noCache, bool writeCombine)
    {
        var list = new List<string>();
        if(baseProtect != 0)
        {
            list.Add(GetProtectionString(baseProtect));
        }

        if(guard)
        {
            list.Add("PAGE_GUARD");
        }

        if(noCache)
        {
            list.Add("PAGE_NOCACHE");
        }

        if(writeCombine)
        {
            list.Add("PAGE_WRITECOMBINE");
        }

        return list;
    }
}
