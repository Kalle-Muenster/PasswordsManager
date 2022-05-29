using System;
using Xunit;
using Passwords.API.Tests.Helpers;

namespace Passwords.API.Tests
{
    public class PasswordsControllerTests : IDisposable
    {
        [Fact]
        public void PasswordsController_GetUsersWorks()
        {
            Test.Context.PrepareDataBase("PasswordsController", "OneUserOneLocation");

            

        }

        public void Dispose()
        {
            Test.CurrentContext.Finished();
        }
    }
}
