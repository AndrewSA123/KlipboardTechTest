using System.Net.Http.Headers;
using FaultTriage.Core;
using FaultTriage.Infrastructure.Configuration;
using FaultTriage.Infrastructure.FaultAnalysers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FaultTriage.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGroqFaultAnalyser(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));

        services.AddHttpClient<IFaultAnalyser, GroqFaultAnalyser>(client =>
        {
            var options = configuration.GetSection(GroqOptions.SectionName).Get<GroqOptions>() ?? new GroqOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}