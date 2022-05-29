using System;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;
using Yps;
using Passwords.API.Tests.Helpers;

namespace Passwords.Controllers
{
    public class TestsController : Controller
    {
        private static Tuple<int,string> executableTest(string dllname)
        {
            System.Diagnostics.ProcessStartInfo startinfo = new System.Diagnostics.ProcessStartInfo(
                "C:\\Program Files\\dotnet\\dotnet.exe",
                $"C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI\\bin\\x64\\Debug\\net5.0\\{dllname}.dll --verbose");
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardError = true;
            System.Diagnostics.Process test = new System.Diagnostics.Process();
            test.StartInfo = startinfo;
            test.EnableRaisingEvents = true;
            bool ok = false;
            System.Text.StringBuilder testoutput = new System.Text.StringBuilder();
            if (test.Start())
            {
                test.WaitForExit(3000);
                ok = true;
            }
            else
            {
                testoutput.Append("Unknown Status running test");
            }

            if (ok)
            {
                FileInfo outfile =
                    new FileInfo("C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI\\dotnet_Err.log");
                if (outfile.Exists)
                {
                    StreamReader f = outfile.OpenText();
                    testoutput.Append(f.ReadToEnd()).Append("\n");
                    f.Close();
                    outfile.Delete();
                    outfile = new FileInfo(
                        "C:\\WORKSPACE\\PROJECTS\\PasswordsManager\\PasswordsAPI\\dotnet_Out.log");
                    f = outfile.OpenText();
                    testoutput.Append(f.ReadToEnd());
                    f.Close();
                    outfile.Delete();
                }
                else
                {
                    testoutput.Append("Unknown Status reading output");
                }
            }
            Tuple<int,string> result = new Tuple<int,string>( test.ExitCode, testoutput.ToString() );
            test.Close();
            return result;
        } 

        public IActionResult Index()
        {
            return Ok("Test");
        }

        [HttpGet("Tests/Int24NativeTypes")]
        public IActionResult TestInt24TypeLib()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\native\\" +
                $"{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}";

            ExternalTestrun testrun = new ExternalTestrun(path, "test_int24_native_cpp.exe");

            if (testrun.FailedTests == 0) return Ok(testrun.TestResults);
            else return StatusCode(500 + testrun.FailedTests, testrun.TestResults);
        }


        [HttpGet("Tests/YpsCryptLib")]
        public IActionResult CryptLibTest()
        {
            string path = $"C:\\WORKSPACE\\PROJECTS\\YpsCrypt\\bin\\tst" +
                $"\\{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun(path, "YpsTest.dll");

            if (test.FailedTests == 0) return Ok(test.TestResults);
            else return StatusCode(500 + test.FailedTests, test.TestResults);
        }

        [HttpGet("Tests/Int24DotnetTypes")]
        public IActionResult GetSchmett()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\core5\\test\\" +
                $"{Test.CurrentConfig.Architecture}\\{Test.CurrentConfig.Configuration}\\net5.0";

            ExternalTestrun test = new ExternalTestrun(path, "test_int24_dotnet_dll.dll");

            if (test.FailedTests == 0) return Ok(test.TestResults);
            else return StatusCode(500 + test.FailedTests, test.TestResults);
        }
    }
}
