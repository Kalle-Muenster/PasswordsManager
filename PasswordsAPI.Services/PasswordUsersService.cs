using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Passwords.API.Abstracts;
using Passwords.API.Models;

namespace Passwords.API.Services
{
    public class PasswordUsersService<CTX>
        : AbstractApiService< PasswordUsers, PasswordUsersService<CTX>, CTX > 
    where CTX 
        : DbContext
        , IPasswordaApiDbContext<CTX>
    {
        private static readonly Status UserServiceError = new Status(ResultCode.User|ResultCode.Service|ResultCode.Invalid, "Invalid User: '{0}'"); 
        private static readonly Status InvalidId = new Status(UserServiceError.Code|ResultCode.Id,"Invalid User.Id: {0}");
        private static readonly Status UsersName = new Status(UserServiceError.Code|ResultCode.Name,"Invalid User.Name {0}");

        protected override Status GetDefaultError() { return UserServiceError; }
        protected override ResultCode GetServiceFlags() { return ResultCode.User; }
        protected override PasswordUsers GetStatusEntity( Status cast ) { return cast; }

        public PasswordUsersService( CTX ctx )
            : base(ctx)
        {
            Status = UserServiceError;
            _enty = PasswordUsers.Invalid;
            _lazy = new Task<PasswordUsers>(()=>_enty);
        }

        public int GetUserId( string nameOrId )
        {
            int id = 0;
            if( !int.TryParse( nameOrId, out id ) ) {
                if (Entity) if (Entity.Name == nameOrId) return Entity.Id;
                Entity = _dset.SingleOrDefault( u => u.Name == nameOrId )
                         ?? UsersName.WithData( nameOrId );
                return _enty?.Id ?? -1;
            } if (Entity) if (Entity.Id == id) return id;
            Entity = _dset.SingleOrDefault( u => u.Id == id )
                     ?? InvalidId.WithData( nameOrId );
            if (Entity) if (Entity.Id == id) return id;
            return -1;
        }

        public async Task<PasswordUsersService<CTX>> GetUserByNameOrId( string nameOrId )
        {
            int id = 0;
            if ( int.TryParse( nameOrId, out id ) ) {
                if ( Entity.IsValid() ) if ( Entity.Id == id) return this;
                _enty = new Status( ResultCode.Unknown | ResultCode.Id, "Unknown user id: '{0}'", nameOrId );
                _lazy = _dset.SingleOrDefaultAsync( u => u.Id == id );
            } else {
                if ( _enty.IsValid() ) if (_enty.Name == nameOrId) return this;
                _enty = new Status( ResultCode.Unknown | ResultCode.Name, "Unknown user name: '{0}'", nameOrId );
                _lazy = _dset.SingleOrDefaultAsync( u => u.Name == nameOrId );
            } Status = Status.NoState.WithData( nameOrId );
            return this;
        }

        public async Task<PasswordUsersService<CTX>> CreateNewAccount( string name, string email, string? info )
        {
            IEnumerator<PasswordUsers> it = _dset.AsTracking().GetEnumerator();
            Status = Status.NoState;
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
                new PasswordUsers( name, email, info ?? string.Empty )
            ).Entity;
            _db.SaveChanges();
            Status = Status.NoState.WithData( "MasterKey" );
            return this;
        }

        public List<PasswordUsers> ListUserAccounts()
        {
            IEnumerator<PasswordUsers> usinger = _dset.AsNoTracking().GetEnumerator();
            List<PasswordUsers> listinger = new List<PasswordUsers>();
            while ( usinger.MoveNext() ) {
                listinger.Add( usinger.Current );
            } usinger.Dispose();
            return listinger;
        }

        public async Task<PasswordUsersService<CTX>> GetUserById( int byId )
        {
            if( _enty.IsValid() ) if( _enty.Id == byId ) return this;
            Status = Status.NoState.WithData( byId );
            _enty = new Status( ResultCode.Unknown|ResultCode.Id, "Unknown user id: '{0}'", byId );
            _lazy = _dset.SingleOrDefaultAsync(u => u.Id == byId);
            return this;
        }

        public async Task<PasswordUsersService<CTX>> GetUserByEmail( string email )
        {
            if ( _enty.IsValid() ) if ( _enty.Mail == email ) return this;
            Status = Status.NoState.WithData( email );
            _enty = new Status( ResultCode.Unknown|ResultCode.Mail, "Wrong email address: '{0}'", email );
            _lazy = _dset.SingleOrDefaultAsync(u => u.Mail == email);
            return this;
        }

        public async Task<PasswordUsersService<CTX>> RemoveUserAccount( PasswordUsers account )
        {
            _dset.Remove( account );
            Status = (Status.Success + ResultCode.User).WithText(
                $"Deleted user {account.Id}: {account.Name} and related data"  
            ).WithData( account );
            _db.SaveChanges();
            _enty = account;
            return this;
        }
    }
}
