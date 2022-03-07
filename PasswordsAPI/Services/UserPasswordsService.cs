using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Validations;
using Yps;

// ReSharper disable All

namespace PasswordsAPI.Services
{
    public class UserPasswordsService 
        : AbstractApiService<UserPasswords,UserPasswordsService>
        , IPasswordsApiService<UserPasswords,UserPasswordsService>
    {
        private readonly Error PassError = new Error(ErrorCode.Service|ErrorCode.Word);
        private readonly Error InvalidId = new Error(ErrorCode.Service|ErrorCode.Word|ErrorCode.User, "Invalid User.Id: {0}");
        private readonly Error HashValue = new Error(ErrorCode.Service|ErrorCode.Word|ErrorCode.Id, "Missing Password Hash" );
        protected override Error GetDefaultError() { return PassError; }

        private UserPasswords pwd = UserPasswords.Invalid;
        private PasswordUsersService usrs;

        public UserPasswordsService( PasswordsDbContext ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService> usr )
            : base(ctx)
        {
            usrs = usr.serve();
        }

        public override UserPasswords Entity {
            get { if (!pwd.IsValid()) Error = pwd.Is().Error; return pwd; }
            set { if(value.IsValid()) pwd = value; Error = Error.NoError; }
        }

        public override bool Ok
        {
            get { return pwd.IsValid() && Error.Code == ErrorCode.NoError; }
            set { if ( value ) Error = pwd.IsValid() ? Error.NoError : pwd.Is().Error;
                else if (Error.Code == ErrorCode.NoError) 
                    Error = PassError;
            }
        }

        public UserPasswordsService ByUserEntity( PasswordUsers byUser )
        {
            if( !byUser ) { pwd = byUser.Is().Error;
                return this;
            }
            if( usrs.Error ) {
                Error = usrs.Error + ErrorCode.Word;
                pwd = Error;
            } else if( pwd.User != byUser.Id ) {
                pwd = db.UserPasswords
                    .AsNoTracking()
                    .SingleOrDefault( p => p.User == byUser.Id )
               ?? PassError;
            } if ( !pwd.IsValid() )
                Error = pwd.Is().Error;
            return this;
        }

        public UserPasswordsService SetMasterKey( int user, string pass )
        {
            if ( usrs.ById( user ).Error ) return OnError( usrs );
            if ( ByUserEntity( usrs.Entity ).Error )
            if ( Error.Code.HasFlag( ErrorCode.Word ) ) {
                Error = Error.NoError;
                pwd = new UserPasswords();
                pwd.Hash = Crypt.CalculateHash( pass );
                pwd.User = user;
                pwd.Pass = "";
                pwd = db.UserPasswords.Add( pwd ).Entity;
                db.SaveChanges();
            } else {
                pwd = PassError;
                return this;
            }
            pwd.Hash = Crypt.CalculateHash( pass );
            db.UserPasswords.Update( pwd );
            db.SaveChanges();
            return this;
        }

        public bool VerifyPassword( int forUser, string masterPassword )
        {
            if ( ByUserEntity( usrs.ById( forUser ).Entity ).Ok ) {
                if( pwd.Hash != Crypt.CalculateHash( masterPassword ) ) {
                    pwd = Error = HashValue.WithData( masterPassword );
                    return false;
                } else Error = Error.NoError;
                return true;
            } return false;
        }

        public Crypt.Key GetMasterKey( int ofUser )
        {
            if ( Entity.IsValid() ) if ( Entity.User == ofUser ) return Crypt.CreateKey( pwd.Hash );
            if ( ByUserEntity( usrs.ById( ofUser ).Entity ).Ok ) return Crypt.CreateKey( pwd.Hash );
            return null;
        }
    } 
}
