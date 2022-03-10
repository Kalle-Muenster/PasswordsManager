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
        private readonly Status PassStatus = new Status(ErrorCode.Service|ErrorCode.Word);
        private readonly Status InvalidId = new Status(ErrorCode.Service|ErrorCode.Word|ErrorCode.User, "Invalid User.Id: {0}");
        private readonly Status HashValue = new Status(ErrorCode.Service|ErrorCode.Word|ErrorCode.Id, "Missing Password Hash" );
        protected override Status GetDefaultError() { return PassStatus; }

        private UserPasswords pwd = UserPasswords.Invalid;
        private PasswordUsersService usrs;

        public UserPasswordsService( PasswordsDbContext ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService> usr )
            : base(ctx)
        {
            usrs = usr.serve();
        }

        public override UserPasswords Entity {
            get { if (!pwd.NoError()) Status = pwd.Is().Status; return pwd; }
            set { if(value.NoError()) pwd = value; Status = Status.NoError; }
        }

        public override bool Ok
        {
            get { return pwd.NoError() && Status.Code == ErrorCode.NoError; }
            set { if ( value ) Status = pwd.NoError() ? Status.NoError : pwd.Is().Status;
                else if (Status.Code == ErrorCode.NoError) 
                    Status = PassStatus;
            }
        }

        public UserPasswordsService ByUserEntity( PasswordUsers byUser )
        {
            if( !byUser ) { pwd = byUser.Is().Status;
                return this;
            }
            if( usrs.Status ) {
                Status = usrs.Status + ErrorCode.Word;
                pwd = Status;
            } else if( pwd.User != byUser.Id ) {
                pwd = db.UserPasswords
                    .AsNoTracking()
                    .SingleOrDefault( p => p.User == byUser.Id )
               ?? PassStatus;
            } if ( !pwd.NoError() )
                Status = pwd.Is().Status;
            return this;
        }

        public UserPasswordsService SetMasterKey( int user, string pass )
        {
            if ( usrs.ById( user ).Status ) return OnError( usrs );
            if ( ByUserEntity( usrs.Entity ).Status )
            if ( Status.Code.HasFlag( ErrorCode.Word ) ) {
                Status = Status.NoError;
                pwd = new UserPasswords();
                pwd.Hash = Crypt.CalculateHash( pass );
                pwd.User = user;
                pwd.Pass = "";
                pwd = db.UserPasswords.Add( pwd ).Entity;
                db.SaveChanges();
            } else {
                pwd = PassStatus;
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
                    pwd = Status = HashValue.WithData( masterPassword );
                    return false;
                } else Status = Status.NoError;
                return true;
            } return false;
        }

        public Crypt.Key GetMasterKey( int ofUser )
        {
            if ( Entity.NoError() ) if ( Entity.User == ofUser ) return Crypt.CreateKey( pwd.Hash );
            if ( ByUserEntity( usrs.ById( ofUser ).Entity ).Ok ) return Crypt.CreateKey( pwd.Hash );
            return null;
        }
    } 
}
