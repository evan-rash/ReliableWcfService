using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ReliableWcfService.Demo
{
    [ServiceContract]
    public interface IDemoService
    {
        [OperationContract]
        Task<string> GetSampleDataAsync();

        [OperationContract]
        Task PerformSampleOperationAsync(string parameter);
    }

    public class DemoService : IDemoService
    {
        public async Task<string> GetSampleDataAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return "This is a demo!";
        }

        public async Task PerformSampleOperationAsync(string parameter)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
