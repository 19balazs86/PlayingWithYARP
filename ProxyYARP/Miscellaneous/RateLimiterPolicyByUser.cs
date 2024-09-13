using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace ProxyYARP.Miscellaneous;

public sealed class RateLimiterPolicyByUser : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        string userName = httpContext.User.Identity?.Name ?? "n/a";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userName, partKey =>
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
}