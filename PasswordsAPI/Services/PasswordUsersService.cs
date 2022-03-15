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
                usr = db.PasswordUsers.AsNoTracking().Single( PasswordUsers => PasswordUsers.Name == nameOrId );
                return usr?.Id ?? -1;
            } return id;
        }

        public async Task<PasswordUsersService> ByNameOrId( string nameOrId )
        {
            int id = 0;
            if ( int.TryParse( nameOrId, out id ) ) {
                if ( usr ) if (usr.Id == id) return this;
                usr = db.PasswordUsers.AsNoTracking().Single( PasswordUsers => PasswordUsers.Id == id );
            } else {
                usr = db.PasswordUsers.AsNoTracking().Single( PasswordUsers => PasswordUsers.Name == nameOrId );
            } return this;
        }

        private PasswordUsers usr = (PasswordUsers)PasswordUsers.Invalid;

        public override PasswordUsers Entity {
            get { return Ok ? usr : Status; }
            set { if ( value ) usr = value;
                else Status = value.Is().Status; }
        }

        public PasswordUsersService( PasswordsDbContext ctx )
            : base(ctx)
        {}
        

        public async Task<PasswordUsersService> CreateNewUser( string name, string email, string pass, string? info )
        {
            IEnumerator<PasswordUsers> it = db.PasswordUsers.AsNoTracking().GetEnumerator();
            while ( it.MoveNext() ) {
                if (it.Current.Name == name) {
                    Status = new Status( UsersName.Code,"Already Exists", name ); 
                break; } 
                if( it.Current.Mail == email ) {
                    Status = UserServiceError.WithText("Already Exists").WithData(email) + ResultCode.Mail; 
                break; }
            } if (Status) return this;
            it.Dispose();
            usr = db.PasswordUsers.Add(
                new PasswordUsers {
                    Info = info ?? String.Empty,
                    Mail = email,
                    Name = name,
                    Icon = Array.Empty<byte>()
                }
            ).Entity;
            db.SaveChanges();
            return this;
        }

        public async Task<PasswordUsersService> ById( int byId )
        {
            if (usr) if (usr.Id == byId) return this;
            usr = db.PasswordUsers.AsNoTracking().SingleOrDefault( u => u.Id == byId ) 
                ?? InvalidId.WithData( byId );
            if( usr.Is().Status ) Status = usr.Is().Status;
            return this;
        }

        public async Task<PasswordUsersService> ByEmail( string email )
        {
            if ( usr.NoError() ) if ( usr.Mail == email ) return this;
            usr = db.PasswordUsers.AsNoTracking().SingleOrDefault( u => u.Mail == email )
                  ?? new Status( UserService | ErrorCode.Mail, "Unknown email address: '{0}'", email );
            if ( usr.Is().Status ) Status = usr.Is().Status;
            return this;
        }

        public async Task<PasswordUsersService> RemoveUser( PasswordUsers account )
        {
            db.PasswordUsers.Remove( account );
            db.SaveChanges();
            Status = (Status.Success + ErrorCode.Cryptic).WithText(
                $"Successfully removed user {account.Name} (id:{account.Id}) from db"
            ).WithData( account );
            return this;
        }
    }
}
