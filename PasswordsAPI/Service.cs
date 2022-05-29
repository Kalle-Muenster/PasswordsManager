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

namespace Passwords.API
{
    public partial class Service : ServiceBase
    {
        private System.ComponentModel.IContainer components = null;

        internal System.Threading.Tasks.Task task; 

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
            task = new System.Threading.Tasks.Task(() =>
            {
                CreateHostBuilder(args).Build().Run();
            });
            task.Start();
            EventLog.Log = "Started!";
        }

        protected override void OnStop()
        {
            EventLog.Log = "Shutting down";
            ExitCode = 0;
            task.Wait( 100 );
            task.Dispose();
            //base.Stop();
        }

        public static IHostBuilder CreateHostBuilder( string[] args ) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
                 webBuilder => { webBuilder.UseStartup<Startup>(); } );
    }
}
#endif