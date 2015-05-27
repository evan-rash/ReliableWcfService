using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.ServiceModel
{

    /// <summary>
    /// A reliable wrapper around a WCF ClientBase that provides seamless retrying and transient fault handling
    /// </summary>
    /// <typeparam name="TProxy">The WCF-generated proxy interface</typeparam>
    public abstract class ReliableWcfService<TProxy> where TProxy : class
    {
        /// <summary>
        /// Constructs a new ReliableService wrapper
        /// </summary>
        protected ReliableWcfService()
            : this(retryLimit: 5)
        {
        }

        /// <summary>
        /// Constructs a new ReliableService wrapper with the specific retry limit
        /// </summary>
        /// <param name="retryLimit">the maximum number of retry attempts</param>
        protected ReliableWcfService(int retryLimit)
        {
            RetryLimit = retryLimit;
        }

        /// <summary>
        /// Gets the maximum number of retry attempts
        /// </summary>
        protected readonly int RetryLimit;

        /// <summary>
        /// Constructs a new ClientBase object. This should be a newly instantiated WCF-generated ClientBase for TProxy
        /// </summary>
        /// <returns>A new ClientBase of TProxy</returns>
        protected abstract ICommunicationObject CreateClientBase();

        /// <summary>
        /// Determines the delay between successive retry attempts. The default implementation utilizes exponential delay in retries
        /// </summary>
        /// <param name="retryAttempt">The current retry attempt number</param>
        /// <returns>A TimeSpan representing the amount of time to delay</returns>
        protected virtual TimeSpan GetDelayBetweenAttempts(int retryAttempt)
        {
            return TimeSpan.FromSeconds(.5 * Math.Pow(2, retryAttempt - 1));
        }
        
        /// <summary>
        /// Determines whether the specified exception is a transient failure that should be retried
        /// </summary>
        /// <param name="x">The exception</param>
        /// <returns>True if the exception is transient</returns>
        protected virtual bool IsTransientFailure(Exception x)
        {
            if (x is FaultException)
                return false;
            
            // The following is typically thrown on the client when a channel is terminated due to the server closing the connection.
            if (x is ChannelTerminatedException)
                return true;
           
            // The following is thrown when a remote endpoint could not be found or reached.  The endpoint may not be found or 
            // reachable because the remote endpoint is down, the remote endpoint is unreachable, or because the remote network is unreachable.
            if (x is EndpointNotFoundException)
                return true;

            // The following exception is thrown when a server is too busy to accept a message.
            if (x is ServerTooBusyException)
                return true;

            if (x is TimeoutException)
                return true;

            if (x is CommunicationException)
                return true;

            return false;
        }
        
        /// <summary>
        /// Wraps the specified failure exceptions in a new parent exception
        /// </summary>
        protected virtual Exception CreateExceptionAfterFailedAttempts(IEnumerable<Exception> failedAttempts)
        {
            return new AggregateException("Service call failed after " + failedAttempts.Count() + " attempts.", failedAttempts);
        }
        
        /// <summary>
        /// Extensibility point to enable wrapping the specified non-transient error in a new exception
        /// </summary>
        protected virtual Exception CreateExceptionAfterNonTransientError(Exception error)
        {
            return error;
        }

        private async Task<object> InvokeCore(InvocationOptions<TProxy> invocationOptions)
        {
            if (invocationOptions == null)
                throw new ArgumentNullException("invocationOptions");

            List<Exception> failures = new List<Exception>();
            
            for (int attempt = 1; attempt <= RetryLimit; attempt++)
            {
                invocationOptions.CancellationToken.ThrowIfCancellationRequested();

                TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();

                var communicationObject = CreateClientBase() as ICommunicationObject;
                var proxy = communicationObject as TProxy;

                if (proxy == null)
                    throw new InvalidOperationException("CreateClientBase() must return a proxy of type " + typeof(TProxy).FullName);

                try
                {
                    object result = null;

                    if (invocationOptions.AsynchronousInvocation != null)
                        result = await invocationOptions.AsynchronousInvocation(proxy);
                    else
                        result = await TaskEx.Run(() => invocationOptions.SynchronousInvocation(proxy));

                    //Begin closing the communication object
                    communicationObject.BeginClose(res => { completionSource.TrySetResult(result); }, null);
                    
                    //Wait for closing to complete
                    return await completionSource.Task;
                }
                catch (Exception x)
                {
                    //Abort the failed attempt
                    communicationObject.Abort();

                    if (IsTransientFailure(x))
                    {
                        //capture the failure
                        failures.Add(x);
                    }
                    else
                    {
                        Exception rethrow = CreateExceptionAfterNonTransientError(x);
                        if (rethrow == null || rethrow == x)
                            throw;
                        else
                            throw rethrow;
                    }
                }

            }
            
            throw CreateExceptionAfterFailedAttempts(failures);

        }

        /// <summary>
        /// Reliably invokes the specified asynchronous operation on the service proxy
        /// </summary>
        public async Task CallAsync(AsyncServiceDelegate<TProxy> operation)
        {
            await CallAsync(operation, CancellationToken.None);
        }

        /// <summary>
        /// Reliably invokes the specified asynchronous operation on the service proxy
        /// </summary>
        public async Task CallAsync(AsyncServiceDelegate<TProxy> operation, CancellationToken cancellationToken)
        {
            Func<TProxy, Task<object>> func = async proxy =>
                {
                    await operation(proxy);
                    return null;
                };

            await InvokeCore(new InvocationOptions<TProxy> { AsynchronousInvocation = func, CancellationToken = cancellationToken });
        }

        /// <summary>
        /// Reliably invokes the specified operation on the service proxy. Use this if the service proxy does not support async client-side methods
        /// </summary>
        public async Task LegacyCallAsync(Action<TProxy> operation)
        {
            await LegacyCallAsync(operation, CancellationToken.None);
        }

        /// <summary>
        /// Reliably invokes the specified operation on the service proxy. Use this if the service proxy does not support async client-side methods
        /// </summary>
        public async Task LegacyCallAsync(Action<TProxy> operation, CancellationToken cancellationToken)
        {
            Func<TProxy, object> func = proxy =>
            {
                operation(proxy);
                return null;
            };

            await InvokeCore(new InvocationOptions<TProxy> { SynchronousInvocation = func , CancellationToken = cancellationToken });
        }

        /// <summary>
        /// Reliably invokes the specified asynchronous operation on the service proxy
        /// </summary>
        public async Task<T> GetAsync<T>(AsyncServiceDelegate<TProxy, T> operation)
        {
            return await GetAsync(operation, CancellationToken.None);
        }

        /// <summary>
        /// Reliably invokes the specified asynchronous operation on the service proxy
        /// </summary>
        public async Task<T> GetAsync<T>(AsyncServiceDelegate<TProxy, T> operation, CancellationToken cancellationToken)
        {
            Func<TProxy, Task<object>> func = async proxy => await operation(proxy);
            object result = await InvokeCore(new InvocationOptions<TProxy> { AsynchronousInvocation = func, CancellationToken = cancellationToken });
            return (T)result;
        }

        /// <summary>
        /// Reliably invokes the specified operation on the service proxy. Use this if the service proxy does not support async client-side methods
        /// </summary>
        public async Task<T> LegacyGetAsync<T>(Func<TProxy, T> operation)
        {
            return await LegacyGetAsync(operation, CancellationToken.None);
        }

        /// <summary>
        /// Reliably invokes the specified operation on the service proxy. Use this if the service proxy does not support async client-side methods
        /// </summary>
        public async Task<T> LegacyGetAsync<T>(Func<TProxy, T> operation, CancellationToken cancellationToken)
        {
            Func<TProxy, object> func = proxy => operation(proxy);
            object result = await InvokeCore(new InvocationOptions<TProxy> { SynchronousInvocation = func });
            return (T)result;
        }

    }
}
