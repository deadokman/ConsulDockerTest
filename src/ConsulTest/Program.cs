using System;
using System.Threading;

using Msc.ConsulServiceDiscovery.Layer;
using Msc.Microservice.Core.Standalone.Implementations;

namespace ConsulTest
{
    public class Program
    {

        public static MicroserviceCore msc = new MicroserviceCore("appsettings", "ASPNETCORE_ENVIRONMENT");

        public static void Main(string[] args)
        {
            msc.AddLayer(new ConsulLayer());
            msc.DoWork += Msc_OnDoWork;
            msc.Run();
        }

        private static void Msc_OnDoWork(IServiceProvider sp, CancellationToken stopservicetoken)
        {
            stopservicetoken.WaitHandle.WaitOne();
        }
    }
}
