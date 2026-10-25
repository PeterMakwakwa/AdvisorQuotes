namespace Quotes.Api.Networking
{
    /// <summary>
    /// The "ForwardedHeaders" section. Behind a load balancer (the ALB in AWS), enable this so the client IP used by rate
    /// limiting and logs is the real client, not the load balancer. Only proxies listed here (plus loopback) are trusted:
    /// accepting X-Forwarded-For from anyone would let a client choose its own IP.
    /// </summary>
    public sealed class ForwardedHeadersSettings
    {
        public const string SectionName = "ForwardedHeaders";

        public bool Enabled { get; init; }

        /// <summary>
        /// Individual proxy addresses to trust, for example "10.0.1.15".
        /// </summary>
        public IReadOnlyList<string> KnownProxies { get; init; } = [];

        /// <summary>
        /// Networks to trust in CIDR notation, for example the VPC range "10.0.0.0/16".
        /// </summary>
        public IReadOnlyList<string> KnownNetworks { get; init; } = [];

        /// <summary>
        /// How many proxy hops to unwind. One for a single load balancer.
        /// </summary>
        public int ForwardLimit { get; init; } = 1;
    }
}
