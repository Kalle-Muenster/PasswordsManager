
using System;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Yps;

namespace PasswordsAPI.Services
{
    public class UserLocationsService : AbstractApiService<UserLocations,UserLocationsService>, IPasswordsApiService<UserLocations,UserLocationsService>
    {
        private static readonly Status LocationStatus = new Status( ErrorCode.Area|ErrorCode.Service|ErrorCode.Invalid,"Invalid Location");
        protected override Status GetDefaultError() { return LocationStatus; }

        private Crypt.Key? _key = null;
        private UserPasswordsService  pwds;
        private UserLocations loc = UserLocations.Invalid;

        public override UserLocations Entity {
            get { return Ok ? loc : Status; }
            set { if ( value ) loc = value;
                else Status = value.Is().Status;
            }
        }

        public UserLocationsService( PasswordsDbContext ctx, IPasswordsApiService<UserPasswords,UserPasswordsService> pwd ) : base(ctx)
        { pwds = pwd.serve(); }

        public UserLocationsService SetKey( string masterPass )
        {
            if( loc.Is().Status.Bad ) return this; 
            if( pwds.VerifyPassword( loc.User, masterPass ) ) _key = pwds.GetMasterKey( loc.User );
            return pwds.Status 
                 ? OnError( pwds )
                 : this;
        }

        public UserLocationsService SetKey( Crypt.Key masterKey )
        {
            if( !masterKey.IsValid() ) {
                Status = (LocationStatus + ErrorCode.Cryptic).WithText( "Invalid Crypt.Key" );
                _key = null;
            } else {
                _key = masterKey;
            } return this;
        }

        public string GetPassword()
        {
            string cryptic = Encoding.Default.GetString( loc?.Pass ?? new byte[]{} );
            return _key?.Decrypt( cryptic ) ?? cryptic;
        }

        public string GetPassword( string masterpass )
        {
            string crypt = SetKey( Crypt.CreateKey( masterpass ) ).GetPassword();
            if ( Crypt.Error ) {
                Status = new Status( Crypt.Error.Code.ToError(), Crypt.Error.Text, "still encrypted: " + crypt );
            } return crypt;
        }

        public int GetAreaId( string nameOrId, int usrId )
        {
            if ( int.TryParse( nameOrId, out int locId ) ) {
                if ( loc.NoError() )
                    if (loc.User == usrId && loc.Id == locId)
                        return locId;
                loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.User == usrId && l.Id == locId) 
                      ?? LocationStatus;
            } else {
                if ( loc.NoError() )
                    if ( loc.User == usrId && loc.Area == nameOrId )
                        return loc.Id;
                loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.User == usrId && l.Area == nameOrId ) 
                    ?? LocationStatus;
            }

            if ( loc.NoError() ) Ok = true;
            else Status = loc.Is().Status;
            if ( Status ) return -1;
            return loc.Id;
        }

        public UserLocationsService FromUserByNameOrId( int userId, string area )
        {
            if ( userId > 0 ) {
                GetAreaId( area, userId );
            } else {
                loc = Status = ( LocationStatus.WithText( "Invalid User.Id" ).WithData( userId ) + ErrorCode.User );
            } return this;
        }

        public UserLocationsService ById( int locId )
        {
            if ( loc ) if ( loc.Id == locId ) return this;
            loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.Id == locId ) 
                ?? LocationStatus.WithData( locId );
            if ( loc.Is().Status ) Status = loc.Is().Status;
            return this;
        }

        public UserLocationsService AddNewLocationEntry( UserLocations init, string passToStore, Crypt.Key keyToUse )
        {
            if ( Status ) return this;
            if ( !keyToUse.IsValid() ) {
                loc= Status = new Status( LocationStatus.Code | ErrorCode.Cryptic | ErrorCode.Word, "Invalid Master Key" );
                return this;
            } loc = init;

            //loc.Pass = new byte[(newPass.Length * 4 / 3) + 16];
            //CrypsData cry = new CrypsData(loc.Pass);
            //OuterCrypticEnumerator it = cry.GetOuterCrypticIterator(crypt);
            //CryptFrame cn = new CryptFrame();
            //cry.ByteIndex = it.GetHeader().GetDataSize();
            //it.GetHeader().GetCopy<byte>().CopyTo(loc.Pass,0);
            //while (it.MoveNext()) {
            //    cn[0] = loc.Pass[cry.ByteIndex++];
            //    cn[1] = loc.Pass[cry.ByteIndex++];
            //    cn[2] = loc.Pass[cry.ByteIndex++];
            //    it.Current = cn.bin;
            //} it.Dispose();
            //cry.Dispose();

            loc.Pass = Encoding.ASCII.GetBytes( keyToUse.Encrypt( passToStore ) );
            if( Crypt.Error ) {
                loc = Status = new Status( LocationStatus.Code|Status.Cryptic.Code, Crypt.Error.ToString(), passToStore );
                return this;
            } db.UserLocations.Add( loc );
            db.SaveChanges();
            return this;
        }

        public UserLocationsService SetLocationPassword( PasswordUsers usr, UserLocations init, string pass )
        {
            if( usr.Is().Status ) {
                Status = new Status( (
                    ErrorCode.Service | ErrorCode.Area 
                  | ErrorCode.User ),"Unknown User"
                );
                return this;
            }

            if ( pwds.ByUserEntity( usr ).Status ) return OnError( pwds );
            Crypt.Key encryptionKey = pwds.Entity.GetUserKey();
            if ( FromUserByNameOrId( init.User = usr.Id, init.Area ).Ok ) {
                loc.Pass = Encoding.ASCII.GetBytes( encryptionKey.Encrypt( pass ) );
                db.UserLocations.Update( loc );
                db.SaveChanges();
                return this;
            } else loc = Status = Status.NoError;
            return AddNewLocationEntry( init, pass, encryptionKey );
        }

        public UserLocationsService SetLoginInfo( int locId, string? login, string? info )
        {
            if( ById( locId ).Ok ) {
                if (info != null) if (info != String.Empty) loc.Info = info;
                if (login != null) if (login != String.Empty) loc.Name = login;
                db.UserLocations.Update(loc);
                db.SaveChanges();
            } return this;
        }

        public override string ToString()
        {
            if ( Status || loc.Is().Status ) return Status.ToString();
            StringBuilder str = new StringBuilder("{ Id:");
            str.Append( loc.Id ).Append( ", User:" ).Append( loc.User ).Append( ", Name:'" ).Append( loc.Area );
            if ( loc.Info != null ) str.Append( "', Info:'" ).Append( loc.Info );
            if ( loc.Name != null ) str.Append( "', LoginName:'" ).Append( loc.Name );
            str.Append( "', Password:'" ).Append( GetPassword() ).Append( "' }" );
            return str.ToString();
        }

        public UserLocationsService RemoveLocation( PasswordUsers user, string area, string master )
        {
            if( pwds.ByUserEntity( user ).Ok ) {
                if( pwds.VerifyPassword( user.Id, master ) ) {
                    if ( FromUserByNameOrId( user.Id, area ).Ok ) {
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
