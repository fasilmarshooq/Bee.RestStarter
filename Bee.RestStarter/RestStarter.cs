using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Text;

namespace Bee.RestStarter
{
    public static class RestStarter
    {
                
        public static string CorsPolicyName = "LowCorsPolicy";
        public static string SwaggerAppName = "App";
        public static string SwaggerAppVersion = "V1";
        public static string JWTSecretKeyName = "JWTSecret";
        public static string AppSettingsName = "appsettings.json";
        internal static IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsName)
                .Build();

        #region - configureSwitch
        public static bool ShouldConfigureSwagger = true;
        public static bool ShouldConfigureJWT = true;
        public static bool ShouldConfigureCors = true;
        public static bool ShouldConfigureContextAccessor = true;
        public static bool ShowSwaggerDoc = true;
        public static bool UseSerilog = true;
        #endregion

        public static void ConfigureServices(IServiceCollection services)
        {
            ConfigureControllers(services);
            ConfigureSwagger(services);
            ConfigureJWT(services);
            ConfigureCORS(services);
            ConfigureHttpContextAccessor(services);
        }

        private static void ConfigureControllers(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(x =>
            {
                x.SerializerSettings.Converters.Add(new StringEnumConverter());
                x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            }
            );
        }

        public static void ConfigureApp(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            if (ShouldConfigureSwagger && ShowSwaggerDoc)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", SwaggerAppName + " " + SwaggerAppVersion));
            }

            if (ShouldConfigureCors)
            {
            app.UseCors(CorsPolicyName);
            }

            if (UseSerilog)
            {
                app.UseSerilogRequestLogging();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        public static void ConfigureLogger()
        {
            Log.Logger =  new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger();
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            if (!ShouldConfigureSwagger) return;
            services.AddSwaggerGen(c => c.SwaggerDoc(SwaggerAppVersion, new OpenApiInfo { Title = SwaggerAppName, Version = SwaggerAppVersion }));
        }

        private static void ConfigureJWT(IServiceCollection services)
        {
            if (!ShouldConfigureJWT) return;

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetValue(JWTSecretKeyName, string.Empty))),
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }

        private static void ConfigureCORS(IServiceCollection services)
        {
            if (!ShouldConfigureCors) return;

            services.AddCors(o => o.AddPolicy(CorsPolicyName, builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
        }

        private static void ConfigureHttpContextAccessor(IServiceCollection services)
        {
            if (!ShouldConfigureContextAccessor) return;
            services.AddHttpContextAccessor();
        }
    }
}
