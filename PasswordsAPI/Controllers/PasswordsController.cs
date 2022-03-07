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

        public PasswordsController( ILogger<PasswordsController> logger, PasswordsDbContext ctx, 
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
            if (usrs.ByNameOrId( user ).Error ) {
                return StatusCode( 400, usrs.Error.ToString() );
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
            } else return StatusCode( 500, usrs.Error.ToString() );
        }

        [Produces(  "application/json" ), HttpPut( "{user}/{area}/Login" )]
        public IActionResult PutUserLocationLoginInfo( string user, string area, string login )
        {
            UserLocations loc = locs.FromUserByNameOrId( usrs.GetUserId(user), area )?.Entity ?? locs.Error;
            if ( loc.Is().Error ) {
                return StatusCode( 400, loc.Is().Error.Code.ToInt32() + loc.Is().Error.ToString() );
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
            } else return StatusCode( 500, locs.Error.ToString() );
        }

        /*---------------------------------------------------------------------------------------*/
        
        [Produces( "application/json"), HttpPost("User")]
        public IActionResult NewUser(string name, string email, string pass)
        {
            PasswordUsers usr = usrs.CreateNewUser( name, email, pass, "").Entity;
            if ( usr.IsValid() ) {
                // as soon user has been add, set the users master password
                // (or a password hash, if user keys residing client sided)
                if( keys.SetMasterKey( usr.Id, pass ).Error ) {
                    dbc.PasswordUsers.Remove( usr );
                    dbc.SaveChanges();
                    return StatusCode( 500, keys.Error.ToString() );
                } return new OkObjectResult( usr ?? usrs.Entity );
            } else return StatusCode( 500, usrs.Error.ToString() );
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
            if ( loc.IsValid() ) return new OkObjectResult( loc );
            else return StatusCode( 400, loc.Is().Error.ToString() );
        }

        [Produces( "application/json" ), HttpGet( "{user}/{area}/Password" )]
        public async Task<IActionResult> GetUserLocationPassword( string user, string area, string master )
        {
            int userId = usrs.GetUserId( user );
            int areaId = locs.GetAreaId( area, userId );
            return Ok( locs.FromUserByNameOrId( userId, areaId.ToString() )?.SetKey( Crypt.CreateKey( master ) ).GetPassword() );
        }

        [Produces( "application/json" ), HttpPost( "{user}/Locations" )]
        public IActionResult NewUserLocation( string user, string name, string pass, string? login, string? info )
        {
            PasswordUsers usr = usrs.ByNameOrId( user ).Entity;
            if( usr.IsValid() ) {
                UserLocations newArea = new UserLocations();
                newArea.User = usr.Id; newArea.Area = name;
                newArea.Name = login ?? String.Empty;
                newArea.Info = info ?? String.Empty;
                newArea = locs.SetLocationPassword( usr, newArea, pass )
                       .FromUserByNameOrId( newArea.User, newArea.Area ).Entity;
                if( newArea.IsValid() ) {
                    return new OkObjectResult( newArea );
                } else {
                    return StatusCode( 500, locs.Error.ToString() ); }
            } return StatusCode( 500, usrs.Error.ToString() );
        }
    }
}