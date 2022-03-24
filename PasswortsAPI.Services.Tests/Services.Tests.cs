using System;
using Xunit;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;
using PasswordsAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Services.Tests
{
    public class TestContext : PasswordsApiDbContext<TestContext>
    {
        public TestContext() : base(new DbContextOptions<TestContext>())
        {
            // nö
            this.SetEntities<PasswordUsers>( new InternalDbSet<PasswordUsers>(this, "PasswordUsers") );
            this.SetEntities<UserPasswords>( new InternalDbSet<UserPasswords>(this, "PasswordUsers") );
            this.SetEntities<UserLocations>( new InternalDbSet<UserLocations>(this, "PasswordUsers") );
        }
    }

    public class ServiceTests
    {
        [Fact] private void UserService()
        {
            PasswordUsersService<TestContext> users = new PasswordUsersService<TestContext>(new TestContext(null));
            PasswordUsers account = users.ById(5).GetAwaiter().GetResult().Entity;
            Assert.False( account.IsValid() );
            Assert.True( account.Is().Status.Code.HasFlag( ResultCode.User ), account.ToString() );
        }
    }
}
