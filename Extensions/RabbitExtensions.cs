using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace futbal.mng.auth_identity.Extensions
{
    public static class RabbitExtensions
    {
        public static IServiceCollection AddRabbit(this IServiceCollection services)
        {
            var factory = new ConnectionFactory{ HostName = "localhost", Password = "guest", UserName= "guest", Port = 5672 };

            // services.AddTransient(typeof(ConnectionFactory));

            services.AddSingleton<IConnection>(factory.CreateConnection());
            services.AddTransient<IModel>(sp => {
                var connection = sp.GetRequiredService<IConnection>();
                return connection.CreateModel();
            });

            return services;
        }
    }
}