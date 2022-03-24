using System;
using Xunit;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;

namespace Tests.Models
{
    public class EntityBaseClasses
    {
        [Fact]
        public void TestEntityModels()
        {
            UserPasswords password = new UserPasswords();
            Assert.True( password, password.ToString() );
            Assert.True( password.IsValid(), password.ToString() );
            Assert.False( password.Is().Status.Ok );

            password = UserPasswords.Invalid;
            Assert.False( password, password.ToString() );
            Assert.False( password.IsValid(), password.ToString() );
            Assert.False( password.Waiting(), password.ToString() );

            UserLocations location = new UserLocations();
            Assert.True( location, location.ToString() );
            Assert.True( location.IsValid(), location.ToString() );
            Assert.False( location.Is().Status.Ok);

            location = UserLocations.Invalid;
            Assert.False( location, location.ToString() );
            Assert.False( location.IsValid(), location.ToString() );
            Assert.False( location.Waiting(), location.ToString() );

            PasswordUsers user = new PasswordUsers();
            Assert.True( user, user.ToString() );
            Assert.True( user.IsValid(), user.ToString() );
            Assert.False( user.Is().Status.Ok );

            user = PasswordUsers.Invalid;
            Assert.False( user, user.ToString() );
            Assert.False( user.IsValid(), user.ToString() );
            Assert.False( user.Waiting(), user.ToString() );
        }
    }
}
