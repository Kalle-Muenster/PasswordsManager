using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using Passwords.API.Tests.Helpers;

namespace Passwords.API.Services.Tests
{
    public class PasswordUsersServiceTests : IDisposable
    {

        public static void AssertUserAccount(PasswordUsers account, ITuple expects)
        {
            Assert.True(account.Id == (int)expects[0], "Id");
            Assert.True(account.Name.Equals(expects[1]), "Name");
            Assert.True(account.Mail.Equals(expects[2]), "Mail");
            Assert.True(account.Info.Equals(expects[3]), "Info");
        }


        [Fact]
        private void UserService_CreateNewAccountWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "EmptyDataBase");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            Status result = service.CreateNewAccount("ElNamo", "El@Mailo.de", "ElInfo").GetAwaiter().GetResult().Status;
            PasswordUsers account = service.Entity;

            // Assert
            Assert.False(result, result);
            Assert.True(account.IsValid(), service.Entity.ToString());
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));
        }

        [Fact]
        private void UserService_GetAccountListingWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            List<PasswordUsers> result = service.ListUserAccounts();

            // Assert
            Assert.True((result != null), "result not null");
            Assert.True(result.Count == 1, result.Count.ToString());
            AssertUserAccount(result[0], (1, "ElNamo", "El@Mailo.de", "ElInfo"));
        }

        [Fact]
        private void UserService_GetAccountByIdWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserById(1).GetAwaiter().GetResult().Entity;

            // Assert
            Assert.True(account, account.ToString());
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));
        }

        [Fact]
        private void UserService_GetAccountByNameWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByNameOrId("ElNamo").GetAwaiter().GetResult().Entity;

            // Assert
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));
        }

        [Fact]
        private void UserService_GetAccountByEmailWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByEmail("El@Mailo.de").GetAwaiter().GetResult().Entity;

            // Assert
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));
        }


        [Fact]
        private void UserService_ErrorOn_GetAccountByInvalidId()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserById(23).GetAwaiter().GetResult().Entity;

            // Assert
            Model.AssertInvalidCase(account);
        }

        [Fact]
        private void UserService_ErrorOnGetAccountByInvalidName()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByNameOrId("Egon").GetAwaiter().GetResult().Entity;

            // Assert
            Assert.False(service.Status, service.Status.ToString());
            Model.AssertInvalidCase(account);
        }

        [Fact]
        private void UserService_ErrorOn_GetAccountByInvalidEmail()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByEmail("El@Peilo.de").GetAwaiter().GetResult().Entity;

            // Assert
            Assert.False(service, service.ToString());
            Model.AssertInvalidCase(account);
        }

        [Fact]
        private void UserService_RemoveUserAccountWorks()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            Status result = service.RemoveUserAccount(service.GetUserById(1).GetAwaiter().GetResult().Entity).GetAwaiter().GetResult().Status;

            Assert.True( result, result );
        }

        public void Dispose()
        {
            Test.CurrentContext.Finished();
        }
    }
}
