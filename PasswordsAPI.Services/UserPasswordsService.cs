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


        private PasswordUsersService<CTX>        _usrs;


        public UserPasswordsService( CTX ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService<CTX>,CTX> usr )
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
            get { return Entity.IsValid() && Status.Code == ResultCode.NoState; }
            protected set { if ( value ) Status = _enty.IsValid() ? Status.NoState : _enty.Is().Status;
                else if (Status.Code == ResultCode.NoState) 
                    Status = PasswordServiceError;
            }
        }

        public async Task<UserPasswordsService<CTX>> ForUserAccount( Task<PasswordUsersService<CTX>> byUser )
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
            if( !( await ForUserAccount(_usrs.ById(userId)) ) ) {
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
            if ( ForUserAccount( _usrs.ById( ofUser ) ).GetAwaiter().GetResult() ) {
                if( Entity.Hash != Crypt.CalculateHash( masterPassword ) ) {
                    _enty = Status = HashValue.WithData( masterPassword );
                    return false;
                } else Status = Status.NoState;
                return true;
            } return false;
        }

        public Crypt.Key GetMasterKey( int ofUser )
        {
            UserPasswords pwd = Entity;
            if ( pwd ) if ( pwd.User == ofUser ) 
                return CreateKey( pwd );
            if ( ForUserAccount( _usrs.ById( ofUser ) ).GetAwaiter().GetResult() )
                return Crypt.CreateKey( pwd.Hash );
            else
                return null;
        }

        private Crypt.Key CreateKey( UserPasswords masterdata )
        {
            if( masterdata.IsValid() ) {
                Status = Status.NoState;
                return Crypt.CreateKey( masterdata.Hash );
            } else {
                Status = masterdata.Is().Status;
                return null;
            }
        }

        public async Task<UserPasswordsService<CTX>> ByUserId( int ofUser )
        {
            if ( Entity ) if ( Entity.User == ofUser ) return this;
            Status = Status.NoState;
            Entity = Status.Unknown;
            _lazy  = _dset.AsNoTracking().SingleOrDefaultAsync(p => p.User == ofUser);
            return this;
        }
    } 
}
