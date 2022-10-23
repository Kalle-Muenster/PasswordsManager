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

namespace Passwords.API
{
    public partial class PasswordsAPIService
        : ServiceBase
    {
        private  System.ComponentModel.IContainer components = null;
        private  System.Threading.Tasks.Task task;
        internal IHost                       host;

        public enum Commands : int
        {
            CreateDump = 1,
            RemoveUser = 2,
            RenameUser = 3,
            NewUserKey = 4,
            DumpApiKey = 5
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing && (components != null) ) {
                components.Dispose();
            } base.Dispose( disposing );
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        public PasswordsAPIService()
        {
            ExitCode = 1;
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            ServiceName = "PasswordsAPI";
            InitializeComponent();
        }

        protected override void OnStart( string[] args )
        {
            host = CreateHostBuilder( args, this ).Build();
            task = host.RunAsync();
            EventLog.WriteEntry( "Service successfully started", EventLogEntryType.Information );
        }

        protected override void OnStop()
        {
            ExitCode = 0;
            host.StopAsync();
            EventLog.WriteEntry( "Service has been stopped", EventLogEntryType.Information );
        }

        protected override void OnCustomCommand( int adminapi )
        {
            Commands command = (Commands)adminapi;
            EventLog.WriteEntry(
                $"Service received {command} command",
                EventLogEntryType.Information, adminapi
            );
            switch( command ) {
                case Commands.CreateDump: break;
                case Commands.NewUserKey: break;
                case Commands.RemoveUser: break;
                case Commands.RenameUser: break;
                case Commands.DumpApiKey: break;
            } base.OnCustomCommand( adminapi );
        }

        public static IHostBuilder CreateHostBuilder( string[] args, PasswordsAPIService serv ) =>
            Host.CreateDefaultBuilder( args ).ConfigureWebHostDefaults(
                 webBuilder => {
                     Startup.Service = serv;
                     webBuilder.UseUrls( new string[] {
                         PasswordServer.Registry.TheUrl,
                         PasswordServer.Registry.Local }
                                          );
                     webBuilder.UseStartup<Startup>();
                 }
            );
    }
}
#endif