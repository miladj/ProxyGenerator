using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProxyGenerator.Core.Asyncs
{
    public interface IInterceptorAsync
    {
        object InterceptSync(IInvocation invocation, Func<object> next);
        Task InterceptAsync(IInvocation invocation, Task invocationReturnValue);
        Task<T> InterceptAsync<T>(IInvocation invocation, Task<T> invocationReturnValue);
        ValueTask InterceptAsync(IInvocation invocation, ValueTask invocationReturnValue);
        ValueTask<T> InterceptAsync<T>(IInvocation invocation, ValueTask<T> invocationReturnValue);
    }
    
}
