using System.Globalization;

namespace DAS.DigitalEngagement.Application.Extensions;

public static class DateTimeExtensions
{
    public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public static string ToODataDateString(this DateTimeOffset value)
    {
        return value.ToUniversalTime()
            .ToString(
                "yyyy-MM-ddTHH:mm:ss'Z'",
                CultureInfo.InvariantCulture);
    }
}
