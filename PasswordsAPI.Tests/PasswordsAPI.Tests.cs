using System;
using System.IO;
using System.Diagnostics;
using Consola;
using Xunit;

namespace Passwords.API.Tests
{
    public class ExternalLibraryTestRuns
    {
        private static Tuple<int, string> executeLibraryTest(string path,string dllname)
        {
            ProcessStartInfo startinfo = new ProcessStartInfo(
                "C:\\Program Files\\dotnet\\dotnet.exe",
                   $"{path}\\{dllname}.dll --verbose" );

            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardError = true;
            startinfo.WorkingDirectory = path;
            Process testrun = new Process();
            testrun.StartInfo = startinfo;
            testrun.EnableRaisingEvents = true;
            bool ok = false;
            System.Text.StringBuilder testoutput = new System.Text.StringBuilder();
            if (testrun.Start()) {
                testrun.WaitForExit(3000);
                ok = true;
            } else {
                testoutput.Append("Unknown Status running test");
            }

            if (ok) {
                FileInfo outfile =
                    new FileInfo($"{path}\\dotnet_Err.log");
                if (outfile.Exists) {
                    StreamReader f = outfile.OpenText();
                    testoutput.Append(f.ReadToEnd()).Append("\n");
                    f.Close();
                    outfile.Delete();
                    outfile = new FileInfo(
                        $"{path}\\dotnet_Out.log");
                    f = outfile.OpenText();
                    testoutput.Append(f.ReadToEnd());
                    f.Close();
                    outfile.Delete();
                } else {
                    testoutput.Append("Unknown Status reading output");
                }
            }
            Tuple<int, string> result = new Tuple<int,string>(testrun.ExitCode, testoutput.ToString());
            testrun.Close();
            return result;
        }

        [Fact]
        public void RunInt24TypesTest()
        {
            Tuple<int, string> result = executeLibraryTest("C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\core5\\test\\x64\\Debug\\net5.0", "test_int24_dotnet_dll");
            Assert.True( result.Item1 == 0, result.Item2 );
        }

        [Fact]
        public void RunYpsCryptTests()
        {
            Tuple<int, string> result = executeLibraryTest("C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI\\bin\\x64\\Debug\\net5.0", "YpsCryps");
            Assert.True( result.Item1 == 0, result.Item2 );
        }
    }
}
