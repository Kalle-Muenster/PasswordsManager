using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Validations;
using Yps;

// ReSharper disable All

namespace PasswordsAPI.Services
{
    public class UserPasswordsService 
        : AbstractApiService<UserPasswords,UserPasswordsService>
        , IPasswordsApiService<UserPasswords,UserPasswordsService>
    {
        private readonly Status PasswordServiceError = new Status(ResultCode.Service|ResultCode.Password|ResultCode.IsError);
        private readonly Status InvalidId = new Status(ResultCode.Service|ResultCode.Password|ResultCode.User|ResultCode.Invalid, "Invalid User.Id: {0}");
        private readonly Status HashValue = new Status(ResultCode.Service|ResultCode.Password|ResultCode.Id|ResultCode.Invalid, "password incorrct '{0}'" );
        protected override Status GetDefaultError() { return PasswordServiceError; }

        private UserPasswords pwd = UserPasswords.Invalid;
        private Task<UserPasswords> pwdLazy;
        private PasswordUsersService usrs;

        public UserPasswordsService( PasswordsDbContext ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService> usr )
            : base(ctx)
        {
            usrs = usr.serve();
            pwdLazy = new Task<UserPasswords>(() => { return pwd; });
        }

        public override UserPasswords Entity {
            get { if ( pwd.Is().Status.Waiting ) pwd = pwdLazy.GetAwaiter().GetResult();
                if (!pwd.NoError()) Status = pwd.Is().Status;
                return pwd; }
            set { if( value.NoError() ) pwd = value;
                Status = pwd.Is().Status; }

        }

        public override bool Ok
        {
            get { return Entity.NoError() && Status.Code == ErrorCode.NoError; }
            set { if ( value ) Status = pwd.NoError() ? Status.NoError : pwd.Is().Status;
                else if (Status.Code == ErrorCode.NoError) 
                    Status = PassStatus;
            }
        }

        public async Task<UserPasswordsService> ByUserEntity( Task<PasswordUsersService> byUser )
        {
            PasswordUsers user = (await byUser).Entity;
            if( !user ) { pwd = user.Is().Status;
                return this;
            }
            if( usrs.Status ) {
                Status = usrs.Status + ErrorCode.Word;
                pwd = Status;
            } else if( pwd.User != user.Id )
            { pwdLazy = db.UserPasswords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.User == user.Id);
                pwd = Status.Unknown;
                //if (pwd.User != user.Id)
                //    pwd = new Status(ErrorCode.User | ErrorCode.Word | ErrorCode.Invalid, "master key invalid" );
            } Status = pwd.Is().Status;
            return this;
        }

        public async Task<UserPasswordsService> SetMasterKey( int userId, string pass )
        {
            if ( ByUserEntity( usrs.ById( userId ) ).GetAwaiter().GetResult().Entity.Is().Status.Bad ) {
                if ( Status.Code.HasFlag( ErrorCode.Word ) ) {
                    Status = Status.NoError;
                    pwd = new UserPasswords();
                    pwd.Hash = Crypt.CalculateHash(pass);
                    BigInteger big = new BigInteger(pwd.Hash);
                    pwd.User = userId;
                    pwd.Pass = "";
                    pwd.Id = 0;
                    db.UserPasswords.AddAsync(pwd);
                    db.SaveChangesAsync();
                    return this;
                } else {
                    pwd = PassStatus;
                    return this;
                }
            } else {
                pwd.Hash = Crypt.CalculateHash(pass);
                db.UserPasswords.Update( pwd );
                db.SaveChangesAsync();
                return this;
            }

            Status = PassStatus.WithText("Unkown Error") + ErrorCode.Unknown;
            return this;
        }

        public bool VerifyPassword( int forUser, string masterPassword )
        {
            if ( ByUserEntity( usrs.ById( forUser ) ).GetAwaiter().GetResult().Entity.Is().Status.Ok ) {
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
            if ( ByUserEntity( usrs.ById( ofUser ) ).GetAwaiter().GetResult().Ok ) return Crypt.CreateKey( pwd.Hash );
            return null;
        }

        public async Task<UserPasswordsService> ByUserId( int ofUser )
        {
            if (pwd.Is().Status.Ok) if (pwd.User == ofUser) return this;
            pwdLazy = db.UserPasswords.AsNoTracking().SingleOrDefaultAsync(p => p.User == ofUser);
            pwd = Status.Unknown;
            //if (pwdLazy.Status.Bad) Status = pwd.Is().Status;
            return this;
        }
    } 
}
