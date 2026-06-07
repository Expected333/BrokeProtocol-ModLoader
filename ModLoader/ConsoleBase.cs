using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ModLoader
{
    // Token: 0x02000005 RID: 5
    public static class ConsoleBase
    {
        // Token: 0x0600000D RID: 13
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        // Token: 0x0600000E RID: 14
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        // Token: 0x0600000F RID: 15
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        // Token: 0x06000010 RID: 16 RVA: 0x0000264C File Offset: 0x0000084C
        static ConsoleBase()
        {
            ConsoleBase.AllocConsole();
            ConsoleBase.StdOutWriter = new StreamWriter(Console.OpenStandardOutput());
            ConsoleBase._stdInReader = new StreamReader(Console.OpenStandardInput());
            ConsoleBase.StdOutWriter.AutoFlush = true;
            ConsoleBase.AttachConsole(-1);
            ConsoleBase.WriteLine("[+] Console Initialized!");
        }

        // Token: 0x06000011 RID: 17 RVA: 0x0000269C File Offset: 0x0000089C
        public static void Release()
        {
            ConsoleBase.FreeConsole();
        }

        // Token: 0x06000012 RID: 18 RVA: 0x000026A8 File Offset: 0x000008A8
        public static string GetLine()
        {
            return ConsoleBase._stdInReader.ReadLine();
        }

        // Token: 0x06000013 RID: 19 RVA: 0x000026C4 File Offset: 0x000008C4
        public static void WriteLine(object line)
        {
            Console.ForegroundColor = ConsoleColor.White;
            ConsoleBase.StdOutWriter.WriteLine(line);
            Console.WriteLine(line);
        }

        // Token: 0x06000014 RID: 20 RVA: 0x000026DA File Offset: 0x000008DA
        public static void Write(object msg)
        {
            ConsoleBase.StdOutWriter.Write(msg);
            Console.Write(msg);
        }

        public static void WriteError(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            ConsoleBase.StdOutWriter.Write(msg);
            Console.Write(msg);
        }

        public static void WriteWarn(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            ConsoleBase.StdOutWriter.Write(msg);
            Console.Write(msg);
        }

        public static void WriteSucces(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            ConsoleBase.StdOutWriter.Write(msg);
            Console.Write(msg);
        }

        // Token: 0x04000011 RID: 17
        private static readonly StreamWriter StdOutWriter;

        // Token: 0x04000012 RID: 18
        private static StreamReader _stdInReader;
    }
}
