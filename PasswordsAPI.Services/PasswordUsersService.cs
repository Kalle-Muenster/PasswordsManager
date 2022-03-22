using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;

namespace PasswordsAPI.Services
{
    public class PasswordUsersService<CTX>
        : AbstractApiService<PasswordUsers,PasswordUsersService<CTX>,CTX> 
        where CTX : DbContext, IPasswordaApiDbContext<CTX>
    {
        private static readonly Status UserServiceError = new Status(ResultCode.User|ResultCode.Service|ResultCode.Invalid); 
        private static readonly Status InvalidId = new Status(UserServiceError.Code|ResultCode.Id,"Invalid User.Id: {0}");
        private static readonly Status UsersName = new Status(UserServiceError.Code|ResultCode.Name,"Invalid User.Name {0}");

        protected override Status GetDefaultError() { return UserServiceError; }

        public int GetUserId( string nameOrId )
        {
            int id = 0;
            if ( !int.TryParse( nameOrId, out id ) ) {
                if ( Entity ) if ( Entity.Name == nameOrId ) return Entity.Id;
                Entity = _dset.AsNoTracking().SingleOrDefault(u => u.Name == nameOrId)
                      ?? UsersName.WithData( nameOrId );
                Status = _enty.Is().Status;
                return _enty?.Id ?? -1;
            } 
            if( Entity ) if ( Entity.Id == id ) return id;
            Entity = _dset.AsNoTracking().SingleOrDefault(u => u.Id == id)
                  ?? InvalidId.WithData( id );
            if (Entity) if (Entity.Id == id) return id;
            return -1;
        }

        public async Task<PasswordUsersService<CTX>> ByNameOrId( string nameOrId )
        {
            int id = 0;
            if ( int.TryParse( nameOrId, out id ) ) {
                if ( _enty ) if (_enty.Id == id) return this;
                _enty = Status.Unknown;
                _lazy = _dset.AsNoTracking().SingleOrDefaultAsync( u => u.Id == id );
            } else {
                if ( _enty ) if ( _enty.Name == nameOrId ) return this;
                _enty = Status.Unknown;
                _lazy = _dset.AsNoTracking().SingleOrDefaultAsync( u => u.Name == nameOrId );
            } Status = Status.NoError;
            return this;
        }

        public override PasswordUsers Entity {
            get { if (_enty.Is().Status.Waiting) {
                    _enty = _lazy.GetAwaiter().GetResult() ?? UserServiceError;
                    Status = _enty.Is().Status; }
                return Ok ? _enty : Status; }
            set { if ( value ) _enty = value;
                else Status = value.Is().Status; }
        }

        public PasswordUsersService( CTX ctx )
            : base(ctx)
        {
            Status = UserServiceError;
            _enty = PasswordUsers.Invalid;
            _lazy = new Task<PasswordUsers>(()=>_enty);
        }
        
        public async Task<PasswordUsersService<CTX>> CreateNewUser( string name, string email, string pass, string? info )
        {
            IEnumerator<PasswordUsers> it = _dset.AsNoTracking().GetEnumerator();
            Status = Status.NoError;
            while ( it.MoveNext() ) {
                if( it.Current.Name == name ) {
                    Status = new Status( UsersName.Code,"Already Exists", name ); 
                break; } 
                if( it.Current.Mail == email ) {
                    Status = UserServiceError.WithText( "Already Exists" ).WithData( email ) + ResultCode.Mail; 
                break; }
            } it.Dispose();
            if ( Status.Bad ) return this;
            _enty = _dset.Add(
                new PasswordUsers {
                    Info = info ?? String.Empty,
                    Mail = email,
                    Name = name,
                    Icon = Array.Empty<byte>() }
            ).Entity;
            _db.SaveChanges();
            return this;
        }

        public List<PasswordUsers> ListAllUsers()
        {
            IEnumerator<PasswordUsers> usinger = _dset.AsNoTracking().GetEnumerator();
            List<PasswordUsers> listinger = new List<PasswordUsers>();
            while (usinger.MoveNext()) {
                listinger.Add( usinger.Current );
            } usinger.Dispose();
            return listinger;
        }

        public async Task<PasswordUsersService<CTX>> ById( int byId )
        {
            Status = Status.NoError;
            if (_enty) if (_enty.Id == byId) return this;
            _enty = Status.Unknown;
            _lazy = _dset.AsNoTracking().SingleOrDefaultAsync(u => u.Id == byId);
            return this;
        }

        public async Task<PasswordUsersService<CTX>> ByEmail( string email )
        {
            Status = Status.NoError;
            if ( _enty.IsValid() ) if ( _enty.Mail == email ) return this;
            _enty = new Status(UserServiceError.Code | ResultCode.Mail | ResultCode.Unknown, "Wrong email address: '{0}'", email);
            _lazy = _dset.AsNoTracking().SingleOrDefaultAsync(u => u.Mail == email);
            return this;
        }

        public async Task<PasswordUsersService<CTX>> RemoveUser( PasswordUsers account )
        {
            _dset.Remove( account );
            Status = (Status.Success + ResultCode.User).WithText(
                $"Deleted user {account.Id}: {account.Name} and related data"  
            ).WithData( account );
            _db.SaveChangesAsync();
            return this;
        }
    }
}
