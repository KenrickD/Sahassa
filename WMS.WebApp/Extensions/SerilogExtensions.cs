using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace WMS.WebApp.Extensions
{
    // Extension method for enrichers
    public static class SerilogExtensions
    {
        public static LoggerConfiguration WithClientIp(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<ClientIpEnricher>();
        }
    }

    // Client IP enricher
    public class ClientIpEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var httpContext = new HttpContextAccessor().HttpContext;

            if (httpContext == null)
                return;

            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(clientIp))
                return;

            var clientIpProperty = propertyFactory.CreateProperty("ClientIp", clientIp);
            logEvent.AddPropertyIfAbsent(clientIpProperty);
        }
    }
}
