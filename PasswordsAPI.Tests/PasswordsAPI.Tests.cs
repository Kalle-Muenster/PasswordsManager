using System;
using Xunit;
using Passwords.API.Tests.Helpers;

namespace Passwords.API.Tests
{
        
    public class ExternalLibraries
    {
        public static readonly string BuildFolder;

        static ExternalLibraries()
        {
            BuildFolder = Test.CurrentConfig.ProjectFolder + "\\bin\\"
                        + Test.CurrentConfig.Architecture + "\\" 
                        + Test.CurrentConfig.Configuration + "\\net5.0";
        }
        
        [Fact]
        public void RunInt24TypesTest()
        {
            ExternalTestrun test = new ExternalTestrun( BuildFolder, "Int24Tests.dll" );
            Assert.True( test.Failures == 0, test.Results + "\n" + $"Failed Tests: {test.Failures}");
        }

        [Fact]
        public void RunYpsCryptTests()
        {
            ExternalTestrun test = new ExternalTestrun( BuildFolder, "YpsTests.dll" );
            Assert.True( test.Failures == 0, test.Results + "\n" + $"Failed Tests: {test.Failures}");
        }
    }
}
