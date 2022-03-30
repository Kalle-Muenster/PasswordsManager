using System;
using Xunit;
using PasswordsAPI.Tests.Helpers;

namespace Passwords.API.Tests
{
        
    public class ExternalLibraries { 

        [Fact]
        public void RunInt24NativeTypesTest()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\native\\" +
                        $"{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}";

            ExternalTestrun testrun = new ExternalTestrun( path, "test_int24_native_cpp.exe" );

            Assert.True( testrun.FailedTests == 0, testrun.TestResults );
        }

        [Fact]
        public void RunInt24DotnetTypesTest()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\core5\\test\\" +
                $"{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun( path, "test_int24_dotnet_dll.dll" );

            Assert.True( test.FailedTests == 0, test.TestResults );
        }

        [Fact]
        public void RunYpsCryptTests()
        {
            string path = $"C:\\WORKSPACE\\PROJECTS\\YpsCrypt\\bin\\tst" +
                $"\\{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun( path, "YpsTest.dll" );

            Assert.True( test.FailedTests == 0, test.TestResults );
        }
    }
}
