using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;

namespace ProxyYARP.Miscellaneous;

public sealed class RateLimiterPolicyByIPAddress : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        string ipAddress = getRemoteIpAddress(httpContext);

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress, partKey =>
        {
            return new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 3,
                Window               = TimeSpan.FromSeconds(5),
                SegmentsPerWindow    = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 5
            };
        });
    }

    private static string getRemoteIpAddress(HttpContext httpContext)
    {
        IHeaderDictionary headers = httpContext.Request.Headers;

        string? ipAddress = getIpAddressFromHeader(headers["X-Real-IP"]);

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            return ipAddress;
        }

        ipAddress = getIpAddressFromHeader(headers["X-Forwarded-For"]);

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            return ipAddress;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "n/a";
    }

    private static string? getIpAddressFromHeader(StringValues headerValues)
    {
        foreach (ReadOnlySpan<char> headerValue in headerValues)
        {
            if (headerValue.IsEmpty)
            {
                continue;
            }

            var ranges = new Span<Range>();

            int splitCount = headerValue.Split(ranges, ',', StringSplitOptions.RemoveEmptyEntries);

            if (splitCount == 0)
            {
                return parseIPAddress(headerValue);
            }

            foreach (Range range in ranges)
            {
                string? ipAddress = parseIPAddress(headerValue[range]);

                if (ipAddress is not null)
                {
                    return ipAddress;
                }
            }
        }

        return null;
    }

    private static string? parseIPAddress(ReadOnlySpan<char> ipValue)
    {
        ReadOnlySpan<char> trimmedIp = ipValue.Trim();

        if (IPAddress.TryParse(trimmedIp, out IPAddress? address) &&
            address is { AddressFamily: AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 })
        {
            return address.ToString();
        }

        return null;
    }
}