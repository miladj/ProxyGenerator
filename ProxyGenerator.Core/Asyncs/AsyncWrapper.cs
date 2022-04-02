using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyGenerator.Core.Asyncs
{
    internal class AsyncWrapper:IInterceptor
    {
        private readonly IInterceptorAsync _interceptorAsync;

        public AsyncWrapper(IInterceptorAsync interceptorAsync)
        {
            _interceptorAsync = interceptorAsync;
        }
        public object Intercept(IInvocation invocation, Func<object> next)
        {
            return this.AsyncHelper(invocation, next, _interceptorAsync);
        }
    }
}
