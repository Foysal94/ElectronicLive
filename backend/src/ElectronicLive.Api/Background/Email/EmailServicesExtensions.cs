using ElectronicLive.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Background.Email;

public static class EmailServicesExtensions
{
    public static IServiceCollection AddEmailDispatching(this IServiceCollection services, IConfiguration configuration)
    {
        var resendSection = configuration.GetSection(ResendOptions.SectionName);
        services.Configure<ResendOptions>(resendSection);

        var resendOptions = resendSection.Get<ResendOptions>() ?? new ResendOptions();
        var apiKey = configuration["Resend:ApiKey"] ?? configuration["Resend__ApiKey"] ?? resendOptions.ApiKey;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            services
                .AddHttpClient<IEmailDispatcher, ResendEmailDispatcher>(
                    (sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<ResendOptions>>().Value;
                        client.BaseAddress = new Uri(options.BaseUrl);
                    }
                )
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddTransient<IEmailDispatcher, LoggingEmailDispatcher>();
        }

        return services;
    }
}
