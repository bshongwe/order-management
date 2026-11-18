using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
namespace OrderManagement.Worker;
public static class RabbitMqRegistration
{
    public static IServiceCollection AddRabbitMqConnection(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddSingleton<IConnection>(_=>
        {
            var factory=new ConnectionFactory
            {
                HostName=configuration["RabbitMq:Host"]??"localhost",
                Port=int.TryParse(configuration["RabbitMq:Port"],out var port)?port:5672,
                UserName=configuration["RabbitMq:Username"]??"guest",
                Password=configuration["RabbitMq:Password"]??"guest",
                DispatchConsumersAsync=true,
                AutomaticRecoveryEnabled=true,
                NetworkRecoveryInterval=TimeSpan.FromSeconds(10)
            };
            return factory.CreateConnection("order-management-worker");
        });
        return services;
    }
}