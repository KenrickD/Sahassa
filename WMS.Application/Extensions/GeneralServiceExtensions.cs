using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.Interfaces;

namespace WMS.Application.Extensions
{
    public static class GeneralServiceExtensions
    {
        public static IServiceCollection AddGeneralServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IDateTime, DateTimeService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IZoneService, ZoneService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IGeneralCodeService, GeneralCodeService>();
            services.AddScoped<IWarehouseNotificationService, WarehouseNotificationService>();
            services.AddScoped<IAPIService, APIService>();
            services.AddScoped<LocationGridHelper>();
            services.AddScoped<PDFHelper>();

            return services;
        }
    }
}
