using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hrimsoft.SqlBulk.PostgreSql
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
        public static IServiceCollection AddPostgreSqlBulkService(this IServiceCollection services, Action<BulkServiceOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            var options = new BulkServiceOptions();
            setupAction(options);

            services.AddSingleton<IBulkServiceOptions>(options);
            services.AddTransient<IInsertSqlCommandBuilder, InsertSqlCommandBuilder>();
            services.AddTransient<IUpdateSqlCommandBuilder, UpdateSqlCommandBuilder>();
            services.AddTransient<IDeleteSqlCommandBuilder, DeleteSqlCommandMediator>();
            services.AddTransient<SimpleDeleteSqlCommandBuilder>();
            services.AddTransient<WhereInDeleteSqlCommandBuilder>();
            services.AddTransient<IUpsertSqlCommandBuilder, UpsertSqlCommandBuilder>();
            
            services.AddTransient<IPostgreSqlBulkService>(
                sp => new NpgsqlCommandsBulkService(
                    options,
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<IInsertSqlCommandBuilder>(),
                    sp.GetRequiredService<IUpdateSqlCommandBuilder>(),
                    sp.GetRequiredService<IDeleteSqlCommandBuilder>(),
                    sp.GetRequiredService<IUpsertSqlCommandBuilder>()));
            
            return services;
        }
    }
}