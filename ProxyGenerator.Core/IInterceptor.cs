using System;

namespace ProxyGenerator.Core
{
    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }
}