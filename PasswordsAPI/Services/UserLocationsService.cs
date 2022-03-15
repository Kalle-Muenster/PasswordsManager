
using System;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yps;

namespace PasswordsAPI.Services
{
    public class UserLocationsService : AbstractApiService<UserLocations,UserLocationsService>, IPasswordsApiService<UserLocations,UserLocationsService>
    {
        private static readonly Status LocationServiceError = new Status( ResultCode.Area|ResultCode.Service|ResultCode.Invalid,"Invalid Location");
        protected override Status GetDefaultError() { return LocationServiceError; }

        private Crypt.Key? _key = null;
        private UserPasswordsService  _keys;


        public override UserLocations Entity {
            get {
                if (_enty.Is().Status.Waiting)
                    _enty = _lazy.GetAwaiter().GetResult() 
                        ?? LocationServiceError;
                return Ok ? _enty : Status;
            }
            set {
                if ( value ) { _enty = value;
                    Status = _enty.Is().Status; }
                else Status = value.Is().Status;
            }
        }

        public UserLocationsService( PasswordsDbContext ctx, IPasswordsApiService<UserPasswords,UserPasswordsService> pwd ) : base( ctx )
        {
            _keys = pwd.serve();
            _enty = UserLocations.Invalid;
            _lazy = new Task<UserLocations>(() => { return _enty; });
        }

        public async Task<UserLocationsService> SetKey( string masterPass )
        {
            if( Entity.Is().Status.Bad ) return this; 
            if( _keys.VerifyPassword( Entity.User, masterPass ) ) _key = _keys.GetMasterKey( Entity.User );
            return _keys.Status 
                 ? OnError( _keys )
                 : this;
        }

        public async Task<UserLocationsService> SetKey( Crypt.Key masterKey )
        {
            if( !masterKey.IsValid() ) {
                Status = (LocationServiceError + ResultCode.Cryptic).WithText( "Invalid Crypt.Key" );
                _key = null;
            } else {
                Ok = true;
                _key = masterKey;
            } return this;
        }

        public string GetPassword()
        {
            string cryptic = Encoding.Default.GetString( Entity?.Pass ?? new byte[]{} );
            return _key?.Decrypt( cryptic ) ?? cryptic;
        }

        public string GetPassword( string masterPass )
        {
            string crypt = SetKey( Crypt.CreateKey( masterPass ) ).GetAwaiter().GetResult().GetPassword();
            if ( Crypt.Error ) {
                Status = new Status( Crypt.Error.Code.ToError(), Crypt.Error.Text, "still encrypted: " + crypt );
            } return crypt;
        }

        public async Task<UserLocationsService> SetPassword( string userMasterPass, string newLocationPass )
        {
            if ( Entity.Id > 0 ) {
                if ( await _keys.ByUserId( Entity.User ).ConfigureAwait(false) ) {
                    if ( _keys.VerifyPassword( Entity.User, userMasterPass ) ) {
                        Crypt.Key key = _keys.GetMasterKey( Entity.User );
                        Entity.Pass = Encoding.ASCII.GetBytes(key.Encrypt(newLocationPass));
                        _db.Update(Entity);
                        _db.SaveChangesAsync();
                    } else Status = _keys.Status;
                } return this;
            } else {
                Status = LocationServiceError.WithText("unknown user id");
                return this;
            }
        }

        public int GetAreaId( string nameOrId, int usrId )
        {
            if ( int.TryParse( nameOrId, out int locId ) ) {
                if ( Entity.IsValid() )
                    if ( Entity.User == usrId && Entity.Id == locId )
                        return locId;
                Status = Status.NoError;
                _enty = Status.Unknown;
                _lazy = _db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Id == locId);
            } else {
                if ( Entity.IsValid() )
                    if ( Entity.User == usrId && Entity.Area == nameOrId )
                        return Entity.Id;
                Status = Status.NoError;
                _enty = Status.Unknown;
                _lazy = _db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Area == nameOrId);
            }
            if ( Entity ) return Entity.Id;
            else return -1;
        }

        public async Task<UserLocationsService> FromUserByNameOrId( int userId, string area )
        {
            if ( userId > 0 ) {
                if ( GetAreaId(area, userId) == -1 ) Status = LocationServiceError.WithText("location invalid");
            } else {
                _enty = Status = ( LocationServiceError.WithText( "user invalid" ).WithData( userId ) + ResultCode.User );
            } return this;
        }

        public async Task<UserLocationsService> ById( int locId )
        {
            if ( _enty ) if ( _enty.Id == locId ) return this;
            _lazy = _db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.Id == locId);
            _enty= Status.Unknown;
            Status = Status.NoError;
            return this;
        }

        public UserLocationsService AddNewLocationEntry( UserLocations init, string passToStore, Crypt.Key keyToUse )
        {
            if ( Status.Bad ) return this;
            if ( !keyToUse.IsValid() ) {
                _enty= Status = new Status( LocationServiceError.Code | ResultCode.Cryptic | ResultCode.Password, "Invalid Master Key" );
                return this;
            } _enty = init;

            _enty.Pass = Encoding.ASCII.GetBytes( keyToUse.Encrypt( passToStore ) );
            if( Crypt.Error ) {
                _enty = Status = new Status( LocationServiceError.Code|Status.Cryptic.Code, Crypt.Error.ToString(), passToStore );
                return this;
            } _db.UserLocations.AddAsync( _enty );
            _db.SaveChangesAsync();
            return this;
        }

        public async Task<UserLocationsService> SetLocationPassword( Task<PasswordUsersService> usrserv, UserLocations init, string pass )
        {
            PasswordUsers usr = (await usrserv).Entity;
            if( usr.Is().Status.Bad ) {
                Status = new Status( (
                    ResultCode.Service | ResultCode.Area 
                  | ResultCode.User ),"Unknown User"
                );
                return this;
            }

            if ( !(await _keys.ByUserEntity( usrserv )) ) return OnError( _keys );
            UserPasswords pwd = _keys.Entity;
            Crypt.Key encryptionKey = pwd.GetUserKey();
            if ( await FromUserByNameOrId( init.User = usr.Id, init.Area ) ) {
                _enty.Pass = Encoding.ASCII.GetBytes( encryptionKey.Encrypt( pass ) );
                _db.UserLocations.Update( _enty );
                _db.SaveChangesAsync();
                return this;
            } else _enty = Status = Status.NoError;
            return AddNewLocationEntry( init, pass, encryptionKey );
        }

        public async Task<UserLocationsService> SetLoginInfo( int locId, string? login, string? info )
        {
            if( (await ById( locId )).Entity.Is().Status.Ok ) {
                if (info != null) if (info != String.Empty) _enty.Info = info;
                if (login != null) if (login != String.Empty) _enty.Name = login;
                _db.UserLocations.Update(_enty);
                _db.SaveChangesAsync();
            } return this;
        }

        public override string ToString()
        {
            if ( Status.Bad || Entity.Is().Status.Bad ) return Status.ToString();
            StringBuilder str = new StringBuilder("{ Id:");
            str.Append( _enty.Id ).Append( ", User:" ).Append( _enty.User ).Append( ", Name:'" ).Append( _enty.Area );
            if ( _enty.Info != null ) str.Append( "', Info:'" ).Append( _enty.Info );
            if ( _enty.Name != null ) str.Append( "', LoginName:'" ).Append( _enty.Name );
            str.Append( "', Password:'" ).Append( GetPassword() ).Append( "' }" );
            return str.ToString();
        }

        public async Task<UserLocationsService> RemoveLocation( Task<PasswordUsersService> userserv, string area, string master )
        {
            PasswordUsers user = (await userserv).Entity;
            UserPasswords password = (await _keys.ByUserEntity( userserv ).ConfigureAwait(false) ).Entity;
            if ( password.Is().Status.Ok ) {
                if( _keys.VerifyPassword( user.Id, master ) ) {
                    if ( (await FromUserByNameOrId( user.Id, area )).Entity.Is().Status.Ok ) {
                        _db.UserLocations.Remove( Entity );
                        _db.SaveChanges();
                    } else Status = LocationServiceError.WithText( "No password for {0}" ).WithData( area );
                } else Status = LocationServiceError.WithData( master ).WithText( 
                  "For Deleting a Passwords, correct master key is needed"
                ) + ( ResultCode.Invalid | ResultCode.User | ResultCode.Password );
            } return this;
        }
    }
}
