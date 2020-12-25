using System;
using System.Collections.Generic;
using System.Text;

namespace Msc.ConsulServiceDiscovery.Layer.Interfaces
{
    /// <summary>
    /// Cinsul service registrator interface
    /// </summary>
    public interface IConsulRegistrator
    {
        /// <summary>
        /// Listen for TCP consul health check
        /// </summary>
        void ListenHealthcheck();

        /// <summary>
        /// Stop health check listening
        /// </summary>
        void StopListenHealthcheck();

        /// <summary>
        /// Registrates current service instance in consul
        /// </summary>
        void Register();

        /// <summary>
        /// Unregister current service instance from consul
        /// </summary>
        void Unregister();
    }
}
