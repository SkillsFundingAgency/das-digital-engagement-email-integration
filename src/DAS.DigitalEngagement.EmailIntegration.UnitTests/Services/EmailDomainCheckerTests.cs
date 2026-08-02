using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Services;

namespace DAS.DigitalEngagement.Tests.Services
{
    [TestFixture]
    public class EmailDomainCheckerTests
    {
        [Test]
        public async Task IsValidDomainAsync_NullOrWhitespace_ReturnsFalse()
        {
            var checker = new EmailDomainChecker();

            Assert.That(await checker.IsValidDomainAsync(null), Is.False);
            Assert.That(await checker.IsValidDomainAsync(string.Empty), Is.False);
            Assert.That(await checker.IsValidDomainAsync("   "), Is.False);
        }

        [Test]
        public async Task IsValidDomainAsync_InvalidFormats_ReturnsFalse()
        {
            var checker = new EmailDomainChecker();

            // No domain part
            Assert.That(await checker.IsValidDomainAsync("@"), Is.False);
            Assert.That(await checker.IsValidDomainAsync("localpart@"), Is.False);
            // Treats whole string as domain when no '@' present; unlikely to have MX
            Assert.That(await checker.IsValidDomainAsync("not-a-real-domain-should-not-exist-xyz-12345"), Is.False);
        }

        [Test]
        public async Task IsValidDomainAsync_KnownValidDomain_ReturnsTrue()
        {
            var checker = new EmailDomainChecker();

            // Uses a widely-known domain that should have MX records.
            // This test requires network/DNS access in the execution environment.
            var result = await checker.IsValidDomainAsync("test@gmail.com");
            Assert.That(result, Is.True, "Expected gmail.com to have MX records.");
        }
    }
}