using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        private readonly ILogger<PasswordsController> log;
        private PasswordsDbContext   dbc;
        private PasswordUsersService usrs;
        private UserPasswordsService keys;
        private UserLocationsService locs;

        public PasswordsController( ILogger<PasswordsController> logger, PasswordsDbContext  ctx, 
                                    IPasswordsApiService<PasswordUsers,PasswordUsersService> usr,
                                    IPasswordsApiService<UserPasswords,UserPasswordsService> pwd,
                                    IPasswordsApiService<UserLocations,UserLocationsService> loc ) {
            log = logger;
            dbc = ctx;
            usrs = usr.serve();
            locs = loc.serve();
            keys = pwd.serve();
        }


        [Produces( "application/json" ), HttpGet( "User")]
        public IActionResult GetUser()
        {
            IEnumerator<PasswordUsers> usinger = dbc.PasswordUsers.AsNoTracking().GetEnumerator();
            List<PasswordUsers> listinger = new List<PasswordUsers>();
            while ( usinger.MoveNext() ) {
                 listinger.Add( usinger.Current );
            } usinger.Dispose(); 
            return new OkObjectResult( listinger );
        }

        [Produces(  "application/json" ), HttpGet( "{user}/Info" )]
        public async Task<IActionResult> GetUserInfo(string user)
        {
            if ( (await usrs.ByNameOrId( user )).Entity.Is().Status.Bad ) {
                return StatusCode( 400, usrs.Status.ToString() );
            } else {
                return Ok( usrs.Entity.Info );
            }
        }

        [Produces( "application/json" ), HttpPut( "{user}/Info" )]
        public async Task<IActionResult> PutUserInfo(string user,string info)
        {
            if ( (await usrs.ByNameOrId(user)).Status.Ok ) {
                PasswordUsers usr = usrs.Entity;
                usr.Info = info;
                dbc.Update( usr );
                dbc.SaveChangesAsync();
                return new OkObjectResult( usr );
            } else return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/Pass" )]
        public async Task<ActionResult> SetUserPassword( string user, string oldpass, string newpass )
        {
            Task<PasswordUsersService> getuser = usrs.ByNameOrId(user);
            Task<UserPasswordsService> getpass = keys.ByUserEntity(getuser);
            PasswordUsers usr = (await getuser).Entity;
            if (usr.Is().Status.Bad ) return StatusCode( 500, usr.Is().Status.ToString() );
            if ( (await getpass).VerifyPassword( usr.Id, oldpass ) ) {
                return new OkObjectResult((await keys.SetMasterKey(usr.Id, newpass)).Status.ToString());
            } else return StatusCode( 500, keys.Status.ToString() );
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public async Task<IActionResult> PutUserLocationLoginInfo( string user, string area, string login )
        {
            UserLocations loc = (await locs.FromUserByNameOrId( usrs.GetUserId(user), area ))?.Entity ?? locs.Status;
            if ( loc.Is().Status ) {
                return StatusCode( 400, loc.Is().Status.Code.ToInt32() + loc.Is().Status.ToString() );
            } else {
                loc.Name = login;
                dbc.Update( loc );
                dbc.SaveChangesAsync();
            } return new OkObjectResult( loc );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public async Task<IActionResult> PutUserLocationInfo( string user, string area, string info )
        {
            UserLocations location = (await locs.ById(locs.GetAreaId(area, usrs.GetUserId(user)))).Entity;
            if ( location.Is().Status.Ok ) {
                location.Info = info;
                dbc.UserLocations.Update( location );
                dbc.SaveChanges();
                return new OkObjectResult( location );
            } else return StatusCode( 500, location.Is().Status.ToString() );
        }

        [Produces("application/json"), HttpPut("{user}/{area}/Password")]
        public async Task<IActionResult> PutUserLocationPassword(string user, string userPass, string area, string areaPass)
        {
            int userId = usrs.GetUserId( user );
            if( await locs.FromUserByNameOrId( userId, area ) ) {
                if( await locs.SetPassword(userPass, areaPass) ) {
                    return Ok( locs.Status.ToString() );
                }
            } return StatusCode( 500, locs.Status.ToString() );
        }

        [Produces( "application/json"), HttpPost("User")]
        public async Task<IActionResult> NewUser( string name, string email, string pass )
        {
            PasswordUsers usr = (await usrs.CreateNewUser( name, email, pass, "")).Entity;
            if ( usr.NoError() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if( await keys.SetMasterKey( usr.Id, pass ) ) {
                    dbc.PasswordUsers.Remove( usr );
                    dbc.SaveChangesAsync();
                    return StatusCode( 500, keys.Status.ToString() );
                } return new OkObjectResult( usr ?? usrs.Entity );
            } else return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/Locations" )]
        public async Task<IActionResult> GetUserLocations( string user )
        {
            int usrid = usrs.GetUserId( user );
            IEnumerator<UserLocations> locations = dbc.UserLocations.AsNoTracking().GetEnumerator();
            List<UserLocations> returnIt = new List<UserLocations>();
            while ( locations.MoveNext() ) {
                if ( locations.Current.User == usrid) {
                    returnIt.Add( locations.Current );
                }
            } locations.Dispose();
            return new OkObjectResult( returnIt );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}" )]
        public async Task<IActionResult> GetUserLocation( string user, string area )
        {
            UserLocations loc = (await locs.FromUserByNameOrId(  usrs.GetUserId( user ),  area )).Entity;
            if ( loc.NoError() ) return new OkObjectResult( loc );
            else return StatusCode( 400, loc.Is().Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}/Password" )]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string master )
        {
            int userId = usrs.GetUserId( user );
            if ( (await locs.FromUserByNameOrId( userId, area )).Status.Bad ) return StatusCode( 500,locs.Status.ToString() );
            string pass = locs.GetPassword( master );
            if ( locs.Status.Bad ) {
                return StatusCode( 500, locs.Status.ToString()  );
            } return Ok( pass );
        }

        [Produces( "application/json" ), HttpPost( "{user}/Locations" )]
        public async Task<IActionResult> NewUserLocation( string user, string name, string pass, string? login, string? info )
        {
            PasswordUsers usr = (await usrs.ByNameOrId( user )).Entity;
            if( usr.NoError() ) {
                UserLocations newArea = new UserLocations();
                newArea.User = usr.Id; newArea.Area = name;
                newArea.Name = login ?? String.Empty;
                newArea.Info = info ?? String.Empty;
                newArea = (await (await locs.SetLocationPassword(usrs.ByNameOrId(user), newArea, pass )).FromUserByNameOrId( newArea.User, newArea.Area )).Entity;
                if( newArea.NoError() ) {
                    return new OkObjectResult( newArea );
                } else {
                    return StatusCode( 500, locs.Status.ToString() ); }
            } return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpDelete( "{user}/{area}" )]
        public async Task<IActionResult> RemoveLocation( string user, string area, string masterPass )
        {
            if( await locs.RemoveLocation( usrs.ByNameOrId( user ), area, masterPass ) )
                return Ok( $"Successfully removed Password for Location: {area}" );
            else return StatusCode( 500, locs.Status.ToString() );
        }
        
        [Produces( "application/json" ), HttpDelete( "{user}" )]
        public async Task<IActionResult> RemoveUserAccount( string user, string mail, string pass )
        {
            PasswordUsers usr = (await usrs.ByNameOrId( user )).Entity;
            if ( usr.NoError() ) {
                if ( usr.Mail == mail ) {
                    if ( (await keys.ByUserEntity( usrs.ByNameOrId( user ) )).VerifyPassword( usr.Id, pass ) ) {
                        if ( await usrs.RemoveUser( usr ) ) return new OkObjectResult( usrs.Status.ToString() );
                        else return StatusCode( 500, "Removing the user account has failed!" );
                    } return StatusCode( 500, keys.Status.ToString() );
                } return StatusCode( 500, "Status: user email address incorrect" );
            } return StatusCode( 500, usr.Is().Status.ToString() );
        }
    }
}
