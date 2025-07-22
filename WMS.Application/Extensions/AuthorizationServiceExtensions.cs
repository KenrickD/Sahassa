using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WMS.Application.Authorization;
using WMS.Application.Authorization.Handlers;
using WMS.Application.Authorization.Requirements;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;

namespace WMS.Application.Extensions
{
    public static class AuthorizationServiceExtensions
    {
        public static IServiceCollection AddWMSAuthorization(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure Auth Settings
            services.Configure<AuthSettings>(configuration.GetSection("AuthSettings"));
            var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>();

            // Add Password Hasher
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            // Add Authorization
            services.AddAuthorizationCore(options =>
            {
                //options.FallbackPolicy = new AuthorizationPolicyBuilder()
                //     .RequireAuthenticatedUser()
                //     .Build();
                // System-level policies
                options.AddPolicy("SystemAdmin", policy =>
                    policy.RequireRole("SystemAdmin"));

                // Warehouse policies
                options.AddPolicy("WarehouseManager", policy =>
                    policy.RequireRole("WarehouseManager"));

                options.AddPolicy("WarehouseUser", policy =>
                    policy.RequireRole("WarehouseUser", "WarehouseManager", "SystemAdmin"));

                // Client policies
                options.AddPolicy("ClientUser", policy =>
                    policy.RequireRole("ClientUser"));
            });

            // Register services
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            // Register authorization handlers
            services.AddScoped<IAuthorizationHandler, WarehouseAccessHandler>();
            services.AddScoped<IAuthorizationHandler, ClientAccessHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            // Add custom policy provider
            services.AddSingleton<IAuthorizationPolicyProvider, WMSAuthorizationPolicyProvider>();

            return services;
        }
    }
}