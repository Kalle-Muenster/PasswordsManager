using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using Yps;

namespace Passwords.API.Services
{
    public class UserLocationsService<CTX>
        : AbstractApiService< UserLocations, UserLocationsService<CTX>, CTX >
    where CTX
        : PasswordsApiDbContext<CTX>
    {
        private static readonly Status LocationServiceError = new Status(
                                    ResultCode.Area | ResultCode.Service |
                                    ResultCode.Invalid, "Invalid Location" );

        protected override Status GetDefaultError() {
            return LocationServiceError;
        }

        protected override ResultCode GetServiceFlags() {
            return ResultCode.Area;
        }

        protected override UserLocations GetStatusEntity(Status cast) {
            return cast;
        }

        private CryptKey?                  _key = null;
        private UserPasswordsService<CTX>  _keys;


        public bool Update()
        {
            if( Entity.Is().Status.Ok ) {
                _db.Update( Entity );
                _db.SaveChanges();
                return true;
            } return false;
        }

        public List<UserLocations> GetUserLocations(int user)
        {
            Func<UserLocations, UserLocations> selector = (UserLocations u) => { return u.User == user ? u : null; };
            IEnumerator<UserLocations> locations = _dset.AsNoTracking().Select(selector).GetEnumerator();
            List<UserLocations> returnList = new List<UserLocations>();
            while ( locations.MoveNext() ) {
                if ( locations.Current != null ) {
                    returnList.Add( locations.Current );
                }
            } locations.Dispose();
            return returnList;
        }

        public UserLocationsService( CTX ctx, IPasswordsApiService<UserPasswords,UserPasswordsService<CTX>,CTX> pwd )
            : base( ctx )
        {
            _keys = pwd.serve();
            _enty = UserLocations.Invalid;
            _lazy = new Task<UserLocations>(() => { return _enty; });
        }

        public async Task<UserLocationsService<CTX>> SetKey( string masterPass )
        {
            if( Entity.Is().Status.Bad ) return this; 
            if( _keys.VerifyPassword( Entity.User, masterPass ) ) _key = _keys.GetMasterKey( Entity.User );
            return _keys.Status 
                 ? OnError( _keys )
                 : this;
        }

        public async Task<UserLocationsService<CTX>> SetKey( Task<UserPasswordsService<CTX>> keyService )
        {
            UserPasswords keyserved = (await keyService).Entity;
            CryptKey set = (await keyService).GetMasterKey( keyserved.User );
            if( !set.IsValid() ) {
                Status = (LocationServiceError + ResultCode.Cryptic).WithText( "CryptKey structure invalid" );
                _key = null;
            } else {
                Ok = true;
                _key = set;
            } return this;
        }

        public string GetPassword()
        {
            string cryptic = Encoding.Default.GetString( Entity?.Pass ?? new byte[]{} );
            return _key?.Decrypt( cryptic ) ?? cryptic;
        }

        public string GetPassword( string masterPass )
        {
            string crypt = SetKey( masterPass ).GetAwaiter().GetResult().GetPassword();
            if ( Crypt.Error ) {
                Status = new Status( ResultCode.Unknown|ResultCode.Cryptic, Crypt.Error.ToString(), "still encrypted: " + crypt );
            } else {
                Status = Status.Success;
            } return crypt;
        }

        public async Task<UserLocationsService<CTX>> SetPassword( string userMasterPass, string newLocationPass )
        {
            if ( Entity.Id > 0 ) {
                if ( (await _keys.LookupUserPasswodById( Entity.User )).VerifyPassword( Entity.User, userMasterPass ) ) {
                    CryptKey key = _keys.GetMasterKey( Entity.User );
                    Entity.Pass = Encoding.ASCII.GetBytes( key.Encrypt( newLocationPass ) );
                    _db.Update( Entity );
                    _db.SaveChanges();
                } else Status = _keys.Status;
                return this;
            } else {
                Status = LocationServiceError.WithText( "unknown user id" );
                return this;
            }
        }

        public int GetAreaId( string nameOrId, int usrId )
        {
            Status = Status.NoState.WithData(nameOrId);
            if ( int.TryParse( nameOrId, out int locId ) ) {
                if ( Entity.IsValid() )
                    if ( Entity.User == usrId && Entity.Id == locId )
                        return locId;
                _enty = Status.Unknown;
                _lazy = _dset.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Id == locId);
            } else {
                if ( Entity.IsValid() )
                    if ( Entity.User == usrId && Entity.Area == nameOrId )
                        return Entity.Id;
                _enty = Status.Unknown;
                _lazy = _dset.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Area == nameOrId);
            }
            if ( Entity ) return Entity.Id;
            else return -1;
        }

        public UserLocations GetLocationOfUser( int userId, string location )
        {
            if ( userId > 0 ) {
                if ( GetAreaId( location, userId ) == -1 ) Status = LocationServiceError.WithText( $"invalid location '{location}'" );
            } else {
                _enty = Status = LocationServiceError.WithText( $"invalid user '{userId}'" ) + ResultCode.User;
            } return Entity;
        }

        public async Task<UserLocationsService<CTX>> GetLocationById( int locationId )
        {
            if ( locationId > 0 ) {
                if (Entity) if (Entity.Id == locationId) return this;
                Status = Status.NoState;
                _lazy = _dset.AsNoTracking().SingleOrDefaultAsync(l => l.Id == locationId);
                _enty = Status.Unknown;
            } else _enty= Status = LocationServiceError.WithText( $"invalid location id '{locationId}'" );
            return this;
        }

        public UserLocationsService<CTX> AddNewLocationEntry( UserLocations init, string passToStore, CryptKey masterKey )
        {
            if (Status) return this;
            if ( !masterKey.IsValid() ) {
                _enty= Status = new Status( LocationServiceError.Code | ResultCode.Cryptic | ResultCode.Password, "Invalid Master Key" );
                return this;
            } _enty = init;

            _enty.Pass = Encoding.ASCII.GetBytes( masterKey.Encrypt( passToStore ) );
            if( Crypt.Error ) {
                _enty = Status = new Status( LocationServiceError.Code|Status.Cryptic.Code, Crypt.Error.ToString(), passToStore );
                return this;
            }

            _dset.AddAsync( _enty );
            _db.SaveChangesAsync();
            Status = Status.Success.WithText( "new password stored for" ).WithData( init.Area );
            return this;
        }

        public async Task<UserLocationsService<CTX>> SetLocationPassword( Task<PasswordUsersService<CTX>> usrserv, UserLocations init, string pass )
        {
            PasswordUsers usr = (await usrserv).Entity;
            if( usr.Is().Status.Bad ) {
                Status = new Status( (
                        ResultCode.Area | ResultCode.Service |
                        ResultCode.User | ResultCode.Invalid )
                    ,"Unknown User"
                );
                return this;
            }

            if ( ! await _keys.LookupPasswordByUserAccount( usrserv ) )
                return OnError( _keys );

            CryptKey masterKey = _keys.GetMasterKey( usr.Id );
            if ( GetLocationOfUser( init.User = usr.Id, init.Area ) ) {
                // if the location already exists, update with new password set
                _enty.Pass = Encoding.ASCII.GetBytes( masterKey.Encrypt( pass ) );
                _dset.Update( _enty );
                Status = Status.NoState;
                _db.SaveChanges();
                return this;
            } else {
                // if location not exists yet, add a new location entry therefore
                _enty = Status = Status.NoState;
                return AddNewLocationEntry( init, pass, masterKey );
            }
        }

        public async Task<UserLocationsService<CTX>> SetLoginInfo( int locId, string? login, string? info )
        {
            if( (await GetLocationById( locId )).Entity.IsValid() ) {
                if (info != null) if (info != String.Empty) _enty.Info = info;
                if (login != null) if (login != String.Empty) _enty.Name = login;
                _dset.Update( _enty );
                _db.SaveChanges();
                Status = Status.Success + GetServiceFlags();
            } return this;
        }

        public override string ToString()
        {
            if ( Status.Bad || Entity.Is().Status.Bad ) return Status.ToString();
            StringBuilder str = new StringBuilder("{ \"Id\":");
            str.Append( _enty.Id ).Append( ", \"User\":" ).Append( _enty.User ).Append( ", \"Name\":\"" ).Append( _enty.Area );
            if ( _enty.Info != null ) str.Append( "\", \"Info\":\"" ).Append( _enty.Info );
            if ( _enty.Name != null ) str.Append( "\", \"LoginName\":\"" ).Append( _enty.Name );
            str.Append( "\", \"Password\":\"" ).Append( GetPassword() ).Append( "\" }" );
            return str.ToString();
        }

        public async Task<UserLocationsService<CTX>> RemoveLocation( int userId, string area, string pass )
        {
            UserPasswords password = ( await _keys.LookupPasswordByUserAccount( userId ) ).Entity;
            if ( password.IsValid() ) {
                if( _keys.VerifyPassword( userId, pass ) ) {
                    if ( GetLocationOfUser( userId, area ).IsValid() ) {
                        _dset.Remove( Entity );
                        _db.SaveChanges();
                        Status = Status.Success.WithData( area ).WithText(
                            "Successfully removed location '{0}' of user " + userId
                        ) + GetServiceFlags();
                    } else {
                        Status = LocationServiceError.WithText( "Location '{0}' not exists" ).WithData( area );
                    }
                } else { 
                    Status = LocationServiceError.WithData( pass ).WithText( 
                        "For Deleting a Passwords, the owning users masterkey is needed"
                    ) + ( ResultCode.Invalid | ResultCode.User | ResultCode.Password );
                }
            } return this;
        }
    }
}
