using System;
using System.Collections.Generic;
using Yps;
using System.Text;

using Microsoft.Win32;

namespace Passwords.GUI
{
    internal class PasswordClient
    {
        private static PasswordClient  instance;
        internal static PasswordClient Instance {
            get { if (instance == null) 
                    instance = new PasswordClient();
                return instance; }
        }

        internal class TheRegistry
        {
            internal const string TheRoot = "HKEY_CURRENT_USER\\Software\\ThePasswords\\TheGUI";
            internal const string TheApp = "\\TheUnique";
            internal const string TheList = "\\TheServers";
            internal const string TheServer = "{0}\\TheServers\\{1}";
        }

        internal ulong UnPackData( byte[] data )
        {
            data = Crypt.BinaryDecrypt( ownkey, data );
            ulong value = 0;
            unsafe {
                byte* ptr = (byte*)&value;
                for( int i = 0; i < 8; ++i ) {
                    ptr[i] = data[i];
                }
            } return value;
        }

        internal byte[] PackValue( ulong value )
        {
            byte[] data = new byte[8];
            unsafe { byte* ptr = (byte*)&value;
                for( int i = 0; i < 8; ++i ) {
                    data[i] = ptr[i];
                }
            } return Crypt.BinaryEncrypt( ownkey, data);
        }

        private CryptKey ownkey;
        internal string  TheAPI;

        private PasswordClient()
        {
            string value = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheApp, "TheId", string.Empty ).ToString();
            if (value.Length == 0) App.Current.Shutdown();

            ownkey = Crypt.CreateKey( value );
            string theOne = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheList, "TheOne", string.Empty ).ToString();
            theOne = string.Format( TheRegistry.TheServer, TheRegistry.TheRoot, theOne );
            byte[] crypic = Registry.GetValue( theOne, "TheKey", Array.Empty<byte>() ) as byte[];
            
            PasswordServer.Selected = new PasswordServer(
                Registry.GetValue( theOne, "TheServer", string.Empty ).ToString(),
                (int)Registry.GetValue( theOne, "ThePort", 0 ), UnPackData( crypic )
            );
            TheAPI = PasswordServer.Selected.Name;
        }
    }

    internal class PasswordServer
    {
        internal static PasswordServer Selected;
        static PasswordServer()
        {
            Selected = null;
        }

        internal string   Name;
        internal Uri      Url;
        internal CryptKey Key;

        internal PasswordServer(string host,int port,ulong hash)
        {
            Name = host;
            Url = new Uri( $"http://{host}:{port}/" );
            Key = Crypt.CreateKey( hash );
        }

        internal static void Select( string byName )
        {
            string theNewbe = string.Format(
                PasswordClient.TheRegistry.TheServer, 
                PasswordClient.TheRegistry.TheRoot,
            byName );

            byte[]? data = Registry.GetValue( 
                theNewbe, "TheKey", Array.Empty<byte>()
            ) as byte[];

            Selected = new PasswordServer(
                Registry.GetValue( theNewbe, "TheServer", string.Empty ).ToString(),
                (int)Registry.GetValue( theNewbe, "ThePort", 0), 
                PasswordClient.Instance.UnPackData( data ) 
            );

            Registry.SetValue( PasswordClient.TheRegistry.TheRoot
                             + PasswordClient.TheRegistry.TheList
                             , "TheOne", byName );
        }

        internal static List<string> Index()
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey(
                "Software\\ThePasswords\\TheGUI"
              + PasswordClient.TheRegistry.TheList
            );
            string[] servers = regkey.GetSubKeyNames();
            regkey.Close();
            regkey.Dispose();
            return new List<string>( servers );
        }

        private ServerConfig.Model config;

        internal static API.Abstracts.Status Store( ServerConfig.Model config )
        {
            if (!config.IsValid()) return API.Abstracts.Status.Invalid.WithData( config );
            byte[] hashdata = PasswordClient.Instance.PackValue( Crypt.CalculateHash(config.Key) );

            string storage = string.Format(
                PasswordClient.TheRegistry.TheServer,
                PasswordClient.TheRegistry.TheRoot,
                config.Name
            );

            Registry.SetValue( storage, "TheServer", config.Url.Host );
            Registry.SetValue( storage, "ThePort", config.Url.Port );
            Registry.SetValue( storage, "TheKey", hashdata );

            return API.Abstracts.Status.Success.WithText("Stored configuration for:").WithData( config.Name );
        }
    }
}
