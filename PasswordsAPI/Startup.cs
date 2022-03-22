#if    !USE_SQLITE
#define USE_MSSQL
#endif

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
//#if USE_SQLITE
using System.IO;
using Constringer = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Connectione = Microsoft.Data.Sqlite.SqliteConnection;
//#endif
using PasswordsAPI.Services;
using PasswordsAPI.Models;
using PasswordsAPI.BaseClasses;
using PasswordsAPI.Database;

namespace PasswordsAPI
{
    public class Startup
    {
        public enum ServerFrameworks {
            USE_MSSQL = 0, USE_SQLITE = 1
        }

        public ServerFrameworks DatabaseType { get; } 

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
            string usedServerType = Configuration[ "ServerFramework:"+Configuration["UsedServerFramework"] ];
            DatabaseType = (ServerFrameworks) System.Enum.Parse( typeof(ServerFrameworks), usedServerType );
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

            services.AddControllers();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PasswordsAPI", Version = "v1" });
            });


            services.AddScoped< IPasswordsApiService<PasswordUsers,PasswordUsersService<PasswordsDbContext>,PasswordsDbContext>, PasswordUsersService<PasswordsDbContext>>();
            services.AddScoped<IPasswordsApiService<UserPasswords, UserPasswordsService<PasswordsDbContext>, PasswordsDbContext>, UserPasswordsService<PasswordsDbContext>>();
            services.AddScoped<IPasswordsApiService<UserLocations, UserLocationsService<PasswordsDbContext>, PasswordsDbContext>, UserLocationsService<PasswordsDbContext>>();

            if( DatabaseType == ServerFrameworks.USE_MSSQL )
                services.AddDbContext<PasswordsDbContext>(
                options => options.UseSqlServer( Configuration.GetConnectionString( "DerBanan" ), null ) );

            if( DatabaseType==ServerFrameworks.USE_SQLITE )
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

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
