using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.ServiceModel
{
    /// <summary>
    /// An asynchronous operation on the service proxy
    /// </summary>
    public delegate Task AsyncServiceDelegate<T>(T proxy);

    /// <summary>
    /// An asynchronous operation on the service proxy
    /// </summary>
    public delegate Task<T> AsyncServiceDelegate<TProxy, T>(TProxy proxy);

}
