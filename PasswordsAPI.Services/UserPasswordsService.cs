using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using Yps;


namespace Passwords.API.Services
{
    public class UserPasswordsService<CTX>
        : AbstractApiService<UserPasswords,UserPasswordsService<CTX>,CTX>
    where CTX
        : PasswordsApiDbContext<CTX>
    {
        private readonly Status PasswordServiceError = new Status(ResultCode.Service|ResultCode.Password|ResultCode.IsError);
        private readonly Status InvalidId = new Status(ResultCode.Service|ResultCode.Password|ResultCode.User|ResultCode.Invalid, "Invalid User.Id: {0}");
        private readonly Status HashValue = new Status(ResultCode.Service|ResultCode.Password|ResultCode.Id|ResultCode.Invalid, "password incorrct '{0}'" );
        protected override Status GetDefaultError() { return PasswordServiceError; }
        protected override ResultCode GetServiceFlags() { return ResultCode.Password; }
        protected override UserPasswords GetStatusEntity(Status cast) { return cast; }


        private PasswordUsersService<CTX>        _usrs;
        private CryptKey                         _apky;
        private CryptBuffer                      _data;

        public UserPasswordsService( CTX ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService<CTX>,CTX> usr, CryptKey api )
            : base(ctx)
        {
            _usrs = usr.serve();
            _enty = UserPasswords.Invalid;
            _lazy = new Task<UserPasswords>(() => { return _enty; });
            _apky = api;
            _data = new CryptBuffer(512);
        }


        public override bool Ok
        {
            get { return Entity.IsValid() && Status.Code == ResultCode.NoState; }
            protected set { if ( value ) Status = _enty.IsValid() ? Status.NoState : _enty.Is().Status;
                else if (Status.Code == ResultCode.NoState) 
                    Status = PasswordServiceError;
            }
        }

        public async Task<UserPasswordsService<CTX>> LookupPasswordByUserAccount( int byUserId )
        {
            PasswordUsers user = (await _usrs.GetUserById(byUserId)).Entity;
            if( !user.IsValid() ) {
                _enty = user.Is().Status + ResultCode.Password;
                Status = _enty.Is().Status;
                return this;
            } else if( _enty.User != user.Id ) {
                Status = Status.NoState;
                _enty = Status.Unknown;
                _lazy = _dset.SingleOrDefaultAsync(p => p.User == user.Id);
            } return this;
        }

        public async Task<UserPasswordsService<CTX>> LookupPasswordByUserAccount( Task<PasswordUsersService<CTX>> byUser )
        {
            PasswordUsers user = byUser.IsCompleted ? byUser.Result.Entity : (await byUser).Entity;
            if( !user.IsValid() ) {
                _enty = user.Is().Status + ResultCode.Password;
                Status = _enty.Is().Status;
                return this;
            } else if( _enty.User != user.Id ) {
                Status = Status.NoState;
                _enty= Status.Unknown;
                _lazy  = _dset.SingleOrDefaultAsync(p => p.User == user.Id);
            } return this;
        }

        public Status DecryptParameter( string data )
        {
            bool nounce = false;
            if ( nounce = Entity.IsValid() ) {
                // try decrypting queryparameter by hashvalue of current users masterkey 
                data = GetMasterKey( Entity.Id ).Decrypt( data );
            } else {
                // if there's no user in context actually, then decrypt queryparameters
                // by this Password.API servers own apikey (*all clients (Password.GUI
                // agent applications) know hashvalue of this servers apikey and
                // will use it for encrypting any calls where no user information
                // exists in context yet actually... (later clients shall be changed
                // to use their individual appkeys instead (clientkeys) by which they
                // have registered as valid client app)
                data = _apky.Decrypt( data );
            }

            if( Crypt.Error ) {
                return Status = new Status(
                    ResultCode.Cryptic | ResultCode.Invalid |
                    ResultCode.Service, $"{Crypt.Error} - ApiKey Invalid",
                    System.Array.Empty<string>()
                );
            } else {
                return Status.Success.WithData(
                    nounce ? data.Substring(3).Split(".~.")
                           : data.Split(".~.")
                );
            }
        }

        public Status GetYpsEnumerator( string yps_parameters )
        { 
            CryptBuffer cryptic = new CryptBuffer( System.Text.Encoding.Default.GetBytes( yps_parameters ) );
            CryptBuffer.OuterCrypticStringEnumerator ypser;
            bool usingAppKey = !Entity.IsValid();
            if ( usingAppKey ) {
                 ypser = cryptic.GetOuterCrypticStringEnumerator( _apky, 0 );
            } else {
                 ypser = cryptic.GetOuterCrypticStringEnumerator( GetMasterKey(Entity.Id), 3 );
            }

            if( Crypt.Error ) {
                return Status = new Status(
                    ResultCode.Cryptic | ResultCode.Invalid |
                    ResultCode.Service, $"{Crypt.Error} - ApiKey Invalid",
                    System.Array.Empty<string>()
                );
            } else {
                return Status.Success.WithData( ypser );
            }
        }

        public async Task<UserPasswordsService<CTX>> SetMasterKey( int userId, string pass )
        {

            if ( !( await LookupPasswordByUserAccount(_usrs.GetUserById(userId)) ) ) {
                if ( Status.Code.HasFlag( ResultCode.Password|ResultCode.Service ) ) {
                    Status = Status.NoState;
                    _enty = new UserPasswords();
                    _enty.Hash = Crypt.CalculateHash( pass );
                    _enty.User = userId;
                    _enty.Pass = "";
                    _enty.Id = 0;
                    _dset.Add(_enty);
                    _db.SaveChanges();
                    return this;
                } else {
                    _enty = PasswordServiceError;
                    return this;
                }
            } else {
                _enty.Hash = Crypt.CalculateHash( pass );
                Save();
                return this;
            }
        }

        public bool VerifyPassword( int ofUser, string masterPassword )
        {
            if ( LookupPasswordByUserAccount( _usrs.GetUserById( ofUser ) ).GetAwaiter().GetResult() ) {
                if( Entity.Hash != Crypt.CalculateHash( masterPassword ) ) {
                    _enty = Status = HashValue.WithData( masterPassword );
                    return false;
                } else Status = Status.NoState;
                return true;
            } return false;
        }

        public CryptKey GetMasterKey( int ofUser )
        {
            if ( Entity ) if ( Entity.User == ofUser ) 
                return CreateKey( Entity );
            if ( LookupPasswordByUserAccount(_usrs.GetUserById( ofUser ) ).GetAwaiter().GetResult() ) {
                return CreateKey( Entity );
            } else {
                Status = Entity.Is().Status;
                return Crypt.CreateKey( 0 );
            }
        }

        private CryptKey CreateKey( UserPasswords masterdata )
        {
            if( masterdata.IsValid() ) {
                Status = Status.NoState;
                return Crypt.CreateKey( masterdata.Hash );
            } else {
                Status = masterdata.Is().Status;
                return null;
            }
        }

        public async Task<UserPasswordsService<CTX>> LookupUserPasswodById( int ofUser )
        {
            if ( Entity ) if ( Entity.User == ofUser ) return this;
            Status = Status.NoState;
            Entity = Status.Unknown;
            _lazy  = _dset.SingleOrDefaultAsync( p => p.User == ofUser );
            return this;
        }

        private static void CleanupDbExport( object FileInfos )
        {
            (FileInfo export,int counter) files = ((FileInfo,int))(FileInfos as System.Runtime.CompilerServices.ITuple);
            System.Threading.Thread.Sleep( 30000 );
            try { files.export.Delete();
            } catch( System.Exception _ ) {
                if( --files.counter > 0 )
                    CleanupDbExport( files );
            }
        }

        public Status GetCrypticDbExport( string exportstamp )
        {
            string path = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            FileInfo dbfile = new FileInfo($"{path}\\DataBase\\SqLite\\db.db");
            Directory.CreateDirectory( path+"\\Exporte" );
            int tries = 3; 
            while( _db.Database.CurrentTransaction != null && --tries >= 0 )
                System.Threading.Thread.Sleep( 1000 );
            if( dbfile.Exists ) {
                if( Crypt.EncryptFile( _apky, dbfile ) > 0 ) {
                    dbfile = new FileInfo($"{path}\\DataBase\\SqLite\\db.db.yps");
                    string export = $"{path}\\Exporte\\DbCore_{exportstamp}.db.yps";
                    if( Consola.Utility.CommandLine( $"rename \"{dbfile.FullName}\" \"{export}\"" ) == 0 ) {
                        dbfile = new FileInfo( export );
                        Status = Status.Success.WithText("Db core exportet");
                        new Task( CleanupDbExport, (dbfile,5) ).Start();
                        return Status.Success.WithData( dbfile.OpenRead() );
                    }
                }
            } else {
                Status = ( Status.Cryptic + Status.Invalid ).WithText(
                    "Error when encrypting database" ).WithData( Crypt.Error );
            } return Status;
        }
    } 
}
