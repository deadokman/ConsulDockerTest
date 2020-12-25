using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Consul;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Msc.ConsulServiceDiscovery.Layer.Interfaces;

namespace Msc.ConsulServiceDiscovery.Layer.Registrator
{
    /// <summary>
    /// Execution consul service registrations
    /// </summary>
    public class ConsulRegistrator : IConsulRegistrator
    {
        private IConsulClient _consulClient;
        private readonly ConsulOptions _opts;
        private ILogger<IConsulRegistrator> _logger;
        private string _registrationId;
        private TcpListener _tcpListner;
        private IPAddress _localIpAddress;

        /// <summary>
        /// Initialize new instance of class
        /// </summary>
        /// <param name="consulClient"><Consul client</param>
        /// <param name="opts"></param>
        /// <param name="logger">Logger</param>
        public ConsulRegistrator(IConsulClient consulClient, IOptions<ConsulOptions> opts, ILogger<IConsulRegistrator> logger)
        {
            _consulClient = consulClient ?? throw new ArgumentNullException(nameof(consulClient));
            _opts = opts.Value ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Listen for TCP consul health check
        /// </summary>
        public void ListenHealthcheck()
        {
            if (string.IsNullOrEmpty(_registrationId) || _localIpAddress == null)
            {
                throw new NotSupportedException("Service registration failed or could not get local ip address");
            }

            try
            {
                // TcpListener server = new TcpListener(port);
                _tcpListner = new TcpListener(_localIpAddress, _opts.HealthCheckPort);

                // Start listening for client requests.
                _tcpListner.Start();

                // Buffer for reading data
                var bytes = new byte[256];
                string data = null;

                _logger.LogInformation($"Healthcheck waiting tcp connection for on {_localIpAddress}:{_opts.HealthCheckPort}");
                // Enter the listening loop.
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    var client = _tcpListner.AcceptTcpClient();
                    // Get a stream object for reading and writing
                    var stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = Encoding.ASCII.GetString(bytes, 0, i);

                        // Process the data sent by the client.
                        data = data.ToUpper();

                        var msg = Encoding.ASCII.GetBytes(data);

                        // Send back a response.
                        stream.Write(msg, 0, msg.Length);
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                _logger.LogError("TcpListener: SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                _tcpListner.Stop();
            }
        }

        /// <summary>
        /// Stop health check listening
        /// </summary>
        public void StopListenHealthcheck()
        {
            _tcpListner.Stop();
        }

        /// <summary>
        /// Registrates current service instance in consul
        /// </summary>
        public void Register()
        {
            _logger.LogInformation($"Trying to register in Consul address ({_opts.Scheme}): {_opts.ConsulHost}:{_opts.ConsulPort}]");
            _logger.LogInformation($"Resolving service host name in DNS");

            string serviceHostName;
            IPHostEntry hostEntry;
            try
            {
                serviceHostName = Dns.GetHostName();
                hostEntry = Dns.GetHostEntry(serviceHostName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while resolving service host name");
                throw ex;
            }

            var serviceChecks = new AgentServiceCheck[1];
            _localIpAddress = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            _logger.LogInformation($"Resolved HOST: [{hostEntry.HostName}] IP: [{_localIpAddress}]");
            var tcpAddess = $"{_localIpAddress}:{_opts.HealthCheckPort}";
            serviceChecks[0] = new AgentServiceCheck
                                  {
                                      Timeout = TimeSpan.FromMilliseconds(_opts.UnregisterTimeoutMsec),
                                      DeregisterCriticalServiceAfter = TimeSpan.FromMilliseconds(_opts.UnregisterTimeoutMsec),
                                      Interval = TimeSpan.FromMilliseconds(_opts.HealthCheckIntervalMsec),
                                      TCP = tcpAddess,
                                  };

            _logger.LogInformation($"Added healthcheck for service (TCP), checking addess=[{tcpAddess}].");
            var registration = new AgentServiceRegistration()
                                   {
                                       Checks = serviceChecks,
                                       ID = "Test",
                                       Name = "Test",
                                   };

            _registrationId = registration.ID;

            // _consulClient.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
            _consulClient.Agent.ServiceRegister(registration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Unregister current service instance from consul
        /// </summary>
        public void Unregister()
        {
            if (string.IsNullOrEmpty(_registrationId))
            {
                throw new NotSupportedException("Regisration id is null, service should be registrated");
            }

            _logger.LogInformation("Deregistering from Consul");
            try
            {
                _consulClient.Agent.ServiceDeregister(_registrationId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Deregisteration failed");
            }
        }
    }
}
