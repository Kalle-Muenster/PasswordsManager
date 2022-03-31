using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;
using Yps;


namespace PasswordsAPI.Services
{
    public class UserPasswordsService<CTX>
        : AbstractApiService<UserPasswords,UserPasswordsService<CTX>,CTX>
        where CTX : PasswordsApiDbContext<CTX>
    {
        private readonly Status PasswordServiceError = new Status(ResultCode.Service|ResultCode.Password|ResultCode.IsError);
        private readonly Status InvalidId = new Status(ResultCode.Service|ResultCode.Password|ResultCode.User|ResultCode.Invalid, "Invalid User.Id: {0}");
        private readonly Status HashValue = new Status(ResultCode.Service|ResultCode.Password|ResultCode.Id|ResultCode.Invalid, "password incorrct '{0}'" );
        protected override Status GetDefaultError() { return PasswordServiceError; }
        protected override ResultCode GetServiceFlags() { return ResultCode.Password|ResultCode.Service; }
        protected override UserPasswords GetStatusEntity(Status cast) { return cast; }


        private PasswordUsersService<CTX>        _usrs;


        public UserPasswordsService( CTX ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService<CTX>,CTX> usr )
            : base(ctx)
        {
            _usrs = usr.serve();
            _enty = UserPasswords.Invalid;
            _lazy = new Task<UserPasswords>(() => { return _enty; });
        }


        public override bool Ok
        {
            get { return Entity.IsValid() && Status.Code == ResultCode.NoState; }
            protected set { if ( value ) Status = _enty.IsValid() ? Status.NoState : _enty.Is().Status;
                else if (Status.Code == ResultCode.NoState) 
                    Status = PasswordServiceError;
            }
        }

        public async Task<UserPasswordsService<CTX>> LookupPasswordByUserAccount( Task<PasswordUsersService<CTX>> byUser )
        {
            PasswordUsers user = byUser.IsCompleted ? byUser.Result.Entity : (await byUser).Entity;
            if( !user.IsValid() ) { _enty = user.Is().Status;
                Status = _enty.Is().Status;
                return this;
            }
            if( _usrs.Status ) {
                Status = _usrs.Status + ResultCode.Password;
                _enty = Status;
            } else if( _enty.User != user.Id ) {
                Status = Status.NoState;
                _enty= Status.Unknown;
                _lazy  = _dset.AsNoTracking().SingleOrDefaultAsync(p => p.User == user.Id);
            } return this;
        }

        public async Task<UserPasswordsService<CTX>> SetMasterKey( int userId, string pass )
        {
            if( !( await LookupPasswordByUserAccount(_usrs.GetUserById(userId)) ) ) {
                if ( Status.Code.HasFlag( ResultCode.Password|ResultCode.Service ) ) {
                    Status = Status.NoState;
                    _enty = new UserPasswords();
                    _enty.Hash = Crypt.CalculateHash(pass);
                    _enty.User = userId;
                    _enty.Pass = "";
                    _enty.Id = 0;
                    _dset.AddAsync(_enty);
                    _db.SaveChangesAsync();
                    return this;
                } else {
                    _enty = PasswordServiceError;
                    return this;
                }
            } else {
                _enty.Hash = Crypt.CalculateHash(pass);
                _dset.Update( _enty );
                _db.SaveChangesAsync();
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
            _lazy  = _dset.AsNoTracking().SingleOrDefaultAsync( p => p.User == ofUser );
            return this;
        }
    } 
}
