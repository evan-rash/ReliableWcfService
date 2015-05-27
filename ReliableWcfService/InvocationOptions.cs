using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.ServiceModel
{
    internal class InvocationOptions<TProxy> where TProxy : class
    {
        public Func<TProxy, Task<object>> AsynchronousInvocation { get; set; }
        public Func<TProxy, object> SynchronousInvocation { get; set; }
        public System.Threading.CancellationToken CancellationToken { get; set; }
    }
}
