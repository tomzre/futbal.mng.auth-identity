using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace futbal.mng.auth_identity.Services
{
    public class CustomChannelFactory 
    {
        public Task<IModel> CreateChannelAsync(CancellationToken token = default)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}