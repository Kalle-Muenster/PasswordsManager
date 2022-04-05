using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PasswordsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Consola.StdStream.Init(
                Consola.CreationFlags.AppendLog
               |Consola.CreationFlags.NoInputLog
               |Consola.CreationFlags.UseConsole
            );
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
                webBuilder => { webBuilder.UseStartup<Startup>(); }  );
    }
}
