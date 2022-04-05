using System;
using System.IO;
using PasswordsAPI.Abstracts;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Xunit;
using Constringer = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Connectione = Microsoft.Data.Sqlite.SqliteConnection;
using PasswordsAPI.Models;

namespace PasswordsAPI.Tests.Helpers
{
    public class BuildConfig
    {
        public readonly string Architecture;
        public readonly string Configuration;
        public readonly string ProjectFolder;

        public BuildConfig()
        {
            ProjectFolder = "C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI";
            Architecture = Consola.Utility.MachineArch().Contains("64bit") ? "x64" : "x86";
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
        public int FailedTests
        {
            get { return _result.Item1; }
        }

        public string TestResults
        {
            get { return _result.Item2; }
        }

        public ExternalTestrun( string path, string file )
        {
            ProcessStartInfo startinfo = null;
            bool ConsolaTest = false;
            if (file.EndsWith(".exe")) {
                startinfo = new ProcessStartInfo( $"{path}\\{file}" );
            } else if ( file.EndsWith(".dll") ) {
                ConsolaTest = true;
                string dotnet = Test.CurrentConfig.Architecture == "x86"
                              ? "C:\\Program Files (x86)\\dotnet\\dotnet.exe"
                              : "C:\\Program Files\\dotnet\\dotnet.exe";
                startinfo = new ProcessStartInfo( dotnet, $"{path}\\{file} --verbose" );
            } else
                _result = new Tuple<int,string>( int.MaxValue, file+" is not executable" );

            if (startinfo != null) {
                startinfo.RedirectStandardOutput = true;
                startinfo.RedirectStandardError = true;
                startinfo.WorkingDirectory = path;
                Process testrun = new Process();
                testrun.StartInfo = startinfo;
                testrun.EnableRaisingEvents = true;
                bool ok = false;
                System.Text.StringBuilder testoutput = new System.Text.StringBuilder();

                if (ConsolaTest) {
                    if (testrun.Start()) {
                        ok = testrun.WaitForExit(300000);
                    } else {
                        testoutput.Append( "Unknown Status running test" );
                    }
                    if (ok) {
                        FileInfo outfile = new FileInfo( $"{path}\\dotnet_Err.log" );
                        if (outfile.Exists) {
                            StreamReader f = outfile.OpenText();
                            testoutput.Append( f.ReadToEnd() ).Append( "\n" );
                            f.Close();
                            outfile.Delete();
                            outfile = new FileInfo( $"{path}\\dotnet_Out.log" );
                            f = outfile.OpenText();
                            testoutput.Append( f.ReadToEnd() );
                            f.Close();
                            outfile.Delete();
                        } else {
                            testoutput.Append( "Unknown Status reading output" );
                        }
                    }
                } else {
                    if (testrun.Start()) {
                        if (ok = testrun.WaitForExit(30000)) {
                            testoutput.Append(testrun.StandardError.ReadToEnd());
                            testoutput.Append(testrun.StandardOutput.ReadToEnd());
                        } else testoutput.Append("test hanging crashed... ");
                    } else {
                        testoutput.Append("Unknown status reading output");
                    }
                } _result = new Tuple<int,string>( testrun.ExitCode,testoutput.ToString() );
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
                    Consola.Utility.CommandExec( "C:\\Windows\\System32\\cmd.exe /c \"del /f /q db.db\"" );
                    Consola.Utility.CommandExec(
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
