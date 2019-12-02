using System;
using System.IO;
using System.Runtime.InteropServices;

namespace XCom2ModTool
{
    /// <summary>
    /// Interop for the native LZO compression library.
    /// </summary>
    internal class Lzo
    {
        private const int LZO_E_OK = 0;
        private const int LZO_E_ERROR = -1;
        private const int LZO_E_OUT_OF_MEMORY = -2;
        private const int LZO_E_NOT_COMPRESSIBLE = -3;
        private const int LZO_E_INPUT_OVERRUN = -4;
        private const int LZO_E_OUTPUT_OVERRUN = -5;
        private const int LZO_E_LOOKBEHIND_OVERRUN = -6;
        private const int LZO_E_EOF_NOT_FOUND = -7;
        private const int LZO_E_INPUT_NOT_CONSUMED = -8;

        private static byte[] wrkmem = new byte[131072]; // Allowing for 64-bit.

        static Lzo()
        {
            var init = 0;
            if (Is64Bit())
            {
                init = __lzo_init_v2(1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
            }
            else
            {
                init = __lzo_init_v2_32(1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
            }

            if (init != 0)
            {
                throw new Exception("LZO initialization failed");
            }
        }

        protected static bool Is64Bit() => IntPtr.Size == 8;

        public static string Version
        {
            get
            {
                IntPtr ptr;
                if (Is64Bit())
                {
                    ptr = lzo_version_string();
                }
                else
                {
                    ptr = lzo_version_string32();
                }

                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public static string VersionDate
        {
            get
            {
                IntPtr ptr;
                if (Is64Bit())
                {
                    ptr = lzo_version_date();
                }
                else
                {
                    ptr = lzo_version_date32();
                }

                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public static byte[] Decompress(byte[] src, byte[] dst)
        {
            var dst_len = dst.Length;
            if (Is64Bit())
            {
                Check(lzo1x_decompress(src, src.Length, dst, ref dst_len, null));
            }
            else
            {
                Check(lzo1x_decompress32(src, src.Length, dst, ref dst_len, null));
            }
            return dst;
        }

        private static void Check(int result)
        {
            switch (result)
            {
                case LZO_E_OK:
                    return;
                case LZO_E_ERROR:
                    throw new Exception("An LZO compression error occurred");
                case LZO_E_OUT_OF_MEMORY:
                    throw new OutOfMemoryException("The LZO compressor ran out of memory");
                case LZO_E_NOT_COMPRESSIBLE:
                    throw new InvalidDataException("The LZO compressor encountered data that could not be de/compressed");
                case LZO_E_INPUT_OVERRUN:
                    throw new InvalidDataException("The LZO compressor found that input overran");
                case LZO_E_OUTPUT_OVERRUN:
                    throw new InvalidDataException("The LZO compressor found that output overran");
                case LZO_E_LOOKBEHIND_OVERRUN:
                    throw new InvalidDataException("The LZO compressor found that its 'look behind' approach overran");
                case LZO_E_EOF_NOT_FOUND:
                    throw new InvalidDataException("The LZO compressor could not find its end of stream marker");
                case LZO_E_INPUT_NOT_CONSUMED:
                    throw new InvalidDataException("The LZO compressor could not consume all its input");
                default:
                    throw new Exception($"The LZO compressor reported an unknown error ({result})");
            }
        }

        [DllImport("lzo64\\lzo2_64.dll")]
        private static extern int __lzo_init_v2(uint v, int s1, int s2, int s3, int s4, int s5, int s6, int s7, int s8, int s9);

        [DllImport("lzo32\\lzo2.dll", EntryPoint = "__lzo_init_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int __lzo_init_v2_32(uint v, int s1, int s2, int s3, int s4, int s5, int s6, int s7, int s8, int s9);

        [DllImport("lzo64\\lzo2_64.dll")]
        private static extern IntPtr lzo_version_string();

        [DllImport("lzo32\\lzo2.dll", EntryPoint = "lzo_version_string")]
        private static extern IntPtr lzo_version_string32();

        [DllImport("lzo64\\lzo2_64.dll")]
        private static extern IntPtr lzo_version_date();

        [DllImport("lzo32\\lzo2.dll", EntryPoint="lzo_version_date", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr lzo_version_date32();

        [DllImport("lzo64\\lzo2_64.dll")]
        private static extern int lzo1x_1_compress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);

        [DllImport("lzo32\\lzo2.dll", EntryPoint = "lzo1x_1_compress", CallingConvention = CallingConvention.Cdecl)]
        private static extern int lzo1x_1_compress32(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);

        [DllImport("lzo64\\lzo2_64.dll")]
        private static extern int lzo1x_decompress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);

        [DllImport("lzo32\\lzo2.dll", EntryPoint = "lzo1x_decompress", CallingConvention = CallingConvention.Cdecl)]
        private static extern int lzo1x_decompress32(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);
    }
}
