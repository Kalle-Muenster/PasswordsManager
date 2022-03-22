using System;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;
using Yps;

namespace PasswordsAPI.Controllers
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

        [HttpGet("Tests/Int24TypeLib")]
        public IActionResult TestInt24TypeLib()
        {
            Tuple<int,string> result = executableTest("test_int24_dotnet_dll");
            string resp = "{\"Int24Tests\":{\"result\":\"" + (result.Item1 == 0 ? "PASS" : "FAIL") + "\",\"failure\":\"" + result.Item1 +
                          "\",\"outputstring:\":\"" + result.Item2 + "\"}}";
            return Ok(resp);
        }


        [HttpGet("Tests/YpsCryptLib")]
        public IActionResult CryptLibTest()
        {
            Tuple<int,string> result = executableTest("YpsTests"); 
            string resp = "{\"YpsCryptTest\":{\"result\":\"" + (result.Item1 == 0 ? "PASS":"FAIL") + "\",\"failure\":\"" + result.Item1.ToString() +
                          "\",\"outputstring:\":\"" + result.Item2 + "\"}}";
            return Ok(resp);
        }

        [HttpGet( "Tests/Crypting" )]
        public IActionResult GetSchmett()
        {
            StringBuilder result = new StringBuilder("Testing Yps.Crypt\nTest Data: This is test data which consists from a System.String which contains 90 characters of text\n");

            Crypt.Key key = Crypt.CreateKey("YpsCryptTest");
            string cryptisch = Crypt.Encrypt(key,System.Text.Encoding.Default.GetBytes("This is test data which consists from a System.String which contains 90 characters of text"));
            result.Append( "Encrypred: " );
            result.Append( cryptisch + "\n" );
            sbyte[] resultdata = Crypt.Decrypt<sbyte>(key, cryptisch);
            if ( Crypt.Error )
            {
                result.Append( "Status: " );
                cryptisch = Crypt.Error.ToString();
            }
            else unsafe
            {
                result.Append( "Decrypted: " );
                fixed ( sbyte* p = &resultdata[0] )
                {
                    result.Append( new string( p, 0, resultdata.Length ) );
                }
            }
            return Ok( result.ToString() );

        }
    }
}
