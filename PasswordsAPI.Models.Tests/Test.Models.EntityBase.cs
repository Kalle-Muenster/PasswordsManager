using Xunit;
using Passwords.API.Abstracts;

namespace Passwords.API.Models.Tests
{
    public class EntitieConstructors
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
    }

    public class EntityInvalidConstants
    {
        [Fact]
        public void InvalidUserPasswords_ShouldBehaveLikeExpected()
        {
            // Arrange
            UserPasswords password = UserPasswords.Invalid;

            // Assert
            Assert.False(password, password.ToString());
            Assert.False(password.IsValid(), password.ToString());
            Assert.False(password.Waiting(), password.ToString());
        }

        [Fact]
        public void InvalidUserLocations_ShouldBehaveLikeExpected()
        {
            // Arrange
            UserLocations location = UserLocations.Invalid;

            // Assert
            Assert.False(location, location.ToString());
            Assert.False(location.IsValid(), location.ToString());
            Assert.False(location.Waiting(), location.ToString());
        }

        [Fact]
        public void InvalidPasswordUsers_ShouldBehaveLikeExpected()
        {
            // Arrange
            PasswordUsers user = PasswordUsers.Invalid;

            // Assert
            Assert.False(user, user.ToString());
            Assert.False(user.IsValid(), user.ToString());
            Assert.False(user.Waiting(), user.ToString());
        }
    }
}
