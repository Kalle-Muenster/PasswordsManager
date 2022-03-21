using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PasswordsAPI.BaseClasses;
using PasswordsAPI.Services;
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
        private PasswordUsersService _usrs;
        private UserPasswordsService _keys;
        private UserLocationsService _locs;

        public PasswordsController( ILogger<PasswordsController> logger, PasswordsDbContext  db, 
                                    IPasswordsApiService<PasswordUsers,PasswordUsersService,PasswordsDbContext> usrs,
                                    IPasswordsApiService<UserPasswords,UserPasswordsService,PasswordsDbContext> keys,
                                    IPasswordsApiService<UserLocations,UserLocationsService,PasswordsDbContext> locs ) {
            _logger = logger;
            _db = db;
            _usrs = usrs.serve();
            _locs = locs.serve();
            _keys = keys.serve();
        }


        [Produces( "application/json" ), HttpGet( "User")]
        public IActionResult GetUser()
        {
            return new OkObjectResult( _usrs.ListAllUsers() );
        }

        [Produces(  "application/json" ), HttpGet( "{user}/Info" )]
        public async Task<IActionResult> GetUserInfo(string user)
        {
            if ( (await _usrs.ByNameOrId( user )).Entity.Is().Status.Bad ) {
                return StatusCode( 400, _usrs.Status.ToString() );
            } else {
                return Ok( _usrs.Entity.Info );
            }
        }

        [Produces( "application/json" ), HttpPut( "{user}/Info" )]
        public async Task<IActionResult> PutUserInfo(string user,string info)
        {
            if ( (await _usrs.ByNameOrId(user)).Entity.IsValid() ) {
                PasswordUsers usr = _usrs.Entity;
                usr.Info = info;
                _db.Update( usr );
                _db.SaveChangesAsync();
                return new OkObjectResult( usr );
            } else return StatusCode( 500, _usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/Pass" )]
        public async Task<ActionResult> SetUserPassword( string user, string oldpass, string newpass )
        {
            if( await _keys.ForUserAccount( _usrs.ByNameOrId(user) ) ) {
                if ( _keys.VerifyPassword( _usrs.Entity.Id, oldpass ) )
                    return new OkObjectResult(
               (await _keys.SetMasterKey(_usrs.Entity.Id, newpass)).Status.ToString()
                                               );
            } return StatusCode(500, _keys.Status.ToString() );
        }    
        
        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public async Task<IActionResult> PutUserLocationLogin( string user, string area, string login )
        {
            if( !await _locs.GetLocationEntity( _usrs.GetUserId(user), area) ) {
                return StatusCode( 400, _locs.Status.ToString() );
            } else {
                _locs.Entity.Name = login;
                if ( _locs.Update() ) return new OkObjectResult( _locs.Entity ); 
            } return StatusCode( 500, _locs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public async Task<IActionResult> PutUserLocationInfo( string user, string area, string info )
        {
            UserLocations location = (await _locs.GetLocationEntity(_locs.GetAreaId(area, _usrs.GetUserId(user)))).Entity;
            if ( location.Is().Status.Ok ) {
                location.Info = info;
                _db.UserLocations.Update( location );
                _db.SaveChanges();
                return new OkObjectResult( location );
            } else return StatusCode( 404, location.Is().Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/{area}/Password")]
        public async Task<IActionResult> PutUserLocationPassword(string user, string userPass, string area, string areaPass)
        {
            if( await _locs.GetLocationEntity(_usrs.GetUserId(user), area ) ) {
                if( await _locs.SetPassword(userPass, areaPass) ) {
                    return Ok( _locs.Status.ToString() );
                }
            } return StatusCode( 404, _locs.Status.ToString() );
        }

        [Produces( "application/json"), HttpPost("User")]
        public async Task<IActionResult> NewUser( string name, string email, string pass )
        {
            PasswordUsers user = (await _usrs.CreateNewUser( name, email, pass, "")).Entity;
            if ( user.IsValid() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if (await _keys.SetMasterKey( user.Id, pass )) {
                    return new OkObjectResult( user );
                } else {
                    _db.PasswordUsers.Remove( user );
                    _db.SaveChanges();
                    return StatusCode( 500, _keys.Status.ToString() );
                }
            } else return StatusCode( 500, _usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/Locations" )]
        public async Task<IActionResult> GetUserLocations( string user )
        {
            int usrid = _usrs.GetUserId( user );
            if (usrid > 0) {
                return new OkObjectResult(_locs.GetUserLocations(usrid) );
            } else return StatusCode( 404, _usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}" )]
        public async Task<IActionResult> GetUserLocation( string user, string area )
        {
            UserLocations location = ( await _locs.GetLocationEntity(_usrs.GetUserId( user ), area ) ).Entity;
            if (location.IsValid() ) return new OkObjectResult( location );
            else return StatusCode( 400, location.Is().Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}/Password" )]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string master )
        {
            int userId = _usrs.GetUserId( user );
            if ( !(await _locs.GetLocationEntity( userId, area )) )
                return StatusCode( 500,_locs.Status.ToString() );
            string pass = _locs.GetPassword( master );
            if ( _locs.Status.Bad ) {
                return StatusCode( 500, _locs.Status.ToString()  );
            } return Ok( pass );
        }

        [Produces( "application/json" ), HttpPost( "{user}/Locations" )]
        public async Task<IActionResult> NewUserLocation( string user, string name, string pass, string? login, string? info )
        {
            if( await _usrs.ByNameOrId( user ) ) {
                UserLocations newArea = new UserLocations();
                newArea.Area = name;
                newArea.User = _usrs.Entity.Id;
                newArea.Info = info ?? String.Empty;
                newArea.Name = login ?? String.Empty;
                await _locs.SetLocationPassword(_usrs.ByNameOrId(user), newArea, pass );
                await _locs.GetLocationEntity( newArea.User, newArea.Area );
                if( _locs.Entity.IsValid() ) {
                    return new OkObjectResult( _locs.Entity );
                } else {
                    return StatusCode( 500, _locs.Status.ToString() ); }
            } else
                return StatusCode( 500, _usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpDelete( "{user}/{area}" )]
        public async Task<IActionResult> RemoveLocation( string user, string area, string masterPass )
        {
            if( await _locs.RemoveLocation( _usrs.ByNameOrId( user ), area, masterPass ) )
                return Ok( $"Successfully removed password for: {area}" );
            else return StatusCode( 500, _locs.Status.ToString() );
        }
        
        [Produces( "application/json" ), HttpDelete( "{user}" )]
        public async Task<IActionResult> RemoveUserAccount( string user, string mail, string pass )
        {
            if ( await _usrs.ByNameOrId( user ) ) {
                if ( _usrs.Entity.Mail == mail ) {
                    if ( (await _keys.ForUserAccount( _usrs.ByNameOrId( user ) )).VerifyPassword( _usrs.Entity.Id, pass ) ) {
                        if ( await _usrs.RemoveUser(_usrs.Entity ) ) return new OkObjectResult( _usrs.Status.ToString() );
                        else return StatusCode( 500, "removing user account failed" );
                    } return StatusCode( 303, _keys.Status.ToString() );
                } return StatusCode( 404, "incorrect Em@il address");
            } return StatusCode( 404, _usrs.Status.ToString() );
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
