# ReliableWcfService

A reliable wrapper around a WCF client proxy that provides seamless retrying, transient fault handling, and automatically closes or aborts the proxy as appropriate.

To use the ReliableWcfService, simply derive from the abstract ReliableWcfService class

```c#
public class DemoWcfService : System.ServiceModel.ReliableWcfService<ServiceReference1.IDemoService>
{
    protected override System.ServiceModel.ICommunicationObject CreateClientBase()
    {
        return new ServiceReference1.DemoServiceClient();
    }
}
```
Now you can host calls to the DemoService proxy within the DemoWcfService:

```c#
DemoWcfService demoService = new DemoWcfService();
string sampleData = await demoService.GetAsync(proxy => proxy.GetSampleDataAsync());
```

## Transient Fault Handling & Retrying

WCF service proxies throw many different types of transient errors. ReliableWcfService automatically catches these transient exceptions and transparently retries.

* FaultException
* ChannelTerminatedException
* EndpointNotFoundException
* ServerTooBusyException
* TimeoutException

## Automatically Closing & Aborting Proxies

WCF service proxies don't support the standard ```using()``` pattern. ReliableWcfService handles all of this for you!

The following vanilla WCF code will fail:

```c#
using (var proxy = new ServiceReference1.DemoServiceClient())
{
  string data = await proxy.GetSampleDataAsync();
}
```
Instead, you have to manually call ```Close()``` and ```Abort()```, which adds unnecessary boilerplate:

```c#
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
```




