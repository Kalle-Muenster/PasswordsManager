#if DEBUG
#define BUILD_AS_DEAMON
#else
#define BUILD_AS_SERVICE
#endif

#if !BUILD_AS_DEAMON
using System;
using System.ServiceProcess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
//using System.Windows.Forms;

namespace Passwords.API
{
    public partial class Service
        : ServiceBase
    {
        private  System.ComponentModel.IContainer components = null;
        private  System.Threading.Tasks.Task task;
        internal IHost                       host;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        public Service()
        {
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            ServiceName = "PasswordsAPI";
            InitializeComponent();
        }

        protected override void OnStart( string[] args )
        {
            host = CreateHostBuilder( args ).Build();
            task = host.RunAsync();
            EventLog.Log = "Started!";
        }

        protected override void OnStop()
        {
            host.StopAsync().GetAwaiter().GetResult();
            EventLog.Log = "Stopped!";
            ExitCode = -1;
            // task.Wait();
        }

        public static IHostBuilder CreateHostBuilder( string[] args ) =>
            Host.CreateDefaultBuilder( args ).ConfigureWebHostDefaults(
                 webBuilder => {
                     webBuilder.UseUrls( new string[] {
                         PasswordServer.Registry.TheUrl,
                         PasswordServer.Registry.Local }
                                          );
                 webBuilder.UseStartup<Startup>();
        } );
    }
}
#endif