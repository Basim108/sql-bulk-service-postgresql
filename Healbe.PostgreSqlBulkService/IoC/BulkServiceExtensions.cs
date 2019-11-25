using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hrimsoft.PostgresSqlBulkService
{
    /// <summary>
    /// Methods to configure <see cref="NpgsqlCommandsBulkService"/>
    /// </summary>
    public static class BulkServiceExtensions
    {
        /// <summary>
        /// Registers and configure bulk service
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction">Bulk Service configuration such as mapping</param>
        /// <returns></returns>
        public static IServiceCollection AddPostgreSqlBulkService([NotNull] this IServiceCollection services, Action<BulkServiceOptions> setupAction)
        {
            var options = new BulkServiceOptions();
            setupAction(options);
            
            services.AddSingleton<IPostgreSqlBulkService>(
                sp => new NpgsqlCommandsBulkService(sp.GetRequiredService<ILoggerFactory>(), options));
            
            return services;
        }
    }
}