namespace DigitalAssets.API
{
    using DigitalAssets.API.API.Repositories.Interfaces;
    using DigitalAssets.API.API.Repositories.Repository;
    using DigitalAssets.API.API.Services.Interfaces;
    using DigitalAssets.API.API.Services.Services;
    using DigitalAssets.API.Helpers;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using Newtonsoft.Json;
    using NLog;
    using Swashbuckle.AspNetCore.Swagger;
    using System;
    using System.Text;


    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettingsSection = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsSection.Get<AppSettings>();


            services.AddDbContext<DigitalAssetsDbContext>(options =>
            {
                options.UseMySQL(appSettings.ConfigurationDbConnection, settings =>
                 {
                     settings.CommandTimeout(appSettings.CommandTimeout);
                 });

                options.UseLazyLoadingProxies(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            });

            #region depedencies

            //service depedencies

            services.AddScoped<DbContext, DigitalAssetsDbContext>();

            #region repositoryDepedencies

            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<ILogsRepository, LogsRepository>();
            services.AddScoped<IDigitalAssetLoadBatchRepository, DigitalAssetLoadBatchRepository>();
            services.AddScoped<IDigitalAssetRepository, DigitalAssetRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IAssetTypesRepository, AssetTypesRepository>();
            services.AddScoped<IRoleRepository, RoleReposotory>();
           
            #endregion

            #region serviceDepedencies


            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<ILogsService, LogsService>();
            services.AddScoped<IDigitalAssetLoadBatchService, DigitalAssetLoadBatchService>();
            services.AddScoped<IDigitalAssetService, DigitalAssetService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IAssetTypeService, AssetTypeService>();
            services.AddScoped<IRoleService, RoleService>();
          
            #endregion

            //end of service depedencies

            //general depedencies

            services.AddSingleton(appSettings);
            services.AddSingleton<ILogger>(LogManager.GetCurrentClassLogger());

            //end of general depedencies

            #endregion

            #region JwtAuthenticationSettings

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.SigningKey));

            services.AddAuthentication(token =>
                {
                    token.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    token.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(tokenSettings =>
              {
                  tokenSettings.RequireHttpsMetadata = false;
                  tokenSettings.SaveToken = true;
                  tokenSettings.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = symmetricSecurityKey,
                      ValidateIssuer = false,
                      ValidateAudience = false,
                      ClockSkew = TimeSpan.FromMinutes(appSettings.ClockSkew),
                      RequireSignedTokens = true,
                      RequireExpirationTime = true,
                      ValidateLifetime = true,
                  };
              });


            #endregion

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                             .AddJsonOptions(options =>
                            {
                                options.SerializerSettings.Formatting = Formatting.Indented;
                            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "DigitalAssets API",
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(corsPolicy => corsPolicy
                                               .AllowAnyOrigin()
                                               .AllowAnyMethod()
                                               .AllowAnyHeader()
                                               .AllowCredentials())
                                               .UseAuthentication();

            app.UseMvc();
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "DigitalAssets V1");
            });
        }
    }
}
