using System;

namespace ProxyGenerator.Core
{
    public interface IInterceptor
    {
        object Intercept(IInvocation invocation,Func<object> next);
    }
}