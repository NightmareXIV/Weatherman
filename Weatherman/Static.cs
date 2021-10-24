using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    unsafe static class Static
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
			StringBuilder b = new StringBuilder();
			foreach(var el in array)
            {
				b.Append(Convert.ToString(el, 16).PadLeft(2, '0'));
				b.Append(" ");
            }
			return b.ToString().ToUpper() ;
        }

		public static DateTimeOffset AlreadyLocal(this DateTimeOffset d)
        {
			return d.Subtract(d.Offset);
        }
    }
}
