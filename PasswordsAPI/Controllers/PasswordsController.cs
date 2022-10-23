using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Passwords.API.Abstracts;
using Passwords.API.Services;
using Passwords.API.Database;
using Yps;
using System;
using System.Threading.Tasks;
using Passwords.API.Models;
using Passwords.API.Xaml;

namespace Passwords.Controllers
{

    [ApiController]
    [Route(template: "[controller]")]
    public class PasswordsController : ControllerBase
    {

        private readonly ILogger<PasswordsController>  _logger;
        private readonly System.Diagnostics.EventLog      _log;
        private PasswordsDbContext                         _db;
        private PasswordUsersService<PasswordsDbContext> _usrs;
        private UserPasswordsService<PasswordsDbContext> _keys;
        private UserLocationsService<PasswordsDbContext> _locs;
        private Status                                  _error;

        public PasswordsController( ILogger<PasswordsController> logger, System.Diagnostics.EventLog log, PasswordsDbContext db,
                                    IPasswordsApiService<PasswordUsers, PasswordUsersService<PasswordsDbContext>, PasswordsDbContext> usrs,
                                    IPasswordsApiService<UserPasswords, UserPasswordsService<PasswordsDbContext>, PasswordsDbContext> keys,
                                    IPasswordsApiService<UserLocations, UserLocationsService<PasswordsDbContext>, PasswordsDbContext> locs ) {
            _logger = logger;
            _log = log;
            _db = db;
            _usrs = usrs.serve();
            _locs = locs.serve();
            _keys = keys.serve();
            _error = Status.Unknown;
        }


        [Produces("application/json"), HttpPatch("User")]
        public async Task<IActionResult> NewUser( string app_user_email_pass )
        {
            Status yps = _keys.DecryptParameter( app_user_email_pass );
            if( yps.Bad ) return BadRequest( yps.ToString() );

            string[] plain = (string[])yps.Data;
            PasswordUsers user = (await _usrs.CreateNewAccount( plain[0], plain[1],
                                             plain.Length > 3 ? plain[3] : string.Empty )
                                   ).Entity;
            if ( user.IsValid() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if ( await _keys.SetMasterKey( user.Id, plain[2] ) ) {
                    return Ok( SerializeAsXaml(user) );
                } else {
                    _db.PasswordUsers.Remove( user );
                    _db.SaveChanges();
                    return StatusCode( 500, _keys.Status.ToString() );
                }
            } else return StatusCode( 500, _usrs.Status.ToString() );
        }

        [Produces("application/json"), HttpGet("User")]
        public IActionResult GetUser()
        {
            return new OkObjectResult( _usrs.ListUserAccounts() );
        }

        [Produces("application/json"), HttpPatch("{user}/Info")]
        public async Task<IActionResult> PatchUserInfo(string user, string yps_info)
        {
            if( (await _usrs.GetUserByNameOrId(user)).Entity.IsValid() ) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_info);
                if (yps.Bad) return BadRequest( yps.ToString() );
                _usrs.Entity.Info = yps.Data.ToString();
                _usrs.Save();
                return Ok( XamlView.SerializeGrid(_usrs.Entity ) );
            } else return StatusCode( 400, _usrs.Status.ToString() );
        }

        [Produces("application/json"), HttpPatch("{user}/Mail")]
        public async Task<IActionResult> PatchUserMail(string user, string yps_mail)
        {
            if ( (await _usrs.GetUserByNameOrId(user)).Entity.IsValid() ) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_mail);
                if( yps.Bad ) {
                    _log.WriteEntry( yps.Text, yps.Event, (int)yps.Code );
                    return BadRequest( yps.ToString() );
                }
                _usrs.Entity.Mail = yps.Data.ToString();
                _usrs.Save();
                return Ok( XamlView.SerializeGrid(_usrs.Entity) );
            } else return StatusCode(400, _usrs.Status.ToString());
        }

        [Produces("application/json"), HttpGet("{user}/Info")]
        public async Task<IActionResult> GetUserInfo(string user)
        {
            if ( (await _usrs.GetUserByNameOrId(user)).Entity.Is().Status.Bad ) {
                return StatusCode( 400, _usrs.Status.ToString() );
            } else {
                return Ok( _usrs.Entity.Info );
            }
        }

        [Produces("application/json"), HttpPatch("{user}/Pass")]
        public async Task<ActionResult> SetUserPassword(string user, string yps_oldpass_newpass)
        {
            if (await _keys.LookupPasswordByUserAccount(_usrs.GetUserByNameOrId(user))) {
                Status yps = _keys.DecryptParameter( yps_oldpass_newpass );
                if (yps.Bad) return BadRequest( yps.ToString() );
                string[] plain = (string[])yps.Data;
                if ( plain.Length < 2 )
                    return BadRequest( "expected parameter: oldPass.~.newPass" );
                if (_keys.VerifyPassword(_usrs.Entity.Id, plain[0]))
                    return new OkObjectResult(
               (await _keys.SetMasterKey(_usrs.Entity.Id, plain[1])).Status.ToString()
                                               );
            } return StatusCode( 500, _keys.Status.ToString() );
        }

        [Produces("application/json"), HttpGet("Delete/{user}")]
        public async Task<IActionResult> RemoveUserAccount(string user, string yps_pass_mail)
        {
            if ((await _usrs.GetUserByNameOrId(user)).Entity) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_pass_mail);
                if ( yps.Bad ) return BadRequest( yps.ToString() );
                string[] args = (string[])yps.Data; 
                if (_usrs.Entity.Mail == args[1]) {
                    if ((await _keys.LookupPasswordByUserAccount(_usrs.GetUserByNameOrId(user))).VerifyPassword(_usrs.Entity.Id, args[0]) ) { 
                        if (await _usrs.RemoveUserAccount(_usrs.Entity)) return Ok( _usrs.Status.ToString() );
                        return StatusCode(500, "Removing user account failed: " + _usrs.Status );
                    } return StatusCode(303, _keys.Status.ToString() );
                } return StatusCode(404, _usrs.Status + "Incorrect Em@il address");
            } return StatusCode(404, _usrs.Status.ToString() );
        }



        [Produces("application/json"), HttpPatch("{user}/Area")]
        public async Task<IActionResult> NewUserLocation( string user, string yps_area_pass_opt_login_info )
        {
            if ((await _usrs.GetUserByNameOrId(user)).Entity) {
                Status yps = DecryptQueryParameter( _usrs.Entity.Id, yps_area_pass_opt_login_info );
                if ( yps.Bad ) return BadRequest( yps.ToString() );
                string[] args = (string[])yps.Data;
                if ( args.Length < 2 ) return BadRequest("expected at least 2 paramerter: locationName.~.locationPass");
                UserLocations newArea = new UserLocations();
                newArea.Area = args[0];
                newArea.User = _usrs.Entity.Id;
                newArea.Name = args.Length > 2 ? args[2] : String.Empty;
                newArea.Info = args.Length > 3 ? args[3] : String.Empty;
                await _locs.SetLocationPassword(_usrs.GetUserByNameOrId(user), newArea, args[1]);
                if (_locs.GetLocationOfUser( _usrs.Entity.Id, args[0] ).IsValid() ) {
                    _log.WriteEntry( _locs.Status.Text, _locs.Status.Event );
                    return Ok( XamlView.SerializeGrid( _locs.Entity ) );
                } else {
                    return StatusCode(_locs.Status.Http,_locs.Status.Text);
                }
            } else return StatusCode(_usrs.Status.Http, _usrs.Status.Text);
        }

        [Produces("application/json"), HttpGet("{user}/Area")]
        public async Task<IActionResult> GetUserLocations( string user ) {
            int usrid = _usrs.GetUserId(user);
            if (usrid > 0) {
                return new OkObjectResult( _locs.GetUserLocations(usrid) );
            } else return StatusCode( 404, _usrs.Status.ToString() );
        }

        [Produces("application/json"), HttpGet("{user}/{area}")]
        public async Task<IActionResult> GetUserLocation( string user, string area )
        {
            UserLocations location = _locs.GetLocationOfUser(_usrs.GetUserId(user), area);
            if (location.IsValid()) return Ok( XamlView.SerializeGrid( location ) );
            else return StatusCode(400, location.Is().Status.ToString());
        }

        [Produces("application/json"), HttpGet("{user}/Delete/{area}")]
        public async Task<IActionResult> RemoveLocation( string user, string area, string yps_upass_apass )
        {
            Status yps = DecryptQueryParameter(_usrs.GetUserId(user), yps_upass_apass );
            if ( yps.Bad ) return StatusCode( 304, yps.ToString() );
            string[] args = (string[])yps.Data;
            if ( args.Length < 2 ) return StatusCode( 400, "Expected masterpass and location password");
            if( _locs.GetLocationOfUser(_usrs.Entity.Id, area).Is().Status.Bad )
                return StatusCode( _locs.Status.Http, _locs.Status.Text );
            if( await _locs.SetKey(_keys.LookupPasswordByUserAccount(_usrs.Entity.Id)) ) {
                if( args[1] != _locs.GetPassword() ) return StatusCode( 400, "Wrong location password" );
            }
            if( ( await _locs.RemoveLocation(_usrs.Entity.Id, area, args[0]) ).Status.Ok ) {
                string success = $"Successfully removed password for: {area}";
                _log.WriteEntry( success, System.Diagnostics.EventLogEntryType.SuccessAudit );
                return Ok($"Successfully removed password for: {area}");
            } else return StatusCode(500, _locs.Status.ToString());
        }

        [Produces(  "application/json" ), HttpPatch( "{user}/{area}/Name" )]
        public async Task<IActionResult> PatchUserLocationLogin( string user, string area, string yps_name )
        {
            if( !_locs.GetLocationOfUser( _usrs.GetUserId(user), area) ) {
                return StatusCode( _locs.Status.Http, _locs.Status.ToString() );
            } else {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_name);
                if (yps.Bad) return BadRequest( yps.ToString() );
                yps_name = ((string[])yps.Data)[0];
                if ( _locs.SetLoginInfo(_locs.Entity.Id, yps_name, null).GetAwaiter().GetResult().Ok )
                    return Ok( XamlView.SerializeGrid(_locs.Entity) ); 
            } return StatusCode(_locs.Status.Http, _locs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPatch( "{user}/{area}/Info" )]
        public async Task<IActionResult> PatchUserLocationInfo( string user, string area, string yps_info )
        {
            if( (await _locs.GetLocationById(_locs.GetAreaId(area, _usrs.GetUserId(user)))).Entity.Is().Status.Ok ) { 
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_info );
                if( yps.Bad ) return BadRequest( yps.ToString() );
                yps_info = ( (string[])yps.Data )[0];
                if( _locs.SetLoginInfo(_locs.Entity.Id, null, yps_info).GetAwaiter().GetResult().Ok )
                    return Ok( XamlView.SerializeGrid(_locs.Entity) );
            } return StatusCode( _locs.Status.Http, _locs.Status.Text );
        }

        [Produces("application/json"), HttpPatch("{user}/{area}/Pass")]
        public async Task<IActionResult> PatchUserLocationPassword( string user, string area, string yps_upass_apass )
        {
            if (_locs.GetLocationOfUser( _usrs.GetUserId(user), area ) ) {
                Status yps = DecryptQueryParameter( _usrs.Entity.Id, yps_upass_apass);
                if ( yps.Bad ) return StatusCode( 304, yps.ToString() );
                string[] args = (string[])yps.Data;
                if( args.Length < 2 )
                    return BadRequest("expected parameter: usersMasterPass and newLocationPass");
                if ( !(await _locs.SetPassword( args[0], args[1] )).Status.Bad ) {
                    return Ok( _locs.Status.Text );
                }
            } return StatusCode( _locs.Status.Http, _locs.Status.Text );
        }

        private Status DecryptQueryParameter( int userId, string encrypted )
        {
            Yps.CryptKey key = _keys.LookupPasswordByUserAccount( _usrs.GetUserById( userId ) )
                                             .GetAwaiter().GetResult().GetMasterKey( userId );
            if( key.IsValid() ) {
                string plaintext = key.Decrypt( encrypted );
                if( plaintext != null ) {
                    return Status.Success.WithData( plaintext.Substring(3).Split(".~.") );
                } return Status.Invalid.WithText(
                    $"{Crypt.Error} - seems to be wrong masterkey"
                ).WithData( Array.Empty<string>() );
            } return _usrs.Status.Ok ? _keys.Status : _usrs.Status;
        }

        private string SerializeAsXaml(object obj)
        {
            return XamlView.SerializeGroup( obj );
        }

        [Produces("application/json"), HttpGet("{user}/{area}/Pass")]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string yps )
        {
            int userId = _usrs.GetUserId(user);
            if (userId <= 0) return StatusCode( 404, _usrs.Status.ToString() );
            if (!_locs.GetLocationOfUser(userId, area))
                return StatusCode( _locs.Status.Http, _locs.Status.Text );
            Status masterPass = DecryptQueryParameter( userId, yps );
            if (masterPass.Bad) return StatusCode(500, masterPass);
            string pass = _locs.GetPassword( ((string[])masterPass.Data)[0] );
            if (_locs.Status.Bad) {
                return StatusCode( _locs.Status.Http, _locs.Status.Text );
            } return Ok( pass );
        }

        [Produces("application/json"), HttpGet("errortext/{code}")]
        public async Task<IActionResult> GetErrorText( uint code )
        {
            Status textFromErrorCode = new Status((ResultCode) code, "message from error code: {0}", (ResultCode) code);
            if (textFromErrorCode.Ok) return Ok( textFromErrorCode.ToString() );
            if (textFromErrorCode.Bad) return StatusCode( textFromErrorCode.Http, textFromErrorCode.Text );
            else return StatusCode( 500, textFromErrorCode.ToString() );
        }

        [Produces("application/json"), HttpGet("ypserror/{code}")]
        public async Task<IActionResult> GetYpsingError( uint code )
        {
            Status textFromErrorCode = new Status( ResultCode.Cryptic, "message from yps crypt: {0}", Yps.Error.GetText( (int)code ) );
            return Ok( textFromErrorCode.ToString() );
        }

        [Produces("application/json"), HttpGet("Export/{user}")]
        public async Task<IActionResult> ExportData( string user, string yps_stamp )
        {
            if( await _keys.LookupPasswordByUserAccount(_usrs.GetUserByNameOrId(user)) ) {
                Status res = _keys.DecryptParameter( yps_stamp );
                if( res.Bad ) return StatusCode( res.Http, res.Text );
                DateTime stamp;
                string[] ypsargs = (string[])res.Data;
                if( DateTime.TryParse( ypsargs[0], out stamp ) ) {
                    res = _keys.GetCrypticDbExport( stamp.ToString() );
                    if( res.Bad ) return StatusCode( res.Http, res.Text );
                    return File( res.Data as System.IO.FileStream, "application/binary" );
                } else return StatusCode( 500, "timestamp expected" );
            }
            if( _usrs.Status.Bad )
                return StatusCode(_usrs.Status.Http, _usrs.Status.Text);
            else
                return StatusCode(_keys.Status.Http, _keys.Status.Text);
        }
    }
}
