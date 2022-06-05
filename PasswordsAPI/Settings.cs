using System;
using System.Collections.Generic;
using Yps;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

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
        private static PasswordServer instance;
        internal static PasswordServer Instance {
            get { if( instance == null ) {
                    instance = new PasswordServer();
                    PasswordClient.LoadKnownClientsList();
                } return instance; }
        }

        internal class TheRegistry
        {
            internal const string TheAPI = "HKEY_LOCAL_MACHINE\\SOFTWARE\\ThePasswords\\TheAPI";
            internal const string TheApp = "\\TheService";
            internal const string TheList = "\\TheClients";
            internal const string TheAgent = "{0}\\TheClients\\{1}";
        }

        internal ulong UnPackData( byte[] data )
        {
            data = Crypt.BinaryDecrypt( theKey, data );
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
            } return Crypt.BinaryEncrypt( theKey, data );
        }

        private CryptKey theKey;
        public CryptKey TheKey { get { return theKey; } }

        private PasswordServer()
        {
            string value = Registry.GetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheKeyString", string.Empty ).ToString();    
            if( value.Length == 0 ) {
                StringBuilder builder = new StringBuilder();
                Random rand = new Random((int)DateTime.Now.Ticks);
                for( int i=0; i<64; ++i ) {
                    builder.Append(rand.Next('A', 'Z' + 1));
                } value = builder.ToString(); 
                Registry.SetValue( TheRegistry.TheAPI + TheRegistry.TheApp, "TheKeyString", value );
            }
            
            theKey = Crypt.CreateKey( value );
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

            byte[] keyhash = PasswordServer.Instance.PackValue( newClient.Key.Hash );
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
                    (string)Registry.GetValue( string.Format(
                    PasswordServer.TheRegistry.TheAgent,
                    PasswordServer.TheRegistry.TheAPI,
                name), "TheIp", string.Empty ));

                byte[] data = Registry.GetValue( string.Format(
                    PasswordServer.TheRegistry.TheAgent,
                    PasswordServer.TheRegistry.TheAPI,
                name), "TheKey", Array.Empty<byte>()) as byte[];

                ulong hash = PasswordServer.Instance.UnPackData( data );
                KnownClients.Add( hash, new PasswordClient( name, ip, hash ) );
            }
            return KnownClients.Count > 0
                 ? API.Abstracts.Status.Success
                 : API.Abstracts.Status.Unknown;
        }
    }
}
