using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PasswordsAPI.Services
{
    public class PasswordUsersService 
        : AbstractApiService<PasswordUsers,PasswordUsersService>
        , IPasswordsApiService<PasswordUsers,PasswordUsersService>
    {
        private static readonly Status UserServiceError = new Status(ResultCode.User|ResultCode.Service|ResultCode.Invalid); 
        private static readonly Status InvalidId = new Status(UserServiceError.Code|ResultCode.Id,"Invalid User.Id: {0}");
        private static readonly Status UsersName = new Status(UserServiceError.Code|ResultCode.Name, "User.Name {0}");

        protected override Status GetDefaultError() { return UserServiceError; }

        public int GetUserId( string nameOrId )
        {
            int id = 0;
            if ( !int.TryParse( nameOrId, out id ) ) {
                _enty = _db.PasswordUsers.AsNoTracking().SingleOrDefault(u => u.Name == nameOrId) ??
                      UsersName.WithData( nameOrId );
                return _enty?.Id ?? -1;
            } return id;
        }

        public async Task<PasswordUsersService> ByNameOrId( string nameOrId )
        {
            int id = 0;
            if ( int.TryParse( nameOrId, out id ) ) {
                if ( _enty ) if (_enty.Id == id) return this;
                _enty = Status.Unknown;
                _lazy = _db.PasswordUsers.AsNoTracking().SingleOrDefaultAsync( u => u.Id == id );
            } else {
                if ( _enty ) if ( _enty.Name == nameOrId ) return this;
                _enty = Status.Unknown;
                _lazy = _db.PasswordUsers.AsNoTracking().SingleOrDefaultAsync( u => u.Name == nameOrId );
            } Status = Status.NoError;
            return this;
        }

        public override PasswordUsers Entity {
            get { if (_enty.Is().Status.Waiting) {
                    _enty = _lazy.GetAwaiter().GetResult();
                    Status = _enty.Is().Status; }
                return Ok ? _enty : Status; }
            set { if ( value ) _enty = value;
                else Status = value.Is().Status; }
        }

        public PasswordUsersService(PasswordsDbContext ctx)
            : base(ctx)
        {
            _enty = PasswordUsers.Invalid;
            _lazy = new Task<PasswordUsers>(() => { return _enty; });
            Status = GetDefaultError();
        }
        

        public async Task<PasswordUsersService> CreateNewUser( string name, string email, string pass, string? info )
        {
            StdStream.Out.Write("CreateNewUser():");
            IEnumerator<PasswordUsers> it = _db.PasswordUsers.AsNoTracking().GetEnumerator();
            Status = Status.NoError;
            while ( it.MoveNext() ) {
                if (it.Current.Name == name) {
                    Status = new Status( UsersName.Code,"Already Exists", name ); 
                break; } 
                if( it.Current.Mail == email ) {
                    Status = UserServiceError.WithText("Already Exists").WithData(email) + ResultCode.Mail; 
                break; }
            } it.Dispose();
            if ( Status.Bad ) { StdStream.Out.WriteLine("CreateNewUser():...returns Bad: {0}", Status ); return this;}
            _enty = _db.PasswordUsers.Add(
                new PasswordUsers {
                    Info = info ?? String.Empty,
                    Mail = email,
                    Name = name,
                    Icon = Array.Empty<byte>() }
            ).Entity;
            _db.SaveChanges();
            return this;
        }

        public async Task<PasswordUsersService> ById( int byId )
        {
            if (_enty) if (_enty.Id == byId) return this;
            _enty = Status.Unknown;
            _lazy = _db.PasswordUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == byId);
            Status = Status.NoError;
            return this;
        }

        public async Task<PasswordUsersService> ByEmail( string email )
        {
            if ( _enty.IsValid() ) if ( _enty.Mail == email ) return this;
            _enty = new Status(UserServiceError.Code | ResultCode.Mail | ResultCode.Unknown, "Unknown email address: '{0}'", email);
            _lazy = _db.PasswordUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Mail == email);
            Status = Status.NoError;
            return this;
        }

        public async Task<PasswordUsersService> RemoveUser( PasswordUsers account )
        {
            _db.PasswordUsers.Remove( account );
            Status = (Status.Success + ResultCode.User).WithText(
                $"Successfully removed user account {account.Name} (id:{account.Id}) from db"
            ).WithData( account );
            _db.SaveChangesAsync();
            return this;
        }
    }
}
