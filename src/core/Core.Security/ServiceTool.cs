using Microsoft.Extensions.DependencyInjection;
namespace Core.Utilities
{
    public static class ServiceTool
    {
        private static IServiceProvider? _serviceProvider;
        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider is null)
                throw new InvalidOperationException("ServiceProvider is not configured. Call ServiceTool.Configure() first.");

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
