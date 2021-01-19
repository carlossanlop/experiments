using System;

namespace RedundantSegments
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Attach dotnet trace to pid {Environment.ProcessId}. Running loop...");
            while (true)
            {
                MyPath.GetFullPath("/this/is/a/path/without/redundant/segments");
            }
        }
    }
}
