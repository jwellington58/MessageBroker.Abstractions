using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Abstractions
{
    public static class ConfigMessageBrokerExtensions
    {
        public static void AddBroker(this IServiceCollection services, Action<MessageBrokerOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<IMessageBroker, AzureMessageBroker>();
        }
    }
}
