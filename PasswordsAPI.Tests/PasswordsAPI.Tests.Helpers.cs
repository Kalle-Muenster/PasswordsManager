using System;
using System.IO;
using Passwords.API.Abstracts;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Xunit;
using Constringer = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Connectione = Microsoft.Data.Sqlite.SqliteConnection;
using Passwords.API.Models;

namespace Passwords.API.Tests.Helpers
{
    public class BuildConfig
    {
        public readonly string Architecture;
        public readonly string Configuration;
        public readonly string ProjectFolder;

        public BuildConfig()
        {
            ProjectFolder = "C:\\WORKSPACE\\PROJECTS\\PasswordsAPI\\PasswordsAPI";
            Architecture = Consola.Utility.ArchitectureOfChip().Contains("64bit") ? "x64" : "x86";
#if DEBUG
            Configuration = "Debug";
#else
            Configuration = "Release";
#endif
        }
    }

    public class Model
    {
        public static void AssertInvalidCase<M>(M modelclass) where M : EntityBase<M>, new()
        {
            Assert.False(modelclass, modelclass.ToString());
            Assert.False(modelclass.IsValid(), modelclass.ToString());
            Assert.False(modelclass.Waiting(), modelclass.ToString());
            Assert.False(modelclass.Is().Status.Ok, modelclass.ToString());
            Assert.True(modelclass.Is().Status.Bad, modelclass.ToString());
        }

        public static void AssertSuccessCase<M>(M modelclass) where M : EntityBase<M>, new()
        {
            Assert.True(modelclass, modelclass.ToString());
            Assert.True(modelclass.IsValid(), modelclass.ToString());
            Assert.False(modelclass.Waiting(), modelclass.ToString());
            Assert.False(modelclass.Is().Status.Bad, modelclass.ToString());
        }
    }

    public class ExternalTestrun
    {
        private Tuple<int,string> _result;
        public int Failures
        {
            get { return _result.Item1; }
        }

        public string Results
        {
            get { return _result.Item2; }
        }

        private static string getConsolaLog( string log )
        {
            FileInfo file = new FileInfo( log );
            if( file.Exists ) {
                StreamReader stream = file.OpenText();
                log = stream.ReadToEnd();
                stream.Close();
                file.Delete();
                return log;
            } else return $"file: {log} doesn't exist";
        }

        public ExternalTestrun( string path, string file )
        {
            ProcessStartInfo startinfo = null;
            bool ConsolaTest = false;
            if( file.EndsWith(".exe") ) {
                startinfo = new ProcessStartInfo( $"{path}\\{file}" );
            } else if ( file.EndsWith(".dll") ) {
                ConsolaTest = true;
                string dotnet = Test.CurrentConfig.Architecture == "x86"
                              ? "C:\\Program Files (x86)\\dotnet\\dotnet.exe"
                              : "C:\\Program Files\\dotnet\\dotnet.exe";
                startinfo = new ProcessStartInfo( dotnet, $"{path}\\{file} -v -x" );
            } else
                _result = new Tuple<int,string>( int.MaxValue, file+" is not executable" );

            if( startinfo != null ) {
                startinfo.RedirectStandardOutput = true;
                startinfo.RedirectStandardError = true;
                startinfo.WorkingDirectory = path;
                Process testrun = new Process();
                testrun.StartInfo = startinfo;
                testrun.EnableRaisingEvents = true;
                System.Text.StringBuilder testout = new System.Text.StringBuilder();

                bool ok = false;
                if( ConsolaTest ) {
                    if( testrun.Start() ) {
                        ok = testrun.WaitForExit( 30000 );
                    } else {
                        testout.Append( "Unknown Status running test" );
                    }
                    if( ok ) {
                        if( testrun.ExitCode != 0 )
                        testout.Append(getConsolaLog( $"{path}\\dotnet_Err.log") ).Append("\n");
                        testout.Append(getConsolaLog( $"{path}\\dotnet_Out.log") ).Append("\n");
                        testout.Append(getConsolaLog( $"{path}\\dotnet_Aux.log") ).Append("\n");
                    }
                } else {
                    if( testrun.Start() ) {
                        if ( ok = testrun.WaitForExit( 30000 ) ) {
                            testout.Append( testrun.StandardError.ReadToEnd() );
                            testout.Append( testrun.StandardOutput.ReadToEnd() );
                        } else testout.Append("test hanging or crashed... ");
                    } else {
                        testout.Append("Unknown status reading output");
                    }
                } _result = new Tuple<int,string>( testrun.ExitCode, testout.ToString() );
                testrun.Close();
            }
        }
    }

    public class Test
    {
        public static Context               CurrentContext;
        public static readonly BuildConfig  CurrentConfig = new BuildConfig();
        public static readonly Yps.CryptKey CurrentApikey = Yps.Crypt.CreateKey("cb2df853b8194e4ff64af32f9131a6318b1e9d5c966fcd7cb5398470ed71e270");
        public class SessionKey
        {
            private static volatile bool IsRunning;
            private static Random KeyGen;
            private int Session;

            static SessionKey()
            {
                KeyGen = new Random( (int)DateTime.Now.Ticks );
                IsRunning = false;
            }

            public SessionKey()
            {
                IsRunning = LockSession();
            }

            public override int GetHashCode()
            {
                return Session;
            }

            public void Unlock(SessionKey key)
            {
                if( Session == key.Session )
                    IsRunning = false;
            }

            public bool LockSession()
            {
                bool state = false;
                lock (Thread.CurrentThread) {
                    while (IsRunning) Thread.Sleep(100);
                    Session = KeyGen.Next(int.MaxValue);
                    state = !IsRunning;
                    if (state) IsRunning = true;
                } return state;
            }
        }

        public class Context : PasswordsApiDbContext<Context>
        {
            public string      TestCase;
            public string      DataBase { get; protected set; }
            private SessionKey Key;

            public static DbContextOptions<Context> CreateTestOptions( string database )
            {
                lock ( Thread.CurrentThread )
                {
                    Thread.Sleep(2000);
                    Consola.StdStream.Cwd = CurrentConfig.ProjectFolder + "\\DataBase\\Tests";
                    Consola.Utility.CommandLine("C:\\Windows\\System32\\cmd.exe /c \"del /f /q db.db\"");
                    Consola.Utility.CommandLine(
                        $"C:\\Windows\\System32\\cmd.exe /c \"copy {CurrentConfig.ProjectFolder}\\DataBase\\Tests\\{database}\\db.db {Consola.StdStream.Cwd}\""
                    );
                    return new DbContextOptionsBuilder<Context>().UseSqlite(
                        new Connectione(
                            new Constringer( string.Format( "Data Source={0}",
                                new FileInfo( $"{CurrentConfig.ProjectFolder}\\DataBase\\Tests\\db.db" ).FullName )
                            ).ConnectionString
                        )
                    ).Options;
                }
            }

            public static void PrepareDataBase( string testcase, string database )
            {
                if ( CurrentContext != null ) {
                    if ( CurrentContext.DataBase.Equals( database ) ) {
                        CurrentContext.Begin( testcase );
                        return;
                    }
                }
                SessionKey key = new SessionKey();
                lock ( key ) {
                    CurrentContext = new Context( database, testcase, key );
                }
            }

            public Context Begin( string testcase ) {
                if ( Key.LockSession() )  {
                    TestCase = testcase;
                } return this;
            }
            public void Finished() {
                Key.Unlock( Key );
            }

            public Context( string database, string testcase, SessionKey key )
                : base( Context.CreateTestOptions( database ) )
            {
                Key = key;
                TestCase = testcase;
                DataBase = database;
            }

            /////////////////////////////////////////////////////////////////
            /// this should equal the regularly used PasswordsDbContext

            public DbSet<PasswordUsers> PasswordUsers
            {
                get { return EntitiesSet<PasswordUsers>(); }
                set { SetEntities(value); }
            }

            public DbSet<UserPasswords> UserPasswords
            {
                get { return EntitiesSet<UserPasswords>(); }
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
