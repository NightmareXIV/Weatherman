using System.Globalization;
using System.Runtime.InteropServices;

namespace Weatherman;

internal static unsafe class Static
{
    [DllImport("kernel32.dll")]
    internal static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress,
      IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);
    [DllImport("kernel32.dll")]
    internal static extern bool VirtualProtect(UIntPtr lpAddress,
      IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);
    public enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    public static string ToHexString(this byte[] array)
    {
        StringBuilder b = new();
        foreach(var el in array)
        {
            b.Append(Convert.ToString(el, 16).PadLeft(2, '0'));
            b.Append(" ");
        }
        return b.ToString().ToUpper();
    }

    internal static DateTimeOffset AlreadyLocal(this DateTimeOffset d)
    {
        return d.Subtract(d.Offset);
    }

    internal static void ValidateRange(ref int source, int a, int b)
    {
        if(source < a) source = a;
        if(source > b) source = b;
    }

    internal static void ValidateRange(ref float source, float a, float b)
    {
        if(source < a) source = a;
        if(source > b) source = b;
    }

    internal static bool TryFindBytes(this byte[] haystack, byte[] needle, out int pos)
    {
        var len = needle.Length;
        var limit = haystack.Length - len;
        for(var i = 0; i <= limit; i++)
        {
            var k = 0;
            for(; k < len; k++)
            {
                if(needle[k] != haystack[i + k]) break;
            }
            if(k == len)
            {
                pos = i;
                return true;
            }
        }
        pos = default;
        return false;
    }

    internal static bool TryFindBytes(this byte[] haystack, string needle, out int pos)
    {
        return TryFindBytes(haystack, needle.Split(" ").Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray(), out pos);
    }
}
