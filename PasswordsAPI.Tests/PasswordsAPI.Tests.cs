using System;
using Xunit;
using Passwords.API.Tests.Helpers;

namespace Passwords.API.Tests
{
        
    public class ExternalLibraries { 

        [Fact]
        public void RunInt24TypesTest()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\core5\\" +
                $"{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun( path, "Int24Tests.dll" );

            Assert.True( test.FailedTests == 0, test.TestResults + "\n" + $"Failed Tests: {test.FailedTests}");
        }

        [Fact]
        public void RunYpsCryptTests()
        {
            string path = $"C:\\WORKSPACE\\PROJECTS\\YpsCrypt\\bin\\tst" +
                $"\\{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun( path, "YpsTest.dll" );

            Assert.True( test.FailedTests == 0, test.TestResults + "\n" + $"Failed Tests: {test.FailedTests}");
        }
    }
}
