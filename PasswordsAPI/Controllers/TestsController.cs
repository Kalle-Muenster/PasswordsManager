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
        private static string path = new FileInfo( 
            System.Reflection.Assembly.GetExecutingAssembly().Location
                                                  ).Directory.FullName;  

        public IActionResult Index()
        {
            return Ok( "Test" );
        }

        [HttpGet("Tests/YpsCrypt")]
        public IActionResult CryptLibTest()
        {
            ExternalTestrun test = new ExternalTestrun( path, "YpsTests.dll" );

            if (test.Failures == 0) return Ok( test.Results );
            else return StatusCode(500 + test.Failures, test.Results);
        }

        [HttpGet("Tests/Int24Types")]
        public IActionResult GetSchmett()
        {

            ExternalTestrun test = new ExternalTestrun( path, "Int24Tests.dll" );

            if (test.Failures == 0) return Ok( test.Results );
            else return StatusCode( 500 + test.Failures, test.Results );
        }
    }
}
