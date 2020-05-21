// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using FutbalMng.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Stores;
using FutbalMng.Auth.Helpers;
using futbal.mng.auth_identity.Helpers;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Configuration;
using RawRabbit.Instantiation;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Channel.Abstraction;
using System.IO;
using RawRabbit.Enrichers.MessageContext.Context;
using Microsoft.AspNetCore.Http.Extensions;
using RawRabbit;

namespace FutbalMng.Auth
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            var rabbitConfig = new RawRabbitConfiguration();
            Configuration.GetSection("rabbitmq").Bind(rabbitConfig);

            var connectionString = Configuration.GetConnectionString("SqlServerConnection");

            services.AddTransient<IReturnUrlParser, FutbalMng.Auth.Helpers.ReturnUrlParser>();

            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(connectionString,
                    sqlOption =>
                    {
                        sqlOption.MigrationsAssembly(migrationsAssembly);
                    }));
            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddCors(setup =>
            {
                setup.AddPolicy("CorsPolicy", policy =>
                {
                    policy.AllowAnyHeader();
                    policy.AllowAnyMethod();
                    policy.WithOrigins("http://localhost:3000");
                    policy.AllowCredentials();
                });
            });


            var builder = services.AddIdentityServer(options =>
                {
                    options.Cors.CorsPaths = new List<PathString>{
                            new PathString("/api/authenticate"),
                            new PathString("/api/authenticate/logout"),
                            new PathString("/api/authenticate/externalLogin")
                            };
                    options.UserInteraction.LoginUrl = "http://localhost:3000/signin";
                    options.UserInteraction.ErrorUrl = "http://localhost:3000/error";
                    options.UserInteraction.LogoutUrl = "http://localhost:3000/logout";
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddAspNetIdentity<AppUser>()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlOption =>
                    {
                        sqlOption.MigrationsAssembly(migrationsAssembly);
                    });
                })
                // // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlOption =>
                    {
                        sqlOption.MigrationsAssembly(migrationsAssembly);
                    });

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();

            var cors = new DefaultCorsPolicyService(new LoggerFactory().CreateLogger<DefaultCorsPolicyService>())
            {
                AllowAll = true
            };
            services.AddSingleton<ICorsPolicyService>(cors);

            services.AddAuthentication()
                .AddGitHub("GitHub", options =>
                {
                    options.ClientId = "--";
                    options.ClientSecret = "secret";
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                })
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to http://localhost:5000/signin-google

                    options.ClientId = "--";
                    options.ClientSecret = "secret";
                })
                .AddFacebook(options => 
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.AppId = "--";
                    options.ClientSecret = "secret";
                });
            services.AddGrpc();
            
            services.AddControllers()
                .AddNewtonsoftJson(options =>
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    );
            services.AddMvc(options => { options.EnableEndpointRouting = false; })
                .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");

            //app.UseRouting();
            app.UseIdentityServer();
            app.UseMvc();
            // app.UseAuthorization();
        }
    }
}