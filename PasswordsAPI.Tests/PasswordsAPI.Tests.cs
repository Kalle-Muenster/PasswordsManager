using System;
using System.IO;
using System.Diagnostics;
using Consola;
using Xunit;

namespace Passwords.API.Tests
{
    public class BuildConfig
    {
        public readonly string Architecture;
        public readonly string Configuration;
        public BuildConfig()
        {
            Consola.StdStream.Init();
            Architecture = Consola.Utility.MachineArch().Contains("64bit")
                         ? "x64" : "x86";
#if DEBUG
            Configuration = "Debug";
#else
            Configuration = "Release";
#endif
        }
    }

    public class ExternalLibraryTestRuns
    {

        private BuildConfig _buildConfig;
        private BuildConfig BuildConfig
        {
            get { return _buildConfig ?? (_buildConfig = new BuildConfig()); }
        }

        private static int resultcode = 0;
        private static bool resultOk;

        private static void ReceiveResult(int exitCode)
        {
            resultcode = exitCode;
            resultOk = true;
        }

        private static Tuple<int, string> executeLibraryTest(string path, string file)
        {
            ProcessStartInfo startinfo;
            string command; 
            bool ConsolaTest;
            if (file.EndsWith(".exe")) {
                ConsolaTest = false;
                startinfo = new ProcessStartInfo(command=$"{path}\\{file}");
            } else if (file.EndsWith(".dll")) {
                ConsolaTest = true;
                command = $"\"C:\\Program Files\\dotnet\\dotnet.exe\" \"{path}\\{file}\" --verbose";
                startinfo = new ProcessStartInfo(
                    "C:\\Program Files\\dotnet\\dotnet.exe",
                    $"{path}\\{file} --verbose" );
            } else 
                return new Tuple<int,string>( int.MaxValue, file+" is not executable" );

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
                        outfile = new FileInfo($"{path}\\dotnet_Out.log");
                        f = outfile.OpenText();
                        testoutput.Append(f.ReadToEnd());
                        f.Close();
                        outfile.Delete();
                    } else {
                        testoutput.Append("Unknown Status reading output");
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
            }
            Tuple<int, string> result = new Tuple<int,string>(testrun.ExitCode, testoutput.ToString());
            testrun.Close();
            return result;
        }

        [Fact]
        public void RunInt24NativeTypesTest()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\native\\" +
                        $"{BuildConfig.Architecture}\\{BuildConfig.Configuration}";

            Tuple<int, string> result = executeLibraryTest( path, "test_int24_native_cpp.exe" );

            Assert.True(result.Item1 == 0, result.Item1.ToString() + "failures... " + result.Item2);
        }

        [Fact]
        public void RunInt24DotnetTypesTest()
        {
            string path = "C:\\WORKSPACE\\PROJECTS\\Int24Types\\bin\\core5\\test\\" +
                $"{BuildConfig.Architecture}\\{BuildConfig.Configuration}\\net5.0";

            Tuple<int,string> result = executeLibraryTest( path, "test_int24_dotnet_dll.dll" );

            Assert.True(result.Item1 == 0, result.Item2);
        }

        [Fact]
        public void RunYpsCryptTests()
        {
            string path = $"C:\\WORKSPACE\\PROJECTS\\YpsCrypt\\bin\\tst" +
                $"\\{BuildConfig.Architecture}\\{BuildConfig.Configuration}\\net5.0";

            Tuple<int, string> result = executeLibraryTest( path, "YpsCryps.dll" );

            Assert.True( result.Item1 == 0, result.Item2 );
        }
    }
}
