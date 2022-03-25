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
