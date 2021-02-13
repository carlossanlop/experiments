using System;
using System.Diagnostics;

namespace RedundantSegments
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000000; i++)
                MyPath.GetFullPath("/home/carlos/runtime/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs");
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds.ToString());
        }
    }
}
