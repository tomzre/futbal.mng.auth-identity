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
            var connectionString = Configuration.GetConnectionString("SqlServerConnection");
            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(connectionString));
            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            //services.AddControllersWithViews();
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

            // configures IIS out-of-proc settings (see https://github.com/aspnet/AspNetCore/issues/14882)
            

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            var builder = services.AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = "http://localhost:3000";
                    options.UserInteraction.ErrorUrl = "http://localhost:3000/error";
                    options.UserInteraction.LogoutUrl = "http://localhost:3000/logout";
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddTestUsers(TestUsers.Users)
                .AddInMemoryClients(Config.Clients)
                .AddInMemoryIdentityResources(Config.Ids)
                .AddInMemoryApiResources(Config.Apis)
                // this adds the config data from DB (clients, resources, CORS)
                // .AddConfigurationStore(options =>
                // {
                //     options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString, 
                //     sqlOption => {
                //         sqlOption.MigrationsAssembly(migrationsAssembly);
                //     });
                // })
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

            //services.AddTransient<IProfileService, IdentityClaimsProfileService>();

            services.AddAuthentication()
                .AddGitHub(options =>
                {
                    options.ClientId = "empty";
                    options.ClientSecret = "empty";
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                })
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to http://localhost:5000/signin-google
                    options.ClientId = "empty";
                    options.ClientSecret = "empty";
                });

            var cors = new DefaultCorsPolicyService(new LoggerFactory().CreateLogger<DefaultCorsPolicyService>())
            {
                AllowAll = true
            };
            services.AddGrpc();
            services.AddControllers(
    //             config =>
    // {
    //     var policy = new AuthorizationPolicyBuilder()
    //                      .RequireAuthenticatedUser()
    //                      .Build();
    //     config.Filters.Add(new AuthorizeFilter(policy));
    // }
    );
            //services.AddSingleton<ICorsPolicyService>(cors);
            services.AddTransient<IReturnUrlParser, Helpers.ReturnUrlParser>();
            services.AddMvc(options => {
                options.EnableEndpointRouting = false; 
                })
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
