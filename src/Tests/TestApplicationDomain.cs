using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPITests
{
    public class TestApplicationDomain : IDisposable
    {
        private ServiceProvider? serviceProvider = null;
        private readonly ServiceCollection services;
        public readonly IConfiguration configuration;

        public ServiceCollection Services
        {
            get
            {
                if (serviceProvider != null) throw new Exception("Uma vez consultado o ServiceProvider já não é possível manipular a coleção de serviços. Se for preciso registar algum serviço faça antes de aceder ao ServiceProvider.");
                return services;
            }
        }

        public void Dispose()
        {
            if (serviceProvider != null)
            {
                serviceProvider.Dispose();
                serviceProvider = null;
            }
        }

        public ServiceProvider ServiceProvider
        {
            get
            {
                if (serviceProvider == null)
                {
                    serviceProvider = services.BuildServiceProvider();
                }
                return serviceProvider;
            }
        }

        public TestApplicationDomain()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<TestApplicationDomain>()
                .AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();
            services = new ServiceCollection();
            this.configuration = configuration;
            services.AddScoped<IConfiguration>(f => configuration);
            Services.AddMemoryCache();
            Services.AddScoped<ICryptoService, AesCryptoService>();
        }
    }
}
