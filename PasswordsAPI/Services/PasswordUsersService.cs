using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PasswordsAPI.Services
{
    public class PasswordUsersService 
        : AbstractApiService<PasswordUsers,PasswordUsersService>
        , IPasswordsApiService<PasswordUsers,PasswordUsersService>
    {
        private static readonly ErrorCode UserService = ErrorCode.User|ErrorCode.Service; 
        private static readonly Error InvalidId = new Error(UserService|ErrorCode.Id,"Invalid User.Id: {0}");
        private static readonly Error UsersName = new Error(UserService|ErrorCode.Name, "User.Name {0}");

        protected override Error GetDefaultError() { return new Error(UserService); }

        public int GetUserId( string nameOrId )
        {
            int id = 0;
            if ( !int.TryParse( nameOrId, out id ) ) {
                usr = db.PasswordUsers.AsNoTracking().Single( PasswordUsers => PasswordUsers.Name == nameOrId );
                return usr?.Id ?? -1;
            } return id;
        }

        public PasswordUsersService ByNameOrId( string nameOrId )
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
            get { return Ok ? usr : Error; }
            set { if ( value ) usr = value;
                else Error = value.Is().Error; }
        }

        public PasswordUsersService( PasswordsDbContext ctx )
            : base(ctx)
        {}
        

        public PasswordUsersService CreateNewUser( string name, string email, string pass, string? info )
        {
            IEnumerator<PasswordUsers> it = db.PasswordUsers.AsNoTracking().GetEnumerator();
            while ( it.MoveNext() ) {
                if (it.Current.Name == name) {
                    Error = new Error( UsersName.Code,"Already Exists", name ); 
                break; } 
                if( it.Current.Mail == email ) {
                    Error = new Error( UserService | ErrorCode.Mail, "Already Exists", email ); 
                break; }
            } if (Error) return this;
            it.Dispose();
            usr = db.PasswordUsers.Add(
                new PasswordUsers {
                    Info = info ?? String.Empty,
                    Mail = email,
                    Name = name }
            ).Entity;
            db.SaveChanges();
            return this;
        }

        public PasswordUsersService ById( int byId )
        {
            if (usr) if (usr.Id == byId) return this;
            usr = db.PasswordUsers.AsNoTracking().SingleOrDefault( u => u.Id == byId ) 
                ?? InvalidId.WithData( byId );
            if( usr.Is().Error ) Error = usr.Is().Error;
            return this;
        }

        public PasswordUsersService ByEmail( string email )
        {
            if ( usr.IsValid() ) if ( usr.Mail == email ) return this;
            usr = db.PasswordUsers.AsNoTracking().SingleOrDefault( u => u.Mail == email )
                  ?? new Error( UserService | ErrorCode.Mail, "Unknown email address: '{0}'", email );
            if ( usr.Is().Error ) Error = usr.Is().Error;
            return this;
        }

    }
}
