using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Consola;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PasswordsAPI.Services;
using Yps;


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
                                    IPasswordsApiService<PasswordUsers,PasswordUsersService> usrs,
                                    IPasswordsApiService<UserPasswords,UserPasswordsService> keys,
                                    IPasswordsApiService<UserLocations,UserLocationsService> locs ) {
            _logger = logger;
            _db = db;
            _usrs = usrs.serve();
            _locs = locs.serve();
            _keys = keys.serve();
        }


        [Produces( "application/json" ), HttpGet( "User")]
        public IActionResult GetUser()
        {
            StdStream.Out.WriteLine(Request.Path);
            IEnumerator<PasswordUsers> usinger = _db.PasswordUsers.AsNoTracking().GetEnumerator();
            List<PasswordUsers> listinger = new List<PasswordUsers>();
            while ( usinger.MoveNext() ) {
                 listinger.Add( usinger.Current );
            } usinger.Dispose(); 
            StdStream.Out.WriteLine("Returns Ok");
            return new OkObjectResult( listinger );
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
            if ( (await _usrs.ByNameOrId(user)).Status.Ok ) {
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
            Task<PasswordUsersService> getuser = _usrs.ByNameOrId(user);
            Task<UserPasswordsService> getpass = _keys.ByUserEntity(getuser);
            PasswordUsers usr = (await getuser).Entity;
            if (usr.Is().Status.Bad ) return StatusCode( 500, usr.Is().Status.ToString() );
            if ( (await getpass).VerifyPassword( usr.Id, oldpass ) ) {
                return new OkObjectResult((await _keys.SetMasterKey(usr.Id, newpass)).Status.ToString());
            } else return StatusCode( 500, _keys.Status.ToString() );
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public async Task<IActionResult> PutUserLocationLogin( string user, string area, string login )
        {
            UserLocations loc = (await _locs.FromUserByNameOrId( _usrs.GetUserId(user), area ))?.Entity ?? _locs.Status;
            if ( loc.Is().Status ) {
                return StatusCode( 400, loc.Is().Status.Code.ToInt32() + loc.Is().Status.ToString() );
            } else {
                loc.Name = login;
                _db.Update( loc );
                _db.SaveChanges();
            } return new OkObjectResult( loc );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public async Task<IActionResult> PutUserLocationInfo( string user, string area, string info )
        {
            UserLocations location = (await _locs.ById(_locs.GetAreaId(area, _usrs.GetUserId(user)))).Entity;
            if ( location.Is().Status.Ok ) {
                location.Info = info;
                _db.UserLocations.Update( location );
                _db.SaveChanges();
                return new OkObjectResult( location );
            } else return StatusCode( 500, location.Is().Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/{area}/Password")]
        public async Task<IActionResult> PutUserLocationPassword(string user, string userPass, string area, string areaPass)
        {
            if( await _locs.FromUserByNameOrId(_usrs.GetUserId(user), area ) ) {
                if( await _locs.SetPassword(userPass, areaPass).ConfigureAwait(false) ) {
                    return Ok( _locs.Status.ToString() );
                }
            } return StatusCode( 500, _locs.Status.ToString() );
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
            IEnumerator<UserLocations> locations = _db.UserLocations.AsNoTracking().GetEnumerator();
            List<UserLocations> returnList = new List<UserLocations>();
            while ( locations.MoveNext() ) {
                if ( locations.Current.User == usrid) {
                    returnList.Add( locations.Current );
                }
            } locations.Dispose();
            return new OkObjectResult( returnList );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}" )]
        public async Task<IActionResult> GetUserLocation( string user, string area )
        {
            UserLocations location = ( await _locs.FromUserByNameOrId(_usrs.GetUserId( user ), area ) ).Entity;
            if (location.IsValid() ) return new OkObjectResult( location );
            else return StatusCode( 400, location.Is().Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}/Password" )]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string master )
        {
            int userId = _usrs.GetUserId( user );
            if ( !(await _locs.FromUserByNameOrId( userId, area )) )
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
                newArea.User = _usrs.Entity.Id; newArea.Area = name;
                newArea.Name = login ?? String.Empty;
                newArea.Info = info ?? String.Empty;
                await (await _locs.SetLocationPassword(_usrs.ByNameOrId(user), newArea, pass )
                       ).FromUserByNameOrId( newArea.User, newArea.Area );
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
                return Ok( $"Successfully removed user location: {area}" );
            else return StatusCode( 500, _locs.Status.ToString() );
        }
        
        [Produces( "application/json" ), HttpDelete( "{user}" )]
        public async Task<IActionResult> RemoveUserAccount( string user, string mail, string pass )
        {
            if ( await _usrs.ByNameOrId( user ) ) {
                if ( _usrs.Entity.Mail == mail ) {
                    if ( (await _keys.ByUserEntity( _usrs.ByNameOrId( user ) )).VerifyPassword( _usrs.Entity.Id, pass ) ) {
                        if ( await _usrs.RemoveUser(_usrs.Entity ) ) return new OkObjectResult( _usrs.Status.ToString() );
                        else return StatusCode( 500, "removing the user account has failed" );
                    } return StatusCode( 500, _keys.Status.ToString() );
                } return StatusCode( 500, "incorrect Em@il address");
            } return StatusCode( 500, _usrs.Entity.Is().Status.ToString() );
        }
    }
}
