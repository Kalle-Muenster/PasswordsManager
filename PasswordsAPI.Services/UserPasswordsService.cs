using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using Yps;


namespace Passwords.API.Services
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
        private CryptKey                         _apky;
        private CryptBuffer                      _data;

        public UserPasswordsService( CTX ctx, IPasswordsApiService<PasswordUsers,PasswordUsersService<CTX>,CTX> usr, CryptKey api )
            : base(ctx)
        {
            _usrs = usr.serve();
            _enty = UserPasswords.Invalid;
            _lazy = new Task<UserPasswords>(() => { return _enty; });
            _apky = api;
            _data = new CryptBuffer(512);
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

        public Status DecryptParameter( string data )
        {
            bool usingAppKey = !Entity.IsValid();
            if ( usingAppKey ) {
                data = _apky.Decrypt( data );
            } else {
                data = GetMasterKey( Entity.Id ).Decrypt( data );
            }

            if( Crypt.Error ) {
                return Status = new Status(
                    ResultCode.Cryptic | ResultCode.Invalid |
                    ResultCode.Service, $"{Crypt.Error} - ApiKey Invalid",
                    System.Array.Empty<string>()
                );
            } else {
                return Status.Success.WithData(
                    usingAppKey ? data.Split(".~.")
                   : data.Substring(3).Split(".~.")
                );
            }
            // TODO: so machen:
            //CryptBuffer outer = new CryptBuffer( System.Text.Encoding.Default.GetBytes(data) );
            //CryptBuffer.OuterCrypticStringEnumerator it = outer.GetOuterCrypticStringEnumerator(key, 0);
            //it.Search = new StringSearch24(name);
            //while ( it.MoveNext() );
            //if ( it.Search.Found ) it.Position = it.Search.FoundAt( it.Position )/4;
        }

        public async Task<UserPasswordsService<CTX>> SetMasterKey( int userId, string pass )
        {

            if ( !( await LookupPasswordByUserAccount(_usrs.GetUserById(userId)) ) ) {
                if ( Status.Code.HasFlag( ResultCode.Password|ResultCode.Service ) ) {
                    Status = Status.NoState;
                    _enty = new UserPasswords();
                    _enty.Hash = Crypt.CalculateHash( pass );
                    _enty.User = userId;
                    _enty.Pass = "";
                    _enty.Id = 0;
                    _dset.Add(_enty);
                    _db.SaveChanges();
                    return this;
                } else {
                    _enty = PasswordServiceError;
                    return this;
                }
            } else {
                _enty.Hash = Crypt.CalculateHash( pass );
                _dset.Update( _enty );
                _db.SaveChanges();
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
