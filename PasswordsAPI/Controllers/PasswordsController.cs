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
        public IActionResult GetUserInfo(string user)
        {
            if (usrs.ByNameOrId( user ).Status ) {
                return StatusCode( 400, usrs.Status.ToString() );
            } else {
                return Ok( usrs.Entity.Info );
            }
        }

        [Produces( "application/json" ), HttpPut( "{user}/Info" )]
        public IActionResult PutUserInfo(string user,string info)
        {
            if (usrs.ByNameOrId(user)) {
                PasswordUsers usr = usrs.Entity;
                usr.Info = info;
                dbc.Update( usr );
                dbc.SaveChanges();
                return new OkObjectResult( usr );
            } else return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpPut( "{user}/Pass" )]
        public IActionResult SetUserPassword( string user, string oldpass, string newpass )
        {
            if ( usrs.ByNameOrId( user ).Ok ) {
                PasswordUsers usr = usrs.Entity;
                if ( keys.ByUserEntity( usr ).Ok ) {
                    if ( keys.VerifyPassword( usr.Id, oldpass ) ) {
                        if ( keys.SetMasterKey( usr.Id, newpass ).Ok )
                            return new OkObjectResult( keys.Status.ToString() );
                    }
                } return StatusCode( 500, keys.Status.ToString() );
            } return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public IActionResult PutUserLocationLoginInfo( string user, string area, string login )
        {
            UserLocations loc = locs.FromUserByNameOrId( usrs.GetUserId(user), area )?.Entity ?? locs.Status;
            if ( loc.Is().Status ) {
                return StatusCode( 400, loc.Is().Status.Code.ToInt32() + loc.Is().Status.ToString() );
            } else {
                loc.Name = login;
                dbc.Update( loc );
                dbc.SaveChanges();
            } return new OkObjectResult( loc );
        }

        [Produces( "application/json" ), HttpPut( "{user}/{area}/Info" )]
        public IActionResult PutUserLocationInfo( string user, string area, string info )
        {
            if( locs.ById(locs.GetAreaId( area, usrs.GetUserId( user ) ) ) ) {
                locs.Entity.Info = info;
                dbc.UserLocations.Update( locs.Entity );
                dbc.SaveChanges();
                return new OkObjectResult( locs.Entity );
            } else return StatusCode( 500, locs.Status.ToString() );
        }

        [Produces( "application/json"), HttpPost("User")]
        public IActionResult NewUser(string name, string email, string pass)
        {
            PasswordUsers usr = usrs.CreateNewUser( name, email, pass, "").Entity;
            if ( usr.NoError() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if( keys.SetMasterKey( usr.Id, pass ).Status ) {
                    dbc.PasswordUsers.Remove( usr );
                    dbc.SaveChanges();
                    return StatusCode( 500, keys.Status.ToString() );
                } return new OkObjectResult( usr ?? usrs.Entity );
            } else return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/Locations" )]
        public IActionResult GetUserLocations( string user )
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
        public IActionResult GetUserLocation( string user, string area )
        {
            UserLocations loc = locs.FromUserByNameOrId(  usrs.GetUserId( user ),  area ).Entity;
            if ( loc.NoError() ) return new OkObjectResult( loc );
            else return StatusCode( 400, loc.Is().Status.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}/Password" )]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string master )
        {
            int userId = usrs.GetUserId( user );
            if ( locs.FromUserByNameOrId( userId, area ).Status ) return StatusCode( 500,locs.Status.ToString() );
            string pass = locs.GetPassword( master );
            if ( locs.Status ) {
                return StatusCode( 500, locs.Status.ToString()  );
            } return Ok( pass );
        }

        [Produces( "application/json" ), HttpPost( "{user}/Locations" )]
        public IActionResult NewUserLocation( string user, string name, string pass, string? login, string? info )
        {
            PasswordUsers usr = usrs.ByNameOrId( user ).Entity;
            if( usr.NoError() ) {
                UserLocations newArea = new UserLocations();
                newArea.User = usr.Id; newArea.Area = name;
                newArea.Name = login ?? String.Empty;
                newArea.Info = info ?? String.Empty;
                newArea = locs.SetLocationPassword( usr, newArea, pass )
                       .FromUserByNameOrId( newArea.User, newArea.Area ).Entity;
                if( newArea.NoError() ) {
                    return new OkObjectResult( newArea );
                } else {
                    return StatusCode( 500, locs.Status.ToString() ); }
            } return StatusCode( 500, usrs.Status.ToString() );
        }

        [Produces( "application/json" ), HttpDelete( "{user}/{area}" )]
        public IActionResult RemoveLocation( string user, string area, string masterPass )
        {
            if( locs.RemoveLocation( usrs.ByNameOrId( user ).Entity, area, masterPass ).Ok )
                return Ok( $"Successfully removed Password for Location: {area}" );
            else return StatusCode( 500, locs.Status.ToString() );
        }
        
        [Produces( "application/json" ), HttpDelete( "{user}" )]
        public IActionResult RemoveUserAccount( string user, string mail, string pass )
        {
            PasswordUsers usr = usrs.ByNameOrId( user ).Entity;
            if ( usr.NoError() ) {
                if ( usr.Mail == mail ) {
                    if ( keys.ByUserEntity( usr ).VerifyPassword( usr.Id, pass ) ) {
                        if ( usrs.RemoveUser( usr ).Status ) return new OkObjectResult( usrs.Status.ToString() );
                        else return StatusCode( 500, "Removing the user account has failed!" );
                    } return StatusCode( 500, keys.Status.ToString() );
                } return StatusCode( 500, "Status: user email address incorrect" );
            } return StatusCode( 500, usr.Is().Status.ToString() );
        }
    }
}
