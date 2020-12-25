using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Msc.ConsulServiceDiscovery.Layer
{
    /// <summary>
    /// Options for consul registration
    /// </summary>
    public class ConsulOptions
    {
        /// <summary>
        /// Consul host
        /// </summary>
        [Required]
        public string ConsulHost { get; set; }

        /// <summary>
        /// Access token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Access scheme (http by default)
        /// </summary>
        public string Scheme { get; set; } = "http";

        /// <summary>
        /// Consul datacenter (null by default)
        /// </summary>
        public string Datacenter { get; set; }

        /// <summary>
        /// Consul port
        /// </summary>
        [Required]
        public int ConsulPort { get; set; }

        /// <summary>
        /// Service health check port (default 8888)
        /// </summary>
        public int HealthCheckPort { get; set; } = 8888;

        /// <summary>
        /// Health check interval (300 by default)
        /// </summary>
        public int HealthCheckIntervalMsec { get; set; } = 300;

        /// <summary>
        /// Timeout before service fill be removed from consul
        /// if health check failed (900 by default)
        /// </summary>
        public int UnregisterTimeoutMsec { get; set; } = 900;
    }
}
