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

            Assert.That(await checker.IsValidDomainAsync("@"), Is.False);
            Assert.That(await checker.IsValidDomainAsync("localpart@"), Is.False);
            Assert.That(await checker.IsValidDomainAsync("not-a-real-domain-should-not-exist-xyz-12345"), Is.False);
        }

        [Test]
        public async Task IsValidDomainAsync_KnownValidDomain_ReturnsTrue()
        {
            var checker = new EmailDomainChecker();

            var result = await checker.IsValidDomainAsync("test@gmail.com");
            Assert.That(result, Is.True, "Expected gmail.com to have MX records.");
        }

        [Test]
        public async Task IsValidDomainAsync_LookupThrows_CachesFalseAndReturnsFalse()
        {
            var checker = new EmailDomainChecker();

            var lookupField = typeof(EmailDomainChecker).GetField("_lookupClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(lookupField, Is.Not.Null, "Unable to locate private field '_lookupClient' via reflection.");
            lookupField.SetValue(checker, null);

            var testEmail = "user@domain-that-will-cause-exception.test";

            var first = await checker.IsValidDomainAsync(testEmail);

            Assert.That(first, Is.False, "Expected IsValidDomainAsync to return false when lookup throws.");

            var second = await checker.IsValidDomainAsync(testEmail);

            Assert.That(second, Is.False, "Expected cached value (false) to be returned on subsequent calls.");
        }
    }
}