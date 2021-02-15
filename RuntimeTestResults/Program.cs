using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RuntimeTestResults.Data;

namespace RuntimeTestResults
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "kusto")
            {
                using var updater = new DatabaseUpdater();
                updater.Update();
            }
            else
            {
                CreateHostBuilder(args).Build().Run();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
