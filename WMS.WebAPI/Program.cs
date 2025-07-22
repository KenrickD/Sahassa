using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Reflection;
using System.Threading.RateLimiting;
using WMS.Application;
using WMS.Application.Extensions;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Data;
using WMS.WebAPI.Extensions;
using WMS.WebAPI.Middleware;

namespace WMS.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //// Configure Serilog
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            //    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            //    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            //    .Enrich.FromLogContext()
            //    .Enrich.WithProperty("ApplicationName", "WMS.API")
            //    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
            //    .Enrich.WithMachineName()
            //    .WriteTo.Console(new JsonFormatter())
            //    .WriteTo.File(
            //        path: "logs/wms-api-.log",
            //        rollingInterval: RollingInterval.Day,
            //        retainedFileCountLimit: 30,
            //        fileSizeLimitBytes: 100_000_000,
            //        rollOnFileSizeLimit: true)
            //    .WriteTo.File(
            //        path: "logs/wms-security-.log",
            //        restrictedToMinimumLevel: LogEventLevel.Warning,
            //        formatter: new JsonFormatter(),
            //        rollingInterval: RollingInterval.Day,
            //        retainedFileCountLimit: 90)
            //    .CreateLogger();
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

            Log.Information("Starting WMS API V1 application");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                //var configuration = new ConfigurationBuilder()
                //.SetBasePath(builder.Environment.ContentRootPath)
                //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                //.Build();

                // Add Serilog
                builder.Host.UseSerilog();

                // Add services using extensions
                builder.Services.AddDatabaseServices(builder.Configuration);
                builder.Services.AddAuthenticationServices(builder.Configuration);
                builder.Services.AddApiServices(builder.Configuration);
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddServices(builder.Configuration);
                builder.Services.AddRateLimitingServices();
                builder.Services.AddHttpsServices();
                builder.Services.AddHttpClient();
                builder.Services.AddMemoryCache();
                //Add Services
                builder.Services.AddAutoMapper(typeof(DtoAutoMapperProfile));
                builder.Services.AddScoped<IRawMaterialService, RawMaterialService>();
                builder.Services.AddScoped<IContainerService, ContainerService>();
                builder.Services.AddScoped<PDFHelper>();
                builder.Services.AddScoped<IFinishedGoodService, FinishedGoodService>();
                builder.Services.AddScoped<IPalletService, PalletService>();
                builder.Services.AddScoped<ILocationService, LocationService>();
                builder.Services.AddScoped<IGeneralCodeService, GeneralCodeService>();
                builder.Services.Configure<AwsS3Config>(builder.Configuration.GetSection("AWS"));

                builder.Services.AddAwsS3Service(builder.Configuration);

                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(configuration.GetValue<string>("ConnectionStrings:DefaultConnection")!));
                // Add Swagger
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "WMS API",
                        Version = "v1.0",
                        Description = "Warehouse Management System REST API - Version 1.0",
                        Contact = new OpenApiContact
                        {
                            Name = "WMS Development Team",
                            Email = "arif@hcs.sg"
                        }
                    });

                    // JWT Authentication
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Enter your JWT token (without 'Bearer' prefix).",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer", // must be lower case
                        BearerFormat = "JWT",
                        Reference = new OpenApiReference
                        {
                            Id = JwtBearerDefaults.AuthenticationScheme,
                            Type = ReferenceType.SecurityScheme
                        }
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                },
                                Scheme = "oauth2",
                                Name = "Bearer",
                                In = ParameterLocation.Header,
                            },
                            new List<string>()
                        }
                    });

                    // Include XML comments
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                });
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowLocalhost7282", builder =>
                    {
                        builder.WithOrigins("https://localhost:7282")
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
                });
                var app = builder.Build();

                var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") ?? "/api";
                app.UsePathBase(pathBase);

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint($"{pathBase}/swagger/v1/swagger.json", "WMS API V1.0");
                    //c.SwaggerEndpoint("/swagger/v1/swagger.json", "WMS API V1.0");
                    c.RoutePrefix = "swagger";
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                    c.DefaultModelsExpandDepth(-1);
                    c.DisplayRequestDuration();
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.ShowExtensions();
                    c.EnableValidator();
                    c.SupportedSubmitMethods(Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                                            Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                                            Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                                            Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete,
                                            Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Patch);
                });

                // Middleware
                app.UseMiddleware<RequestLoggingMiddleware>();
                app.UseMiddleware<SecurityHeadersMiddleware>();
                app.UseMiddleware<GlobalExceptionMiddleware>();

                //app.UseCors("ApiCorsPolicy");
                app.UseRateLimiter();

                

                app.UseCors("AllowLocalhost7282");


                // Auth
                app.UseAuthentication();
                app.UseAuthorization();

                // Controllers
                app.MapControllers();

                // Health checks
                app.MapHealthChecks("/health");

                Log.Information("WMS API V1 configured successfully");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "WMS API V1 application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}