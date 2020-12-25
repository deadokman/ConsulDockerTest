using System;
using System.Collections.Generic;
using System.Linq;

using Consul;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.CompilerServices;

using Msc.ConsulServiceDiscovery.Layer.Interfaces;
using Msc.ConsulServiceDiscovery.Layer.Registrator;
using Msc.Microservice.Core.Standalone.Interfaces;
using Msc.Microservice.Core.Standalone.Layering;

namespace Msc.ConsulServiceDiscovery.Layer
{
    public class ConsulLayer : IRunnableLayer
    {
        /// <summary>Выполнить регистрацию и валидацию конфигураций.</summary>
        /// <param name="configurationRoot">Валидация конфигураций.</param>
        public void RegisterConfiguration(IConfigurationBuilder configurationRoot)
        {
            return;
        }

        /// <summary>Выполнить регистрацию и валидацию конфигураций.</summary>
        /// <param name="configurationRoot">Валидация конфигураций.</param>
        /// <param name="serviceCollection">Коллекция служб.</param>
        /// <returns>Список ошибок во время конфигурирования.</returns>
        public IEnumerable<string> RegisterLayer(IConfigurationRoot configurationRoot, IServiceCollection serviceCollection)
        {
            var configurationSection = configurationRoot.GetSection("Consul");

            var registerErrors = new List<string>();
            if (configurationSection == null)
            {
                registerErrors.Add($"Consul section required in application configuration file");
            }

            var consulOptions = configurationSection.Get<ConsulOptions>();
            var validateError = consulOptions.ValidateErrors().ToArray();
            if (validateError.Any())
            {
                registerErrors.AddRange(validateError);
            }

            serviceCollection.Configure<ConsulOptions>(configurationSection);

            var uriBuilder = new UriBuilder(consulOptions.Scheme, consulOptions.ConsulHost, consulOptions.ConsulPort);

            // register consul client
            serviceCollection.AddSingleton<IConsulClient>((p) => new ConsulClient(
                cfg =>
                    {
                        cfg.Token = consulOptions.Token;
                        cfg.Address = uriBuilder.Uri;
                        cfg.Datacenter = consulOptions.Datacenter;
                    }));

            serviceCollection.AddSingleton<IConsulRegistrator, ConsulRegistrator>();
            return registerErrors;
        }

        /// <summary>Запустить выполннение операций в слое асинхронно.</summary>
        /// <param name="serviceProvider">Провайдер служб.</param>
        public void RunAsync(IServiceProvider serviceProvider)
        {
            var consulRegistrator = serviceProvider.GetService<IConsulRegistrator>();
            consulRegistrator.Register();
            consulRegistrator.ListenHealthcheck();
        }

        /// <summary>Отключить работу службы.</summary>
        /// <param name="serviceProvider">Провайдер служб.</param>
        public void Shutdown(IServiceProvider serviceProvider)
        {
            var consulRegistrator = serviceProvider.GetService<IConsulRegistrator>();
            consulRegistrator.Unregister();
            consulRegistrator.StopListenHealthcheck();
        }
    }
}
