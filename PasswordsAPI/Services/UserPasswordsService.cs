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


        private PasswordUsersService _usrs;

        public UserPasswordsService( PasswordsDbContext ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService> usr )
            : base(ctx)
        {
            _usrs = usr.serve();
            _enty = UserPasswords.Invalid;
            _lazy = new Task<UserPasswords>(() => { return _enty; });
        }

        public override UserPasswords Entity {
            get { if ( _enty.Is().Status.Waiting )
                    _enty = _lazy.GetAwaiter().GetResult() 
                             ?? PasswordServiceError;
                if (_enty.Is().Status.Bad ) Status = _enty.Is().Status;
                return _enty; }
            set { if( value.IsValid() ) _enty = value;
                Status = _enty.Is().Status; }

        }

        public override bool Ok
        {
            get { return Entity.IsValid() && Status.Code == ResultCode.NoError; }
            protected set { if ( value ) Status = _enty.IsValid() ? Status.NoError : _enty.Is().Status;
                else if (Status.Code == ResultCode.NoError) 
                    Status = PasswordServiceError;
            }
        }

        public async Task<UserPasswordsService> ByUserEntity( Task<PasswordUsersService> byUser )
        {
            PasswordUsers user = (await byUser).Entity;
            if( !user ) { _enty = user.Is().Status;
                Status = _enty.Is().Status;
                return this;
            }
            if( _usrs.Status ) {
                Status = _usrs.Status + ResultCode.Password;
                _enty = Status;
            } else if( _enty.User != user.Id ) {
                Status = Status.NoError;
                _enty= Status.Unknown;
                _lazy  = _db.UserPasswords
                         .AsNoTracking()
                         .SingleOrDefaultAsync(p => p.User == user.Id);
            } return this;
        }

        public async Task<UserPasswordsService> SetMasterKey( int userId, string pass )
        {
            if( !( await ByUserEntity(_usrs.ById(userId)) ) ) {
                if ( Status.Code.HasFlag( ResultCode.Password|ResultCode.Service ) ) {
                    Status = Status.NoError;
                    _enty = new UserPasswords();
                    _enty.Hash = Crypt.CalculateHash(pass);
                    BigInteger big = new BigInteger(_enty.Hash);
                    _enty.User = userId;
                    _enty.Pass = "";
                    _enty.Id = 0;
                    _db.UserPasswords.AddAsync(_enty);
                    _db.SaveChangesAsync();
                    return this;
                } else {
                    _enty = PasswordServiceError;
                    return this;
                }
            } else {
                _enty.Hash = Crypt.CalculateHash(pass);
                _db.UserPasswords.Update( _enty );
                _db.SaveChangesAsync();
                return this;
            }
        }

        public bool VerifyPassword( int forUser, string masterPassword )
        {
            if ( ByUserEntity( _usrs.ById( forUser ) ).GetAwaiter().GetResult() ) {
                if( Entity.Hash != Crypt.CalculateHash( masterPassword ) ) {
                    _enty = Status = HashValue.WithData( masterPassword );
                    return false;
                } else Status = Status.NoError;
                return true;
            } return false;
        }

        public Crypt.Key GetMasterKey( int ofUser )
        {
            if ( Entity ) if ( Entity.User == ofUser ) 
                return Crypt.CreateKey( _enty.Hash );
            if ( ByUserEntity( _usrs.ById( ofUser ) ).GetAwaiter().GetResult() )
                return Crypt.CreateKey( _enty.Hash );
            else
                return null;
        }

        public async Task<UserPasswordsService> ByUserId( int ofUser )
        {
            if ( Entity ) if ( Entity.User == ofUser ) return this;
            Status = Status.NoError;
            _enty= Status.Unknown;
            _lazy = _db.UserPasswords.AsNoTracking().SingleOrDefaultAsync(p => p.User == ofUser);
            return this;
        }
    } 
}
