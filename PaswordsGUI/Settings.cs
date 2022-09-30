using System;
using System.Collections.Generic;
using Yps;
using Microsoft.Win32;

namespace Passwords.GUI
{
    internal class PasswordClient
    {
        private static PasswordClient  instance;
        internal static PasswordClient Instance {
            get { if (instance == null) {
                    instance = new PasswordClient(
                        (App.Current as App).Connection
                    );
                } return instance; }
        }

        internal class TheRegistry
        {
            internal const string TheRoot = "HKEY_CURRENT_USER\\Software\\ThePasswords\\TheGUI";
            internal const string TheSelf = "\\TheClient";
            internal const string TheList = "\\TheNetwork";
            internal const string TheHost = "{0}\\TheNetwork\\{1}";
        }

        internal ulong UnPackData( byte[] Data )
        {
            Span<byte> data = Crypt.BinaryDecrypt( theKey, Data );
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
            byte[] data = new byte[9];
            unsafe { byte* ptr = (byte*)&value;
                for( int i = 0; i < 8; ++i ) {
                    data[i] = ptr[i];
                } data[8] = 0;
            } return Crypt.BinaryEncrypt( theKey, data ).ToArray();
        }

        private CryptKey theKey;
        internal CryptKey Key { get { return theKey; } }
        internal string  TheAPI;

        private PasswordClient(string connect)
        {
            // The Passwords -> The API -> The GUI  only can act as a Passwords Client (fetching Passwords via Passwords API
            // from a Passwords Server) if application is registered with
            // it's unique client id which the software installer autogenerates during installation of the client application
            // and which then will be registered with actually running Desktop sessions user account.       
            string client = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheSelf, "TheAgent", string.Empty ).ToString();
            
            // If actually running desktop session cannot provide a valid PasswordsAPI Client key (which should be registered
            // for this application during installation progress) the application will not run and the process will terminate.
            if (client.Length == 0) App.Current.Shutdown();
            
            // if desktop session provides a valid registration id the application then generates a cryption key by using
            // it's registration id value then for that keys password value (which only desktop session user, the system 
            // and the application itself should be able knowing that password) so it is NOT the users own master password
            // these never will be stored anywhere else then in a users very own mind - instead this is agent-key is used
            // for identifying clients/useragents at a PasswordsAPI Server and authorizes them for being allowed feching
            // encrypted password data for the actually running desktop sessions user who stores passwords on the server
            theKey = Crypt.CreateKey( client );
            client = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheSelf, "TheName", string.Empty ).ToString();
            if( client.Length == 0 ) 
                Registry.SetValue( TheRegistry.TheRoot + TheRegistry.TheSelf, "TheName",
                                   Consola.Utility.NameOfTheMachinery(),
                                   RegistryValueKind.String );

            string start = Consola.Utility.PathOfTheCommander();
            client = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheSelf, "ThePath", string.Empty).ToString();
            if( client != start )
                Registry.SetValue( TheRegistry.TheRoot + TheRegistry.TheSelf, "ThePath",
                                   start, RegistryValueKind.String );

            // when is clear that current running process is a valid PasswordsAPI Client application, the actually configured
            // dataset will be loaded from the clients list of known server connections, which defines connection credentials 
            string theOne = Registry.GetValue( TheRegistry.TheRoot + TheRegistry.TheList, "TheHost", string.Empty ).ToString();
            theOne = string.Format( TheRegistry.TheHost, TheRegistry.TheRoot, theOne );
            byte[] crypic = Registry.GetValue( theOne, "TheKey", Array.Empty<byte>() ) as byte[];
            if( crypic.Length == 0 ) {
                TheAPI = "";
            } else {
                ulong hash = UnPackData( crypic );

                // from that dataset then an authenticator for all further traffic with the server gets initialzed
                PasswordServer.SelectedServer = new PasswordServer(
                    Registry.GetValue(theOne, "TheServer", string.Empty).ToString(),
                    (int)Registry.GetValue(theOne, "ThePort", 0), hash
                );

                // at least name of that server connection is set as flag signaling
                // that connection is configured and application is ready to use now 
                TheAPI = PasswordServer.SelectedServer.Name;
            }
        }
    }

    internal class PasswordServer
    {
        internal static PasswordServer SelectedServer;
        static PasswordServer()
        {
            SelectedServer = null;
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
                PasswordClient.TheRegistry.TheHost,
                PasswordClient.TheRegistry.TheRoot,
            byName );

            byte[]? data = Registry.GetValue( 
                theNewbe, "TheKey", Array.Empty<byte>()
            ) as byte[];

            SelectedServer = new PasswordServer(
                Registry.GetValue( theNewbe, "TheServer", string.Empty ).ToString(),
                (int)Registry.GetValue( theNewbe, "ThePort", 0 ), 
                PasswordClient.Instance.UnPackData( data ) 
            );

            Registry.SetValue( PasswordClient.TheRegistry.TheRoot
                             + PasswordClient.TheRegistry.TheList
                             , "TheHost", byName );
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
            byte[] hashdata = PasswordClient.Instance.PackValue( Crypt.CalculateHash( config.Key ) );

            string storage = string.Format(
                PasswordClient.TheRegistry.TheHost,
                PasswordClient.TheRegistry.TheRoot,
                config.Name
            );

            Registry.SetValue( storage, "TheServer", config.Url.Host );
            Registry.SetValue( storage, "ThePort", config.Url.Port );
            Registry.SetValue( storage, "TheKey", hashdata );

            return API.Abstracts.Status.Success.WithText( "Stored configuration for:" ).WithData( config.Name );
        }
    }
}
