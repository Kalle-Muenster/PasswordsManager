using System;
using Xunit;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;

namespace Tests.Models
{
    public class EntityBaseClasses
    {
        [Fact]
        public void PasswordsConstructor_ShouldBehaveLikeExpected()
        {
            // Arrange
            var password = new UserPasswords();

            // Assert
            Assert.True(password, password.ToString());
            Assert.True(password.IsValid(), password.ToString());
            Assert.False(password.Is().Status.Ok);
        }

        [Fact]
        public void PasswordConstructor_ShouldBehaveLikeExpected()
        {
            // Arrange
            var user = new PasswordUsers();

            // Assert
            Assert.True(user, user.ToString());
            Assert.True(user.IsValid(), user.ToString());
            Assert.False(user.Is().Status.Ok);
        }

        [Fact]
        public void UserLocationConstructor_ShouldBehaveLikeExpected()
        {
            // Arrange
            var location = new UserLocations();

            // Assert
            Assert.True(location, location.ToString());
            Assert.True(location.IsValid(), location.ToString());
            Assert.False(location.Is().Status.Ok);
        }

        [Fact]
        public void TestEntityModels2()
        {
            // Arrange
            UserPasswords password = UserPasswords.Invalid;
            UserLocations location = UserLocations.Invalid;
            PasswordUsers user = PasswordUsers.Invalid;

            // Assert
            Assert.False(password, password.ToString());
            Assert.False(password.IsValid(), password.ToString());
            Assert.False(password.Waiting(), password.ToString());
            Assert.False(location, location.ToString());
            Assert.False(location.IsValid(), location.ToString());
            Assert.False(location.Waiting(), location.ToString());
            Assert.False(user, user.ToString());
            Assert.False(user.IsValid(), user.ToString());
            Assert.False(user.Waiting(), user.ToString());
        }
    }
}
