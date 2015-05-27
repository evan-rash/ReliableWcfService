using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableWcfService.Demo
{
    public class DemoWcfService : System.ServiceModel.ReliableWcfService<ServiceReference1.IDemoService>
    {
        protected override System.ServiceModel.ICommunicationObject CreateClientBase()
        {
            return new ServiceReference1.DemoServiceClient();
        }
    }
}
