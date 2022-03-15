
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
        private UserPasswordsService  pwds;
        private UserLocations loc = UserLocations.Invalid;
        private Task<UserLocations> locLazy;

        public override UserLocations Entity {
            get {
                if (loc.Is().Status.Waiting) loc = locLazy.GetAwaiter().GetResult();
                return Ok ? loc : Status;
            }
            set {
                if ( value ) loc = value;
                else Status = value.Is().Status;
            }
        }

        public UserLocationsService( PasswordsDbContext ctx, IPasswordsApiService<UserPasswords,UserPasswordsService> pwd ) : base(ctx)
        { 
            pwds = pwd.serve();
            locLazy = new Task<UserLocations>(() => { return loc; });
        }

        public async Task<UserLocationsService> SetKey( string masterPass )
        {
            if( Entity.Is().Status.Bad ) return this; 
            if( pwds.VerifyPassword( Entity.User, masterPass ) ) _key = pwds.GetMasterKey( Entity.User );
            return pwds.Status 
                 ? OnError( pwds )
                 : this;
        }

        public async Task<UserLocationsService> SetKey( Crypt.Key masterKey )
        {
            if( !masterKey.IsValid() ) {
                Status = (LocationServiceError + ResultCode.Cryptic).WithText( "Invalid Crypt.Key" );
                _key = null;
            } else {
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
                if ( await pwds.ByUserId( Entity.User ) ) {
                    if ( pwds.VerifyPassword( Entity.User, userMasterPass ) ) {
                        Crypt.Key key = pwds.GetMasterKey( Entity.User );
                        Entity.Pass = Encoding.ASCII.GetBytes(key.Encrypt(newLocationPass));
                        db.Update(Entity);
                        db.SaveChangesAsync();
                    } else Status = pwds.Status;
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
                locLazy = db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Id == locId);
                loc = Status.Unknown;
                Status = Status.NoError;
            } else {
                if ( Entity.IsValid() )
                    if ( Entity.User == usrId && Entity.Area == nameOrId )
                        return Entity.Id;
                locLazy = db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.User == usrId && l.Area == nameOrId);
                loc = Status.Unknown;
                Status = Status.NoError;
            }
            if ( Status ) return -1;
            return Entity.Id;
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
            if ( loc ) if ( loc.Id == locId ) return this;
            locLazy = db.UserLocations.AsNoTracking().SingleOrDefaultAsync(l => l.Id == locId);
            loc = Status.Unknown;
            return this;
        }

        public UserLocationsService AddNewLocationEntry( UserLocations init, string passToStore, Crypt.Key keyToUse )
        {
            if ( Status.Bad ) return this;
            if ( !keyToUse.IsValid() ) {
                loc= Status = new Status( LocationStatus.Code | ErrorCode.Cryptic | ErrorCode.Word, "Invalid Master Key" );
                return this;
            } loc = init;

            loc.Pass = Encoding.ASCII.GetBytes( keyToUse.Encrypt( passToStore ) );
            if( Crypt.Error ) {
                loc = Status = new Status( LocationStatus.Code|Status.Cryptic.Code, Crypt.Error.ToString(), passToStore );
                return this;
            } db.UserLocations.AddAsync( loc );
            db.SaveChangesAsync();
            return this;
        }

        public async Task<UserLocationsService> SetLocationPassword( Task<PasswordUsersService> usrserv, UserLocations init, string pass )
        {
            PasswordUsers usr = (await usrserv).Entity;
            if( usr.Is().Status.Bad ) {
                Status = new Status( (
                    ErrorCode.Service | ErrorCode.Area 
                  | ErrorCode.User ),"Unknown User"
                );
                return this;
            }

            if ( (await pwds.ByUserEntity( usrserv ) ).Entity.Is().Status.Bad ) return OnError( pwds );

            Crypt.Key encryptionKey = pwds.Entity.GetUserKey();
            if ( await FromUserByNameOrId( init.User = usr.Id, init.Area ) ) {
                loc.Pass = Encoding.ASCII.GetBytes( encryptionKey.Encrypt( pass ) );
                db.UserLocations.Update( loc );
                db.SaveChangesAsync();
                return this;
            } else loc = Status = Status.NoError;
            return AddNewLocationEntry( init, pass, encryptionKey );
        }

        public async Task<UserLocationsService> SetLoginInfo( int locId, string? login, string? info )
        {
            if( (await ById( locId )).Entity.Is().Status.Ok ) {
                if (info != null) if (info != String.Empty) loc.Info = info;
                if (login != null) if (login != String.Empty) loc.Name = login;
                db.UserLocations.Update(loc);
                db.SaveChangesAsync();
            } return this;
        }

        public override string ToString()
        {
            if ( Status.Bad || Entity.Is().Status.Bad ) return Status.ToString();
            StringBuilder str = new StringBuilder("{ Id:");
            str.Append( loc.Id ).Append( ", User:" ).Append( loc.User ).Append( ", Name:'" ).Append( loc.Area );
            if ( loc.Info != null ) str.Append( "', Info:'" ).Append( loc.Info );
            if ( loc.Name != null ) str.Append( "', LoginName:'" ).Append( loc.Name );
            str.Append( "', Password:'" ).Append( GetPassword() ).Append( "' }" );
            return str.ToString();
        }

        public async Task<UserLocationsService> RemoveLocation( Task<PasswordUsersService> userserv, string area, string master )
        {
            PasswordUsers user = (await userserv).Entity;
            UserPasswords password = (await pwds.ByUserEntity( userserv ) ).Entity;
            if ( password.Is().Status.Ok ) {
                if( pwds.VerifyPassword( user.Id, master ) ) {
                    if ( (await FromUserByNameOrId( user.Id, area )).Entity.Is().Status.Ok ) {
                        db.UserLocations.Remove( Entity );
                        db.SaveChanges();
                    } else Status = LocationStatus.WithText( "No password for {0}" ).WithData( area );
                } else Status = LocationStatus.WithData( master ).WithText( 
                  "For Deleting a Passwords, correct master key is needed"
                ) + ( ErrorCode.Invalid | ErrorCode.User | ErrorCode.Word );
            } return this;
        }
    }
}
