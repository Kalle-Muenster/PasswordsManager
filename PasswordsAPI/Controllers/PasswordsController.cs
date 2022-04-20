using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PasswordsAPI.Abstracts;
using PasswordsAPI.Services;
using PasswordsAPI.Database;

using System;
using System.Threading.Tasks;
using PasswordsAPI.Models;

namespace PasswordsAPI.Controllers
{

    [ApiController]
    [Route(template: "[controller]")]
    public class PasswordsController : ControllerBase
    {

        private readonly ILogger<PasswordsController> _logger;
        private PasswordsDbContext   _db;
        private PasswordUsersService<PasswordsDbContext> _usrs;
        private UserPasswordsService<PasswordsDbContext> _keys;
        private UserLocationsService<PasswordsDbContext> _locs;
        private Status                                  _error;

        public PasswordsController( ILogger<PasswordsController> logger, PasswordsDbContext  db, 
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


        [Produces("application/json"), HttpPost("User")]
        public async Task<IActionResult> NewUser(string name, string email, string pass)
        {
            PasswordUsers user = (await _usrs.CreateNewAccount(name, email, "")).Entity;
            if ( user.IsValid() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if ( await _keys.SetMasterKey( user.Id, pass ) ) {
                    return Ok( user );
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
        public async Task<IActionResult> PutUserInfo(string user, string ypsInfo)
        {
            if( (await _usrs.GetUserByNameOrId(user)).Entity.IsValid() ) {
                PasswordUsers usr = _usrs.Entity;
                Status yps = DecryptQueryParameter(usr.Id, ypsInfo);
                if (yps.Bad) return BadRequest( yps );
                usr.Info = yps.Data.ToString();
                _db.Update(usr);
                _db.SaveChanges();
                return new OkObjectResult( usr );
            } else return StatusCode( 400, _usrs.Status.ToString() );
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
        public async Task<ActionResult> SetUserPassword(string user, string ypsOldPassVsNewPass)
        {
            if (await _keys.LookupPasswordByUserAccount(_usrs.GetUserByNameOrId(user))) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, ypsOldPassVsNewPass);
                if (yps.Bad) return BadRequest( yps );
                ypsOldPassVsNewPass = yps.Data.ToString() ?? String.Empty;
                if (!ypsOldPassVsNewPass.Contains(".<-."))
                    return BadRequest( "expected parameter: oldPass.<-.newPass" );
                string[] ypsSplit = ypsOldPassVsNewPass.Split(".<-.");
                if (_keys.VerifyPassword(_usrs.Entity.Id, ypsSplit[0]))
                    return new OkObjectResult(
               (await _keys.SetMasterKey(_usrs.Entity.Id, ypsSplit[1])).Status.ToString()
                                               );
            } return StatusCode( 500, _keys.Status.ToString() );
        }

        [Produces("application/json"), HttpDelete("{user}")]
        public async Task<IActionResult> RemoveUserAccount(string user, string mail, string pass)
        {
            if ((await _usrs.GetUserByNameOrId(user)).Entity) {
                if (_usrs.Entity.Mail == mail) {
                    if ((await _keys.LookupPasswordByUserAccount(_usrs.GetUserByNameOrId(user))).VerifyPassword(_usrs.Entity.Id, pass)) {
                        if (await _usrs.RemoveUserAccount(_usrs.Entity)) return Ok( _usrs.Status.ToString() );
                        return StatusCode(500, "Removing user account failed: " + _usrs.Status );
                    } return StatusCode(303, _keys.Status );
                } return StatusCode(404, _usrs.Status + "Incorrect Em@il address");
            } return StatusCode(404, _usrs.Status );
        }

        [Produces("application/json"), HttpPost("{user}/Location")]
        public async Task<IActionResult> NewUserLocation(string user, string areaNameYareaPass, string? login, string? info)
        {
            if ((await _usrs.GetUserByNameOrId(user)).Entity) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, areaNameYareaPass);
                if ( yps.Bad ) return BadRequest( yps );
                areaNameYareaPass = yps.Data.ToString() ?? String.Empty;
                if (!areaNameYareaPass.Contains(".~.")) return BadRequest("expected paramerter: locationName.~.locationPass");
                string[] ypsSplit = areaNameYareaPass.Split(".~.");
                UserLocations newArea = new UserLocations();
                newArea.Area = ypsSplit[0];
                newArea.User = _usrs.Entity.Id;
                newArea.Info = info ?? String.Empty;
                newArea.Name = login ?? String.Empty;
                await _locs.SetLocationPassword(_usrs.GetUserByNameOrId(user), newArea, ypsSplit[1]);
                if (_locs.GetLocationOfUser(newArea.User, newArea.Area).IsValid()) {
                    return Ok(_locs.Entity);
                } else {
                    return StatusCode(404, _locs.Status.ToString());
                }
            } else return StatusCode(404, _usrs.Status.ToString());
        }

        [Produces("application/json"), HttpGet("{user}/Locations")]
        public async Task<IActionResult> GetUserLocations(string user) {
            int usrid = _usrs.GetUserId(user);
            if (usrid > 0) {
                return new OkObjectResult( _locs.GetUserLocations(usrid) );
            } else return StatusCode( 404, _usrs.Status.ToString() );
        }

        [Produces("application/json"), HttpGet("{user}/{area}")]
        public async Task<IActionResult> GetUserLocation(string user, string area)
        {
            UserLocations location = _locs.GetLocationOfUser(_usrs.GetUserId(user), area);
            if (location.IsValid()) return Ok( location );
            else return StatusCode(400, location.Is().Status.ToString());
        }

        [Produces("application/json"), HttpDelete("{user}/{area}")]
        public async Task<IActionResult> RemoveLocation(string user, string area, string ypsVerify )
        {
            Status yps = DecryptQueryParameter(_usrs.GetUserId(user), ypsVerify );
            if (yps.Bad) return StatusCode(304, yps);
            ypsVerify = yps.Data.ToString() ?? string.Empty;
            if (await _locs.RemoveLocation(_usrs.GetUserByNameOrId(user), area, ypsVerify))
                return Ok($"Successfully removed password for: {area}");
            else return StatusCode(500, _locs.Status.ToString());
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public async Task<IActionResult> PutUserLocationLogin( string user, string area, string ypsLogin )
        {
            if( !_locs.GetLocationOfUser( _usrs.GetUserId(user), area) ) {
                return StatusCode( 400, _locs.Status.ToString() );
            } else {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, ypsLogin );
                if (yps.Bad) return BadRequest(yps);
                ypsLogin = yps.Data.ToString() ?? string.Empty;
                _locs.Entity.Name = ypsLogin;
                if ( _locs.Update() ) return new OkObjectResult( _locs.Entity ); 
            } return StatusCode( 500, _locs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public async Task<IActionResult> PutUserLocationInfo( string user, string area, string ypsInfo )
        {
            UserLocations location = (await _locs.GetLocationById(_locs.GetAreaId(area, _usrs.GetUserId(user)))).Entity;
            if ( !location.Is().Status.Bad ) {
                Status yps = DecryptQueryParameter(_usrs.Entity.Id, ypsInfo );
                if (yps.Bad) return BadRequest(yps);
                location.Info = yps.Data.ToString();
                _db.UserLocations.Update( location );
                _db.SaveChanges();
                return new OkObjectResult( location );
            } else return StatusCode( 404, location.Is().Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/{area}/Password")]
        public async Task<IActionResult> PutUserLocationPassword(string user, string area, string ypsMasterPassAreaPass)
        {
            if (_locs.GetLocationOfUser( _usrs.GetUserId(user), area ) ) {
                Status yps = DecryptQueryParameter( _usrs.Entity.Id, ypsMasterPassAreaPass);
                if (yps.Bad) return StatusCode(304, yps);
                ypsMasterPassAreaPass = yps.Data.ToString() ?? string.Empty;
                if(!ypsMasterPassAreaPass.Contains(".~."))
                    return BadRequest("expected parameter: masterPass.~.locationPass");
                string[] ypsSplit = ypsMasterPassAreaPass.Split(".~."); 
                if (!(await _locs.SetPassword( ypsSplit[0], ypsSplit[1] )).Status.Bad) {
                    return Ok(_locs.Status.ToString());
                }
            } return StatusCode(404, _locs.Status.ToString());
        }

        private Status DecryptQueryParameter( int userId, string encrypted )
        {
            Yps.CryptKey key = _keys.LookupPasswordByUserAccount( _usrs.GetUserById(userId) ).GetAwaiter().GetResult().GetMasterKey( userId );
            if( key.IsValid() ) {
                string masterPass = key.Decrypt(encrypted);
                if( masterPass != null ) {
                    return Status.Success.WithData( masterPass.Substring(3) );
                } return Status.Invalid.WithText("MasterKey").WithData(Yps.Crypt.Error.ToString());
            } return _usrs.Status.Ok ? _keys.Status : _usrs.Status;
        }

        [Produces("application/json"), HttpGet("{user}/{area}/Password")]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string yps )
        {
            int userId = _usrs.GetUserId(user);
            if (userId <= 0) return StatusCode( 404, _usrs.Status.ToString() );
            if (!_locs.GetLocationOfUser(userId, area))
                return StatusCode(404, _locs.Status.ToString());
            Status masterPass = DecryptQueryParameter( userId, yps );
            if (masterPass.Bad) return StatusCode(500, masterPass.ToString());
            string pass = _locs.GetPassword( masterPass.Data.ToString() );
            if (_locs.Status.Bad) {
                return StatusCode( 303, _locs.Status.ToString() );
            } return Ok( pass );
        }

        [Produces("application/json"), HttpGet("errormessage/{code}")]
        public async Task<IActionResult> GetErrorMessage(uint code)
        {
            Status textFromErrorCode = new Status((ResultCode) code, "message from error code: {0}", (ResultCode) code);
            if (textFromErrorCode.Ok) return Ok(textFromErrorCode.ToString());
            if (textFromErrorCode.Bad) return StatusCode(404, textFromErrorCode.ToString());
            else return StatusCode( 500, textFromErrorCode.ToString() ); 
        }
    }
}
