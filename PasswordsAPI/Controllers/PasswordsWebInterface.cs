<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
=======
﻿using Microsoft.AspNetCore.Mvc;
>>>>>>> refs/remotes/fork/main
using Microsoft.Extensions.Logging;
using Passwords.API.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Html;

namespace Passwords.API.Controllers
{
    [ApiController]
    [Route(template: "[controller]")]
    public class PasswordsWebInterface : ControllerBase
    {
        private string _internalroot;
        public PasswordsWebInterface()
        {
            _internalroot = Consola.StdStream.Cwd;
        }


        [Produces("text/html"), HttpGet("index.html")]
        public async Task<FileStream> Index()
        {
            return new FileInfo($"{_internalroot}\\Resources\\html\\index.html").OpenRead();
        }

        [Produces("text/javascript"), HttpGet("js/textedit.js")]
        public async Task<FileStream> jsTextEdit()
        {
            return new FileInfo( $"{_internalroot}\\Resources\\js\\textedit.js").OpenRead();
        }
    }
}
