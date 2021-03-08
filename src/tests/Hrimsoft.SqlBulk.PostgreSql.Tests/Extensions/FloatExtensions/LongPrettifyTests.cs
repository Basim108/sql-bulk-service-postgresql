using NUnit.Framework;

namespace Hrimsoft.SqlBulk.PostgreSql.Tests.Extensions
{
    public class LongPrettifyTests
    {
        [Test]
        public void ShouldPrettifyToMb()
        {
            var test = 1024L * 1024 * 2;
            var (actualSize, actualSuffix) = test.PrettifySize();
            Assert.AreEqual(2, actualSize);
            Assert.AreEqual("Mb", actualSuffix);
        }

        [Test]
        public void ShouldPrettifyToBytes()
        {
            var (actualSize, actualSuffix) = 1023L.PrettifySize();
            Assert.AreEqual(1023, actualSize);
            Assert.AreEqual("bytes", actualSuffix);
        }

        [Test]
        public void ShouldPrettifyToKb()
        {
            var (actualSize, actualSuffix) = 2048L.PrettifySize();
            Assert.AreEqual(2, actualSize);
            Assert.AreEqual("Kb", actualSuffix);
        }

        [Test]
        public void ShouldPrettifyToGb()
        {
            var test = 1024L * 1024 * 1024 * 2;
            var (actualSize, actualSuffix) = test.PrettifySize();
            Assert.AreEqual(2, actualSize);
            Assert.AreEqual("Gb", actualSuffix);
        }

        [Test]
        public void ShouldPrettifyToTb()
        {
            var test = 1024L * 1024 * 1024 * 1024 * 2;
            var (actualSize, actualSuffix) = test.PrettifySize();
            Assert.AreEqual(2, actualSize);
            Assert.AreEqual("Tb", actualSuffix);
        }
    }
}