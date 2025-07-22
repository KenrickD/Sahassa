using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using WMS.Application;
using WMS.Application.Extensions;
using WMS.Application.Helpers;
using WMS.Application.Hubs;
using WMS.Application.Interfaces;
using WMS.Application.Options;
using WMS.Application.Services;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Data.Seeders;
using WMS.WebApp.Middlewares;

namespace WMS.WebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Configure Serilog with settings from appsettings.json
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();

            try
            {
                Log.Information("Starting WMS application");
                var builder = WebApplication.CreateBuilder(args);


                // Add Serilog to the application
                builder.Host.UseSerilog();

                //AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

                // Add services to the container.
                //builder.Services.AddControllersWithViews();
                builder.Services.AddControllersWithViews(options =>
                {
                    //Global HTTPS enforcement
                    //options.Filters.Add(new RequireHttpsAttribute());

                    //Global anti-forgery token validation
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                });

                // Configure security headers
                builder.Services.AddAntiforgery(options =>
                {
                    options.HeaderName = "X-CSRF-TOKEN";
                    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.HttpOnly = true;
                });

                // Configure HSTS
                //builder.Services.AddHsts(options =>
                //{
                //    options.Preload = true;
                //    options.IncludeSubDomains = true;
                //    options.MaxAge = TimeSpan.FromDays(365);
                //});

                // Configure forwarded headers for reverse proxy scenarios
                //builder.Services.Configure<ForwardedHeadersOptions>(options =>
                //{
                //    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                //});

                // Add database context
                builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

                    // Disable sensitive data logging in production to avoid exposing data in logs
                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging(false);
                    }
                },ServiceLifetime.Scoped);

                // Add session for storing tokens
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(240);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

                // Add cookie authentication
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/Account/Login";
                        options.LogoutPath = "/Account/Logout";
                        options.AccessDeniedPath = "/Account/AccessDenied";
                        options.ExpireTimeSpan = TimeSpan.FromMinutes(240);
                        options.SlidingExpiration = true;
                        options.Cookie.Name = "WMS.Auth";
                        options.Cookie.HttpOnly = true;
                        options.ReturnUrlParameter = "returnUrl";

                        // Add event handlers for authentication events (for logging purposes)
                        options.Events = new CookieAuthenticationEvents
                        {
                            OnValidatePrincipal = context =>
                            {
                                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                                var username = context.Principal?.FindFirstValue(ClaimTypes.Name);

                                if (!string.IsNullOrEmpty(username))
                                {
                                    Log.Information("User {UserId} ({Username}) authenticated successfully", userId, username);
                                }

                                return Task.CompletedTask;
                            },
                            OnSignedIn = context =>
                            {
                                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                                var username = context.Principal?.FindFirstValue(ClaimTypes.Name);

                                Log.Information("User {UserId} ({Username}) signed in", userId, username);
                                return Task.CompletedTask;
                            },
                            OnSigningOut = context =>
                            {
                                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                                var username = context.HttpContext.User.FindFirstValue(ClaimTypes.Name);

                                Log.Information("User {UserId} ({Username}) signed out", userId, username);
                                return Task.CompletedTask;
                            }
                        };
                    });

                // Add authorization AFTER authentication
                //builder.Services.AddAuthorization(options =>
                //{
                //    // Set a default policy that requires authentication
                //    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                //        .RequireAuthenticatedUser()
                //        .Build();
                //});
                // Configure JWT Authentication for service tokens
                builder.Services.AddAuthentication()
                .AddJwtBearer("ServiceToken", options =>
                {
                    var secretKey = builder.Configuration["ServiceJwt:SecretKey"];
                    var issuer = builder.Configuration["ServiceJwt:Issuer"];
                    var audience = builder.Configuration["ServiceJwt:Audience"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                });

                // Add authorization policies
                builder.Services.AddAuthorization(options =>
                {
                    // Default policy for web pages
                    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();

                    // Service token policies
                    options.AddPolicy("ServiceToken", policy =>
                    {
                        policy.AuthenticationSchemes.Add("ServiceToken");
                        policy.RequireClaim("service");
                    });

                    options.AddPolicy("ServiceToken.location-update", policy =>
                    {
                        policy.AuthenticationSchemes.Add("ServiceToken");
                        policy.RequireClaim("service");
                        policy.RequireClaim("permission", "location-update");
                    });
                });

                // Add SignalR services
                builder.Services.AddSignalR(options =>
                {
                    // Configure SignalR options
                    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1); // Client must send keepalive within 1 minute
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);    // Server sends keepalive every 15 seconds
                    options.HandshakeTimeout = TimeSpan.FromSeconds(15);     // Handshake timeout
                    options.MaximumReceiveMessageSize = 32 * 1024;          // 32KB max message size
                });

                builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

                // Add HttpContextAccessor
                builder.Services.AddHttpContextAccessor();

                // Add WMS Authorization services (modified for MVC)
                builder.Services.AddWMSAuthorization(builder.Configuration);

                builder.Services.AddGeneralServices(builder.Configuration);
                builder.Services.AddAwsS3Service(builder.Configuration);

                // Register logging middleware
                builder.Services.AddSingleton<LoggingMiddleware>();
                builder.Services.AddSingleton<ToastService>();

                //memory cache
                builder.Services.AddMemoryCache();
                builder.Services.AddHttpClient();
                builder.Services.AddScoped<WMS.Application.Helpers.MemoryHelper>();

                builder.Services.Configure<GivaudanApiOptions>(
                configuration.GetSection("GivaudanApi"));


                //added due to auth service used in both web app and api
                builder.Services.AddScoped<JWTHelper>();
                builder.Services.AddAutoMapper(typeof(DtoAutoMapperProfile));
                builder.Services.AddScoped<IRawMaterialService,RawMaterialService>();
                builder.Services.AddScoped<IFinishedGoodService, FinishedGoodService>();
                builder.Services.AddScoped<IContainerService, ContainerService>();
                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    Log.Information("Running in Development environment");
                }
                else
                {
                    app.UseStatusCodePagesWithReExecute("/Error/Error/{0}");
                    app.UseExceptionHandler("/Error/Error");
                    Log.Information("Running in Production environment");
                }

                app.UseHsts();
                //app.UseHttpsRedirection();
                app.UseStaticFiles();
                // Security middleware - ORDER MATTERS!
                //app.UseForwardedHeaders();
                //app.UseHttpsRedirection();

                // Custom security headers middleware
                //app.UseMiddleware<SecurityHeadersMiddleware>();

                //app.UseStaticFiles(new StaticFileOptions
                //{
                //    OnPrepareResponse = ctx =>
                //    {
                //        // Add security headers for static files
                //        ctx.Context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                //        ctx.Context.Response.Headers.Add("X-Frame-Options", "DENY");
                //    }
                //});
                app.UseRouting();

                // Add request logging middleware
                app.UseMiddleware<LoggingMiddleware>();

                app.UseSession();
                app.UseAuthentication();
                app.UseAuthorization();

                // Rate limiting middleware (add after authentication)
                //app.UseMiddleware<RateLimitingMiddleware>();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Seed authorization data
                using (var scope = app.Services.CreateScope())
                {
                    Log.Information("Seeding authorization data");
                    await AuthorizationSeeder.SeedAuthorizationDataAsync(scope.ServiceProvider);
                    Log.Information("Authorization data seeding completed");
                }

                // Map SignalR hub
                app.MapHub<WarehouseHub>("/warehouseHub", options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;

                    // Configure CORS for SignalR if needed
                    options.AllowStatefulReconnects = true;
                });


                Log.Information("WMS application started successfully");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
