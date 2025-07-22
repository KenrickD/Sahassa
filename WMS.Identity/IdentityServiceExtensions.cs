using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using System.Threading.Tasks;
using WMS.Identity.Models;
using WMS.Identity.Data;
using WMS.Identity.Services;
using WMS.Identity.Requirements;
using WMS.Identity.Handlers;

namespace WMS.Identity
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Identity DbContext
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("IdentityConnection"),
                    b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

            // Add ASP.NET Core Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 10;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

            // Add JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Configure Authorization Policies
            services.AddAuthorization(options =>
            {
                // System level policies
                options.AddPolicy("RequireSystemAdmin", policy =>
                    policy.RequireRole("SystemAdmin"));

                // Warehouse level policies
                options.AddPolicy("RequireWarehouseManager", policy =>
                    policy.RequireRole("WarehouseManager"));

                options.AddPolicy("RequireWarehouseAccess", policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole("SystemAdmin") ||
                        context.User.IsInRole("WarehouseManager") ||
                        context.User.IsInRole("WarehouseUser")));

                // Client level policies
                options.AddPolicy("RequireClientAccess", policy =>
                    policy.AddRequirements(new ClientAccessRequirement()));

                // Feature-specific policies
                options.AddPolicy("CanManageInventory", policy =>
                    policy.RequireClaim("Permission", "Inventory.Manage"));

                options.AddPolicy("CanManageOrders", policy =>
                    policy.RequireClaim("Permission", "Orders.Manage"));

                options.AddPolicy("CanViewReports", policy =>
                    policy.RequireClaim("Permission", "Reports.View"));
            });

            // Register Authorization Handlers
            services.AddScoped<IAuthorizationHandler, ClientAccessHandler>();

            // Identity Services
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<ITokenService, TokenService>();

            // Configuration
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            return services;
        }
    }
}