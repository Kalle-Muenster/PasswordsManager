#if     DEBUG
#define BUILD_AS_DEAMON
#else
#define BUILD_AS_SERVICE
#endif

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.ServiceProcess;

namespace Passwords
{
    namespace API
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                Consola.StdStream.Init(
                     Consola.CreationFlags.AppendLog
                   | Consola.CreationFlags.NoInputLog
                   | Consola.CreationFlags.NewConsole
                );

                Consola.StdStream.Cwd = Consola.Utility.PathOfTheCommander();

                List<string> Args = new List<string>(args);
                if (Args.Contains("--key"))
                {
                    int argn = Args.IndexOf("--key") + 1;
                    System.IO.FileInfo keyfile = new System.IO.FileInfo(Args[argn]);
                    if (keyfile.Exists)
                    {
                        args[argn] = keyfile.OpenText().ReadLine();
                    }
                }

#if BUILD_AS_SERVICE
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] {
                new PasswordsAPIService()
            };
            ServiceBase.Run( ServicesToRun );
        }
#elif BUILD_AS_DEAMON
                CreateHostBuilder(args).Build().Run();
            }

            public static IHostBuilder CreateHostBuilder( string[] args ) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
                 webBuilder => {
                     webBuilder.UseUrls(new string[] {
                         PasswordServer.Registry.TheUrl,
                         PasswordServer.Registry.Local
                     });
                     webBuilder.UseStartup<Startup>();
            } );
#endif
        }
    }
}
