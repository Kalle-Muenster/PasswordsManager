using System;
using System.Runtime.CompilerServices;
using Xunit;
using System.Threading;
using Passwords.API.Models;
using Passwords.API.Tests.Helpers;
using Passwords.API.Abstracts;

namespace Passwords.API.Services.Tests
{
    public class UserLocationsServiceTests : IDisposable
    {
        public static void AssertUserLocation(UserLocations location, ITuple expects)
        {
            Assert.True(location.Id == (int)expects[0], "Id");
            Assert.True(location.User == (int)expects[1], "User");
            Assert.True(location.Area.Equals(expects[2]), "Area");
            if (expects.Length > 3)
                Assert.True(location.Name.Equals(expects[3]),"Name");
            if (expects.Length > 4)
                Assert.True(location.Info.Equals(expects[4]), "Info");
            Assert.True(location.Pass.Length > 0, "Pass");
        }

        [Fact]
        private void UserLocations_AddLocationWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneValidUserAccount");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey );
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = new UserLocations();
            location.User = 1;
            location.Area = "ElLoco";
            location.Name = "ElNamo";
            location.Info = "ElInfo";
            location = service.SetLocationPassword( usinger.GetUserById(location.User), location, "ElPasso").GetAwaiter().GetResult().Entity;

            Assert.True(service, service.Status);
        }

        [Fact]
        private void UserLocations_GetLocationIdByUserAndName()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            int locationId = service.GetAreaId( "ElLoco", 1 );

            Assert.Equal( 1, locationId );
        }

        [Fact]
        private void UserLocations_GetLocationByIdWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = service.GetLocationById(1).GetAwaiter().GetResult().Entity;

            Model.AssertSuccessCase( location );
            AssertUserLocation( location, (1, 1, "ElLoco") );
            Assert.False( System.Text.Encoding.Default.GetString(location.Pass).Equals("ElPasso"), location.ToString() );
        }

        [Fact]
        private void UserLocations_GetLocationByUserWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = service.GetLocationOfUser(1, "ElLoco");

            Model.AssertSuccessCase( location );
            AssertUserLocation( location, (1, 1, "ElLoco") );
            Assert.False( System.Text.Encoding.Default.GetString( location.Pass ).Equals( "ElPasso" ), location.ToString() );
        }

        [Fact]
        private void UserLocations_GetLocationList()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = service.GetUserLocations(1)[0];

            Model.AssertSuccessCase( location );
            AssertUserLocation( location, (1, 1, "ElLoco") );
            Assert.False( System.Text.Encoding.Default.GetString( location.Pass ).Equals( "ElPasso" ), location.ToString() );
        }

        [Fact]
        private void UserLocations_SetLocationInfo()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = service.SetLoginInfo( service.GetLocationOfUser(1,"ElLoco").Id, "ElNamo", "ElInfo" ).GetAwaiter().GetResult().Entity;

            Assert.True( service.Ok, service.ToString() );
            Model.AssertSuccessCase( location );
            AssertUserLocation( location, (1, 1, "ElLoco","ElNamo","ElInfo") );
        }

        [Fact]
        private void UserLocations_GetPasswordWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            string password = service.GetLocationById(1).GetAwaiter().GetResult().SetKey("ElMaestro").GetAwaiter().GetResult().GetPassword();

            Assert.True( password.Equals("ElPasso"), service.ToString() );
        }

        [Fact]
        private void UserLocations_ChangePasswordWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            UserLocations location = new UserLocations();
            location.User = 1;
            location.Id = 1;
            location.Area = "ElLoco";
            service.SetLocationPassword( usinger.GetUserById(1), location, "ElBongo").GetAwaiter().GetResult();
            Thread.Sleep(1000);
            string password = usrkeys.GetMasterKey(1).Decrypt( service.GetPassword() );
            
            Assert.True( password.Equals("ElBongo"), password );
        }

        [Fact]
        private void UserLocations_RemoveLocationWorks()
        {
            Test.Context.PrepareDataBase("UserLocationsService", "OneUserOneLocation");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> usrkeys = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger, Test.CurrentApikey);
            UserLocationsService<Test.Context> service = new UserLocationsService<Test.Context>(Test.CurrentContext, usrkeys);

            Status result = service.RemoveLocation( 1, "ElLoco", "ElMaestro" ).GetAwaiter().GetResult().Status;
            
            Assert.False( result.Bad, result );
        }


        public void Dispose()
        {
            Test.CurrentContext.Finished();
        }
    }
}
