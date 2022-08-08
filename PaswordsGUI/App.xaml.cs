using System;
using System.Collections.Generic;
using System.Windows;

namespace Passwords.GUI
{
    public partial class App : Application
    {
        private string host;
        private int    port;
        private ulong  hash;
        private string name;

        internal string Connection {
            get { return name.Length > 0 
                       ? name 
                       : host.Length > 0
                       ? $"{host}:{port}:{hash}"
                       : String.Empty; }
        }

        private void ApplicationStart( object sender, StartupEventArgs e )
        {
            List<string> args = new List<string>( e.Args );
            name = String.Empty;
            host = String.Empty;
            hash = 0;
            port = 5000;
            int index = 0;
            if( args.Contains("--host") ) {
                if( args.Count > ( index = args.IndexOf("--host") + 1 ) )
                    host = args[index];
                else App.Current.Shutdown(1953722216);
            }
            if( args.Contains("--port") ) {
                if( args.Count > ( index = args.IndexOf("--port") + 1 ) ) {
                    if( !int.TryParse(args[index], out port) )
                        App.Current.Shutdown(1953656688);
                } else App.Current.Shutdown(1953656688);
            }
            if( args.Contains("--name") ) {
                if( args.Count > ( index = args.IndexOf("--name") + 1 ) )
                    name = args[index];
                else App.Current.Shutdown(1701667182);
            }
            if( args.Contains("--hash") ) {
                if( args.Count > ( index = args.IndexOf("--hash") + 1 ) ) {
                    if( !ulong.TryParse( args[index], out hash ) ) {
                        
                    }
                }
            }
        }
    }
}
