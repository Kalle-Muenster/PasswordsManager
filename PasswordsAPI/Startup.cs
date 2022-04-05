#if    !USE_SQLITE
#define USE_MSSQL
#endif

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System.IO;
using Constringer = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Connectione = Microsoft.Data.Sqlite.SqliteConnection;
using Passwords.API.Abstracts;
using Passwords.API.Services;
using Passwords.API.Database;
using Passwords.API.Models;
using Yps;

namespace Passwords.API
{
    public class Startup
    {
        string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        public enum ServerFrameworks {
            USE_MSSQL = 0,
            USE_SQLITE = 1
        }
        public ServerFrameworks DatabaseType { get; }
        private string ApplicationKey { get; }

        // kann weg
        private string ApplicationKey { get; }
        public Startup( IConfiguration configuration ) {
            Configuration = configuration;
            string usedServerType = Configuration[ "ServerFramework:"+Configuration["UsedServerFramework"] ];
            DatabaseType = (ServerFrameworks) System.Enum.Parse( typeof(ServerFrameworks), usedServerType );
            ApplicationKey = Configuration["ApplicationKey"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

            services.AddCors(
                options => {
                    options.AddPolicy( name: MyAllowSpecificOrigins, policy => {
                        policy.WithOrigins("http://localhost:5255")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowAnyOrigin()
                              .SetIsOriginAllowedToAllowWildcardSubdomains();
                        }
                    );
                }
            );

            services.AddControllers();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc( "v1", new OpenApiInfo { Title = "PasswordsAPI", Version = "v1" } );
            });

            services.AddSingleton( PasswordServer.Registry.TheKey );
            services.AddSingleton( new Consola.StdStreams(
                                   Consola.CreationFlags.AppendLog
                                 | Consola.CreationFlags.NoInputLog
                                 | Consola.CreationFlags.NewConsole )
                                 );

            services.AddScoped<IPasswordsApiService<PasswordUsers, PasswordUsersService<PasswordsDbContext>, PasswordsDbContext>, PasswordUsersService<PasswordsDbContext>>();
            services.AddScoped<IPasswordsApiService<UserPasswords, UserPasswordsService<PasswordsDbContext>, PasswordsDbContext>, UserPasswordsService<PasswordsDbContext>>();
            services.AddScoped<IPasswordsApiService<UserLocations, UserLocationsService<PasswordsDbContext>, PasswordsDbContext>, UserLocationsService<PasswordsDbContext>>();

            if( DatabaseType == ServerFrameworks.USE_MSSQL )
                services.AddDbContext<PasswordsDbContext>(
                options => options.UseSqlServer( Configuration.GetConnectionString( "DerBanan" ), null ) );

            if( DatabaseType == ServerFrameworks.USE_SQLITE )
                services.AddDbContext<PasswordsDbContext>(
                options => options.UseSqlite(
                    new Connectione(
                        new Constringer( "Data Source="
                         + new FileInfo( ".\\DataBase\\SqLite\\db.db" ).FullName
                        ).ConnectionString ) ),
                ServiceLifetime.Singleton,
                ServiceLifetime.Transient
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IWebHostEnvironment env )
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PasswordsAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseCors( MyAllowSpecificOrigins );


            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
