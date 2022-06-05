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
        private PasswordsDbContext                         _db;
        private PasswordUsersService<PasswordsDbContext> _usrs;
        private UserPasswordsService<PasswordsDbContext> _keys;
        private UserLocationsService<PasswordsDbContext> _locs;
        private Status                                  _error;

        public PasswordsController( ILogger<PasswordsController> logger, PasswordsDbContext db, 
                                    IPasswordsApiService<PasswordUsers, PasswordUsersService<PasswordsDbContext>, PasswordsDbContext> usrs,
                                    IPasswordsApiService<UserPasswords, UserPasswordsService<PasswordsDbContext>, PasswordsDbContext> keys,
                                    IPasswordsApiService<UserLocations, UserLocationsService<PasswordsDbContext>, PasswordsDbContext> locs ) {
            _logger = logger;
            _db = db;
            _usrs = usrs.serve();
            _locs = locs.serve();
            _keys = keys.serve();
            _error = Status.Unknown;
        }


        [Produces("application/json"), HttpPut("User")]
        public async Task<IActionResult> NewUser(string app_user_email_pass)
        {
            Status yps = _keys.DecryptParameter( app_user_email_pass );
            if( yps.Bad ) return BadRequest( yps.ToString() );
            string[] plain = (string[])yps.Data;
            PasswordUsers user = (await _usrs.CreateNewAccount( plain[0], plain[1],
                                              plain.Length > 3 ? plain[3] : string.Empty)
                                   ).Entity;
            if ( user.IsValid() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if ( await _keys.SetMasterKey( user.Id, plain[2] ) ) {
                    return Ok(SerializeAsXaml(user));
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

        [Produces("application/json"), HttpPut("{user}/Info")]
        public async Task<IActionResult> PutUserInfo(string user, string yps_info)
        {
            if( (await _usrs.GetUserByNameOrId(user)).Entity.IsValid() ) {
                PasswordUsers usr = _usrs.Entity;
                Status yps = DecryptQueryParameter(usr.Id, yps_info);
                if (yps.Bad) return BadRequest( yps.ToString() );
                usr.Info = yps.Data.ToString();
                _db.Update(usr);
                _db.SaveChanges();
                return Ok( XamlView.Serialize( usr ) );
            } else return StatusCode( 400, _usrs.Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/Mail")]
        public async Task<IActionResult> PutUserMail(string user, string yps_mail)
        {
            if ((await _usrs.GetUserByNameOrId(user)).Entity.IsValid())
            {
                PasswordUsers usr = _usrs.Entity;
                Status yps = DecryptQueryParameter(usr.Id, yps_mail);
                if (yps.Bad) return BadRequest( yps.ToString() );
                usr.Mail = yps.Data.ToString();
                _db.Update(usr);
                _db.SaveChanges();
                return Ok( XamlView.Serialize(usr) );
            }
            else return StatusCode(400, _usrs.Status.ToString());
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

        [Produces("application/json"), HttpPut("{user}/Pass")]
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

        [Produces("application/json"), HttpDelete("{user}")]
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



        [Produces("application/json"), HttpPut("{user}/Area")]
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
                    return Ok( XamlView.Serialize( _locs.Entity ) );
                } else {
                    return StatusCode(404, _locs.Status.ToString());
                }
            } else return StatusCode(404, _usrs.Status.ToString());
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
            if (location.IsValid()) return Ok( XamlView.Serialize( location ) );
            else return StatusCode(400, location.Is().Status.ToString());
        }

        [Produces("application/json"), HttpDelete("{user}/{area}")]
        public async Task<IActionResult> RemoveLocation( string user, string area, string yps_upass_apass )
        {
            Status yps = DecryptQueryParameter(_usrs.GetUserId(user), yps_upass_apass );
            if (yps.Bad) return StatusCode( 304, yps.ToString() );
            string[] args = (string[])yps.Data;
            if (args.Length < 2) return BadRequest("Expected masterpass and location password");
            if (await _locs.SetKey(_keys.LookupPasswordByUserAccount(_usrs.GetUserById(_usrs.Entity.Id))))
               if (args[1] != _locs.GetPassword()) return BadRequest("Wrong location password");
            if (await _locs.RemoveLocation(_usrs.GetUserByNameOrId(user), area, args[0]))
                return Ok($"Successfully removed password for: {area}");
            else return StatusCode(500, _locs.Status.ToString());
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Name" )]
        public async Task<IActionResult> PutUserLocationLogin( string user, string area, string yps_name )
        {
            if( !_locs.GetLocationOfUser( _usrs.GetUserId(user), area) ) {
                return StatusCode( 400, _locs.Status.ToString() );
            } else {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_name);
                if (yps.Bad) return BadRequest( yps.ToString() );
                yps_name = ((string[])yps.Data)[0];
                _locs.Entity.Name = yps_name;
                if ( _locs.Update() ) return Ok( XamlView.Serialize(_locs.Entity) ); 
            } return StatusCode( 500, _locs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public async Task<IActionResult> PutUserLocationInfo( string user, string area, string yps_info )
        {
            UserLocations location = (await _locs.GetLocationById(_locs.GetAreaId(area, _usrs.GetUserId(user)))).Entity;
            if ( !location.Is().Status.Bad ) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, yps_info );
                if( yps.Bad ) return BadRequest( yps.ToString() );
                location.Info = ( (string[])yps.Data )[0];
                _db.UserLocations.Update( location );
                _db.SaveChanges();
                return Ok( XamlView.Serialize( location ) );
            } else return StatusCode( 404, location.Is().Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/{area}/Pass")]
        public async Task<IActionResult> PutUserLocationPassword( string user, string area, string yps_upass_apass )
        {
            if (_locs.GetLocationOfUser( _usrs.GetUserId(user), area ) ) {
                Status yps = DecryptQueryParameter( _usrs.Entity.Id, yps_upass_apass);
                if ( yps.Bad ) return StatusCode( 304, yps.ToString() );
                string[] args = (string[])yps.Data;
                if( args.Length < 2 )
                    return BadRequest("expected parameter: usersMasterPass and newLocationPass");
                if ( !(await _locs.SetPassword( args[0], args[1] )).Status.Bad ) {
                    return Ok(_locs.Status.ToString());
                }
            } return StatusCode( 404, _locs.Status.ToString() );
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

        private string SerializeAsXaml(object oh)
        {
            System.Reflection.PropertyInfo[] props = oh.GetType().GetProperties();
            System.Text.StringBuilder docum = new System.Text.StringBuilder();
            docum.Append(String.Format("<GroupBox Header='{0}' Orientation='Vertical'>\n", oh.GetType().Name ) );
            for (int i = 0; i < props.Length; ++i) {
                docum.Append("<Rectangle Orientation='Horizontal'>\n");
                docum.Append(String.Format("<Label Content='{0}' HorizontalAlignment='Left' ToolTip='{1}' />\n", props[i].Name, props[i].PropertyType.Name));
                docum.Append(String.Format("<TextBox Text='{0}' HorizontalAlignment='Right' />\n", props[i].GetMethod.Invoke(oh, Array.Empty<object>()) ) );
                docum.Append("</Rectangle>\n");
            } docum.Append("</GroupBox>\n");
            return docum.ToString();
        }

        [Produces("application/json"), HttpGet("{user}/{area}/Pass")]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string yps )
        {
            int userId = _usrs.GetUserId(user);
            if (userId <= 0) return StatusCode( 404, _usrs.Status.ToString() );
            if (!_locs.GetLocationOfUser(userId, area))
                return StatusCode( 404, _locs.Status.ToString());
            Status masterPass = DecryptQueryParameter( userId, yps );
            if (masterPass.Bad) return StatusCode(500, masterPass);
            string pass = _locs.GetPassword( ((string[])masterPass.Data)[0] );
            if (_locs.Status.Bad) {
                return StatusCode( 303, _locs.Status.ToString() );
            } return Ok( pass );
        }

        [Produces("application/json"), HttpGet("errortext/{code}")]
        public async Task<IActionResult> GetErrorText( uint code )
        {
            Status textFromErrorCode = new Status((ResultCode) code, "message from error code: {0}", (ResultCode) code);
            if (textFromErrorCode.Ok) return Ok( textFromErrorCode.ToString() );
            if (textFromErrorCode.Bad) return StatusCode( 404, textFromErrorCode.ToString() );
            else return StatusCode( 500, textFromErrorCode.ToString() ); 
        }

        [Produces("application/json"), HttpGet("ypserror/{code}")]
        public async Task<IActionResult> GetYpsingError( uint code )
        {
            Status textFromErrorCode = new Status( ResultCode.Cryptic, "message from yps crypt: {0}", Yps.Error.GetText( (int)code ) );
            return Ok( textFromErrorCode.ToString() );
        }
    }
}
