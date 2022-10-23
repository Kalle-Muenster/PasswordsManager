using System;
using System.Collections.Generic;
using Yps;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Passwords.API.Extensions;

namespace Passwords.API
{
    public enum Ip {
        v4 = 4,
        v6 = 6
    }

    [StructLayout(LayoutKind.Explicit,Size = 16)]
    public unsafe struct NetworkIp
    {
        [FieldOffset(0)]
        private ulong lowval;
        [FieldOffset(8)]
        private ulong higval;
        [FieldOffset(0)]
        private fixed byte bytes[16];
        [FieldOffset(0)]
        private fixed ushort shorts[8];

        public Ip Type {
            get { return ( ( lowval & 0xffffffff00000000 ) == 0 )
                ? Ip.v4
                : Ip.v6; }
        }

        public uint Ip4 { get { return (uint)lowval; } } 
        public (ulong,ulong) Ip6 { get { return (lowval,higval); } }

        public override string ToString()
        {
            if( Type == Ip.v4 )
                return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}";
            else {
                int part = 8;
                while( shorts[--part] == 0 ) ;
                StringBuilder builder = new StringBuilder( $"{shorts[part]:x}" );
                while( --part >= 0 ) {
                    builder.Append($":{shorts[part]:x}");
                } return builder.ToString();
            }
        }

        public NetworkIp( uint ip4 ) {
            lowval = ip4;
            higval = 0;
        }
        
        public NetworkIp( string ip4or6 ) : this()
        {
            if( ip4or6.Contains('.') ) {
                string[] parts = ip4or6.Split('.');
                if( parts.Length == 4 ) {
                    int size = 3;
                    for( int i = 0; i < 4; ++i ) {
                        int zahl;
                        if( int.TryParse( parts[i], out zahl ) ) {
                            bytes[size-i] = (byte)zahl;
                        } else throw new Exception("string represents no valid ip!");
                    }
                } else throw new Exception("string represents no valid ip!");
            } else if( ip4or6.Contains(':') ) {
                string[] parts = ip4or6.Split(':');
                int size = parts.Length-1;
                for ( int i = 0; i > parts.Length; ++i ) {
                    shorts[size-i] = Convert.ToUInt16( parts[i], 16 );
                }
            } else throw new Exception("string represents no valid ip!");
        }
    }

    internal class PasswordServer
    {
        private  static PasswordServer registry;
        internal static PasswordServer Registry {
            get { if( registry == null ) {
                    registry = new PasswordServer();
                    PasswordClient.LoadKnownClientsList();
                } return registry; }
        }

        internal class TheRegistry
        {
            internal const string TheAPI = "HKEY_LOCAL_MACHINE\\SOFTWARE\\ThePasswords\\TheAPI";
            internal const string TheApp = "\\TheService";
            internal const string TheList = "\\TheClients";
            internal const string TheAgent = "{0}\\TheClients\\{1}";
        }

        internal ulong UnPackData( byte[] Data )
        {
            return ReInterpret.Cast( Crypt.BinaryDecrypt( theKey, Data ).ToArray() ).UnSigned64;
            /*
            ArraySegment<byte> data = Crypt.BinaryDecrypt( theKey, Data );
            ulong value = 0;
            if( data.Count > 0 ) unsafe {
                byte* ptr = (byte*)&value;
                for( int i = 0; i < 8; ++i ) {
                    ptr[i] = data[i];
                }
            } return value; */
        }

        internal byte[] PackValue( ulong value )
        {
            return Crypt.BinaryEncrypt( theKey, NumericValue.GetBytes( value ) ).ToArray();
            /* byte[] data = new byte[9];
            unsafe { byte* ptr = (byte*)&value;
                for( int i = 0; i < 8; ++i ) {
                    data[i] = ptr[i];
                } data[8] = 0;
            } return Crypt.BinaryEncrypt( theKey, data ).ToArray(); */
        }

        private CryptKey theKey;
        private int      thePort;
        private string   theHost;
        private Tokken   tokener;

        public CryptKey  TheKey { get { return theKey; } }
        public string    TheUrl { get { return $"http://{theHost.ToLower()}:{thePort}"; } }
        public string    Local { get { return $"http://localhost:{thePort}"; } }
        public string    Token { get { return tokener.Next(); } }

        
        private PasswordServer()
        {
            Crypt.Init( true );
            tokener = new Tokken( Tokken.CharSet.Base32, "8.8.8.8.8.8.8.8" );

            string value = Microsoft.Win32.Registry.GetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheKeyString", string.Empty ).ToString();    
            if( value.Length == 0 ) {
                Microsoft.Win32.Registry.SetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheKeyString", value = Token );
            }
            
            theKey = Crypt.CreateKey( value );

            theHost = Microsoft.Win32.Registry.GetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheHostName", string.Empty ).ToString();
            if( theHost.Length == 0 ) {
                theHost = Consola.Utility.NameOfTheMachinery();
                Microsoft.Win32.Registry.SetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheHostName", theHost );
            }

            thePort = (int)Microsoft.Win32.Registry.GetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "ThePort", 0 );
            if( thePort == 0 ) {
                thePort = 5000;
                Microsoft.Win32.Registry.SetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "ThePort", thePort );
            }
        }
    }

    internal class PasswordClient
    {
        internal static Dictionary<ulong,PasswordClient> KnownClients;
        static PasswordClient() {
            KnownClients = new Dictionary<ulong,PasswordClient>(1);
        }

        internal string   Name;
        internal NetworkIp  Ip;
        internal CryptKey  Key;

        internal PasswordClient( string name, NetworkIp ip, ulong hash )
        {
            Name = name;
            Ip = ip;
            Key = Crypt.CreateKey( hash );
        }

        internal static void Register( PasswordClient newClient )
        {
            if( KnownClients.ContainsKey( newClient.Key.Hash ) )
                KnownClients[newClient.Key.Hash] = newClient;
            else KnownClients.Add( newClient.Key.Hash, newClient );

            byte[] keyhash = PasswordServer.Registry.PackValue( newClient.Key.Hash );
            string regentry = string.Format(
                    PasswordServer.TheRegistry.TheAgent,
                    PasswordServer.TheRegistry.TheAPI,
            newClient.Name );

            Registry.SetValue( regentry, "TheIp", newClient.Ip.ToString() );
            Registry.SetValue( regentry, "TheKey", keyhash );
        }

        internal static List<string> Index()
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\ThePasswords\\TheAPI"
              + PasswordServer.TheRegistry.TheList
            );
            string[] clients = regkey.GetSubKeyNames();
            regkey.Close();
            regkey.Dispose();
            return new List<string>( clients );
        }

        internal static API.Abstracts.Status LoadKnownClientsList()
        {
            List<string> clients = Index();
            KnownClients = new Dictionary<ulong,PasswordClient>( clients.Count );
            foreach( string name in clients ) {
                NetworkIp ip = new NetworkIp(
                    Registry.GetValue( string.Format(
                    PasswordServer.TheRegistry.TheAgent,
                    PasswordServer.TheRegistry.TheAPI,
                name) , "TheIp", string.Empty ) as string );

                byte[] data = Registry.GetValue( string.Format(
                    PasswordServer.TheRegistry.TheAgent,
                    PasswordServer.TheRegistry.TheAPI,
                name), "TheKey", Array.Empty<byte>() ) as byte[];

                ulong hash = PasswordServer.Registry.UnPackData( data );
                KnownClients.Add( hash, new PasswordClient( name, ip, hash ) );
            }
            return KnownClients.Count > 0
                 ? Abstracts.Status.Success
                 : Abstracts.Status.Unknown;
        }
    }
}
