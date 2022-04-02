using System;
using System.Threading.Tasks;

namespace ProxyGenerator.Core.Asyncs
{
    public interface IInterceptorAsync
    {
        object InterceptSync(IInvocation invocation, Func<object> next);
        Task<object> InterceptAsync(IInvocation invocation, Func<Task<object>> invocationReturnValue);
    }
    
}
