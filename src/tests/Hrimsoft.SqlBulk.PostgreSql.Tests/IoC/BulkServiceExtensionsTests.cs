using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.IoC
{
    public class BulkServiceExtensionsTests
    {
        private ServiceProvider _serviceProvider;
        
        [SetUp]
        public void SetUp()
        {
            _serviceProvider = new ServiceCollection()
                .AddPostgreSqlBulkService(options => { })
                .AddLogging()
                .BuildServiceProvider();
        }
        
        [Test]
        public void Should_register_IPostgreSqlBulkService()
        {
            var bulkService = _serviceProvider.GetService<IPostgreSqlBulkService>();
            Assert.NotNull(bulkService);
        }
    }
}