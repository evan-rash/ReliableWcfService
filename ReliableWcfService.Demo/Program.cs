using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace ReliableWcfService.Demo
{
    class Program
    {
        public static void Main()
        {
            Task t = DemoAsync();
            t.Wait();
        }

        private static async Task AnnoyingExampleAsync()
        {
            var proxy = new ServiceReference1.DemoServiceClient();
            try
            {
                string data = await proxy.GetSampleDataAsync();
                proxy.Close();
            }
            catch(CommunicationException)
            {
                proxy.Abort();
            }
            catch (TimeoutException)
            {
                proxy.Abort();
            }

        }

        private static async Task BadExampleAsync()
        {
            using (var proxy = new ServiceReference1.DemoServiceClient())
            {
                string data = await proxy.GetSampleDataAsync();
            }
        }

        private static async Task DemoAsync()
        {
            Uri baseAddress = new Uri("http://localhost:8080/demo");

            // Create the ServiceHost.
            using (ServiceHost host = new ServiceHost(typeof(DemoService), baseAddress))
            {
                // Enable metadata publishing.
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                host.Description.Behaviors.Add(smb);

                // Open the ServiceHost to start listening for messages. Since
                // no endpoints are explicitly configured, the runtime will create
                // one endpoint per base address for each service contract implemented
                // by the service.
                host.Open();

                Console.WriteLine("The service is ready at {0}", baseAddress);

                Console.WriteLine("Invoking GetSampleData...");

                DemoWcfService demoService = new DemoWcfService();
                string sampleData = await demoService.GetAsync(proxy => proxy.GetSampleDataAsync());

                Console.WriteLine("Result: {0}", sampleData);
              

                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();

                // Close the ServiceHost.
                host.Close();
            }
        }

    }
}
