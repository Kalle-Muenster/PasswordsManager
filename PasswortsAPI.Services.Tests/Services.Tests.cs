using System;
using Consola;
using System.IO;
using Xunit;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Models;
using PasswordsAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using Yps;
using Constringer = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Connectione = Microsoft.Data.Sqlite.SqliteConnection;


namespace Services.Tests
{
    public class Test
    {
        public static Context CurrentContext;
        public class SessionKey
        {
            private static volatile bool running;
            private static Random keygen;
            private int session;

            static SessionKey() {
                keygen = new Random(DateTime.Now.Millisecond);
                running = false;
            }

            public SessionKey() {
                running = LockSession();
            }

            public override int GetHashCode() {
                return session;
            }

            public void Unlock(SessionKey key) {
                if (session == key.session)
                    running = false;
            }

            public bool LockSession() {
                while(running) Thread.Sleep(100);
                session = keygen.Next(int.MaxValue);
                bool state = !running;
                if (state) running = true;
                return state;
            }
        }

        
        public static void AssertInvalidCase<M>(M modelclass) where M : EntityBase<M>, new()
        {
            Assert.False(modelclass, modelclass.ToString());
            Assert.False(modelclass.IsValid(), modelclass.ToString());
            Assert.False(modelclass.Waiting(), modelclass.ToString());
            Assert.False(modelclass.Is().Status.Ok, modelclass.ToString());
            Assert.True(modelclass.Is().Status.Bad, modelclass.ToString());
        }

        public class Context : PasswordsApiDbContext<Context>
        {
            public static DbContextOptions<Context> CreateTestOptions( string database )
            {
                lock (Thread.CurrentThread) {
                    Thread.Sleep(2000);
                    string project = "C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI";
                    StdStream.Cwd = project + "\\DataBase\\Tests";
                    StdStream.ProrammExec("C:\\Windows\\System32\\cmd.exe /c \"del /f /q db.db\"", 0);
                    StdStream.ProrammExec(
                        $"C:\\Windows\\System32\\cmd.exe /c \"copy {project}\\DataBase\\Tests\\{database}\\db.db {StdStream.Cwd}\"",
                        0);
                    return new DbContextOptionsBuilder<Context>().UseSqlite(
                        new Connectione(
                            new Constringer(string.Format("Data Source={0}",
                                new FileInfo($"{project}\\DataBase\\Tests\\db.db").FullName)).ConnectionString)
                    ).Options as DbContextOptions<Context>;
                }
            }

            public static Context PrepareDataBase( string testcase, string database )
            {
                if (CurrentContext != null)
                    if (CurrentContext.Name.Equals(database))
                        return CurrentContext.Begin();
                SessionKey key = new SessionKey();
                lock (key) { 
                    return CurrentContext = new Context(database,key);
                }
            }

            public readonly string Name;
            private SessionKey     Key;
            public Context Begin(){ Key.LockSession(); return this; }
            public void Finished(){ Key.Unlock( Key ); }
            public Context(string testcase, SessionKey key) : base(Context.CreateTestOptions(testcase))
            {
                Name = testcase;
                Key = key;
            }

            public DbSet<PasswordUsers> PasswordUsers {
                get { return EntitiesSet<PasswordUsers>(); }
                set { SetEntities(value); }
            }

            public DbSet<UserPasswords> UserPasswords {
                get { return EntitiesSet<UserPasswords>();}
                set { SetEntities(value); }
            }

            public DbSet<UserLocations> UserLocations
            {
                get { return EntitiesSet<UserLocations>(); }
                set { SetEntities(value); }
            }
        }
    }

    public class PasswordUsersServiceTests
    {

        public void AssertUserAccount( PasswordUsers account, ITuple expects )
        {
            Assert.True( account.Id == (int)expects[0], "Id" );
            Assert.True( account.Name.Equals(expects[1]), "Name" );
            Assert.True( account.Mail.Equals(expects[2]), "Mail" );
            Assert.True( account.Info.Equals(expects[3]), "Info" );
        }

        [Fact]
        private void UserService_Assert_AccountCreation()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService","EmptyDataBase");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            Status result = service.CreateNewAccount("ElNamo", "El@Mailo.de", "ElInfo").GetAwaiter().GetResult().Status;
            PasswordUsers account = service.Entity;
            
            // Assert
            Assert.False(result, result);
            Assert.True(account.IsValid(), service.Entity.ToString());
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));

            // Close Session
            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_Assert_AccountListing()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>( Test.CurrentContext );
            
            // Act
            List<PasswordUsers> result = service.ListUserAccounts();

            // Assert
            Assert.True( (result != null), "result not null" );
            Assert.True( result.Count == 1, result.Count.ToString() );
            AssertUserAccount( result[0], (1,"ElNamo","El@Mailo.de","ElInfo") );

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_Assert_GetAccountById()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserById(1).GetAwaiter().GetResult().Entity;

            // Assert
            Assert.True(account,account.ToString());
            AssertUserAccount( account, (1, "ElNamo", "El@Mailo.de", "ElInfo") );

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_Asssert_GetAccountByName()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByNameOrId("ElNamo").GetAwaiter().GetResult().Entity;

            // Assert
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_Assert_GetAccountByEmail()
        {
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByEmail("El@Mailo.de").GetAwaiter().GetResult().Entity;

            // Assert
            AssertUserAccount(account, (1, "ElNamo", "El@Mailo.de", "ElInfo"));

            Test.CurrentContext.Finished();
        }


        [Fact]
        private void UserService_ErrorOn_GetAccountByInvalidId()
        {
            // Arrange
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserById(23).GetAwaiter().GetResult().Entity;

            // Assert
            Test.AssertInvalidCase( account );

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_ErrorOn_GetAccountByInvalidName()
        {
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByNameOrId("Egon").GetAwaiter().GetResult().Entity;

            // Assert
            Assert.False( service.Status, service.Status.ToString() );
            Test.AssertInvalidCase( account );

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void UserService_ErrorOn_GetAccountByInvalidEmail()
        {
            Test.Context.PrepareDataBase("UserService","OneUserNoPassword");
            PasswordUsersService<Test.Context> service = new PasswordUsersService<Test.Context>(Test.CurrentContext);

            // Act
            PasswordUsers account = service.GetUserByEmail("El@Peilo.de").GetAwaiter().GetResult().Entity;

            // Assert
            Assert.False( service, service.ToString() );
            Test.AssertInvalidCase( account );

            Test.CurrentContext.Finished();
        }
    }

    public class UserPasswordsServiceTests
    {
        [Fact]
        private void Assert_CreatedUserAcount_Has_MasterPassword()
        {
            // Arrange
            Test.Context.PrepareDataBase("PasswordsService","OneUserNoPassword");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> service = new UserPasswordsService<Test.Context>(Test.CurrentContext,usinger);

            // Act
            Status result = service.SetMasterKey(1,"ElPasso").GetAwaiter().GetResult().Status;
            UserPasswords usrkey = service.Entity;

            // Assert
            Assert.True(result.Text.Length>0, result);

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void Assert_PasswordVerification()
        {
            // Arrange
            Test.Context.PrepareDataBase("PasswordsService","OneValidUserAccount");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> service = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger);

            bool valid = service.VerifyPassword(1, "ElPasso");

            Assert.True( valid , service.Status );

            Test.CurrentContext.Finished();
        }

        [Fact]
        private void Assert_GetMasterKey()
        {
            // Arrange
            Test.Context.PrepareDataBase("PasswordsService","OneValidUserAccount");
            PasswordUsersService<Test.Context> usinger = new PasswordUsersService<Test.Context>(Test.CurrentContext);
            UserPasswordsService<Test.Context> service = new UserPasswordsService<Test.Context>(Test.CurrentContext, usinger);

            Crypt.Key key = service.ForUserAccount( usinger.GetUserByNameOrId("ElNamo") ).GetAwaiter().GetResult().GetMasterKey(1);

            Assert.True( service, service.Status.ToString() );
            Assert.True( key.IsValid(), service.Status );
            Assert.True( key.VerifyPhrase("ElPasso") );

            Test.CurrentContext.Finished();
        }
    }
}
