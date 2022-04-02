using System;
using System.Threading.Tasks;

namespace ProxyGenerator.Core.Asyncs
{
    public interface IInterceptorAsync
    {
        object InterceptSync(IInvocation invocation, Func<object> next);
        Task InterceptAsync(IInvocation invocation, Func<Task> invocationReturnValue);
        Task<T> InterceptAsync<T>(IInvocation invocation, Func<Task<T>> invocationReturnValue);
        ValueTask InterceptAsync(IInvocation invocation, Func<ValueTask> invocationReturnValue);
        ValueTask<T> InterceptAsync<T>(IInvocation invocation, Func<ValueTask<T>> invocationReturnValue);
    }
    
}
