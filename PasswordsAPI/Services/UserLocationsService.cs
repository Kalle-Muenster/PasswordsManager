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
        private static readonly Error LocationError = new Error( ErrorCode.Area|ErrorCode.Service|ErrorCode.Invalid,"Invalid Location");
        protected override Error GetDefaultError() { return LocationError; }

        private Crypt.Key? _key = null;
        private UserPasswordsService  pwds;
        private UserLocations loc = UserLocations.Invalid;

        public override UserLocations Entity {
            get { return Ok ? loc : Error; }
            set { if ( value ) loc = value;
                else Error = value.Is().Error;
            }
        }

        public UserLocationsService( PasswordsDbContext ctx, IPasswordsApiService<UserPasswords,UserPasswordsService> pwd ) : base(ctx)
        { pwds = pwd.serve(); }

        public UserLocationsService SetKey( string masterPass )
        {
            if( loc.Is().Error ) return this; 
            if( pwds.VerifyPassword( loc.User, masterPass ) ) _key = pwds.GetMasterKey( loc.User );
            return pwds.Error 
                 ? OnError( pwds )
                 : this;
        }

        public UserLocationsService SetKey( Crypt.Key masterKey )
        {
            if( !masterKey.IsValid() ) {
                Error = (LocationError + ErrorCode.Cryptic).WithText( "Invalid Crypt.Key" );
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

        public int GetAreaId( string nameOrId, int usrId )
        {
            if ( int.TryParse( nameOrId, out int locId ) ) {
                if ( loc.IsValid() )
                    if (loc.User == usrId && loc.Id == locId)
                        return locId;
                loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.User == usrId && l.Id == locId) 
                      ?? LocationError;
            } else {
                if ( loc.IsValid() )
                    if ( loc.User == usrId && loc.Area == nameOrId )
                        return loc.Id;
                loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.User == usrId && l.Area == nameOrId ) 
                    ?? LocationError;
            }

            if (loc.IsValid()) Ok = true;
            else Error = loc.Is().Error;
            if ( Error ) return -1;
            return loc.Id;
        }

        public UserLocationsService FromUserByNameOrId( int userId, string area )
        {
            if ( userId > 0 ) {
                GetAreaId( area, userId );
            } else {
                loc = Error = ( LocationError.WithText( "Invalid User.Id" ).WithData( userId ) + ErrorCode.User );
            } return this;
        }

        public UserLocationsService ById( int locId )
        {
            if ( loc ) if ( loc.Id == locId ) return this;
            loc = db.UserLocations.AsNoTracking().SingleOrDefault(l => l.Id == locId ) 
                ?? LocationError.WithData( locId );
            if ( loc.Is().Error ) Error = loc.Is().Error;
            return this;
        }

        public UserLocationsService AddNewLocationEntry( UserLocations init, string passToStore, Crypt.Key keyToUse )
        {
            if ( Error ) return this;
            if ( !keyToUse.IsValid() ) {
                loc= Error = new Error( LocationError.Code | ErrorCode.Cryptic | ErrorCode.Word, "Invalid Master Key" );
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
                loc = Error = new Error( LocationError.Code|Error.Cryptic.Code, Crypt.Error.ToString(), passToStore );
                return this;
            } db.UserLocations.Add( loc );
            db.SaveChanges();
            return this;
        }

        public UserLocationsService SetLocationPassword( PasswordUsers usr, UserLocations init, string pass )
        {
            if( usr.Is().Error ) {
                Error = new Error( ( ErrorCode.Service | ErrorCode.Area | ErrorCode.User ), "Unknown User" );
                return this;
            }

            if ( pwds.ByUserEntity( usr ).Error ) return OnError( pwds );
            Crypt.Key encryptionKey = pwds.Entity.GetUserKey();
            if ( FromUserByNameOrId( init.User = usr.Id, init.Area ).Ok ) {
                loc.Pass = Encoding.ASCII.GetBytes( encryptionKey.Encrypt( pass ) );
                db.UserLocations.Update( loc );
                db.SaveChanges();
                return this;
            } else loc = Error = Error.NoError;
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
            if ( Error || loc.Is().Error ) return Error.ToString();
            StringBuilder str = new StringBuilder("{ Id:");
            str.Append( loc.Id ).Append( ", User:" ).Append( loc.User ).Append( ", Name:'" ).Append( loc.Area );
            if ( loc.Info != null ) str.Append( "', Info:'" ).Append( loc.Info );
            if ( loc.Name != null ) str.Append( "', LoginName:'" ).Append( loc.Name );
            str.Append( "', Password:'" ).Append( GetPassword() ).Append( "' }" );
            return str.ToString();
        }
    }
}
