using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace ProxyGenerator.Core.Asyncs
{
    public static class AsyncHelperExtensions
    {
        public static bool IsAsync(this MethodInfo methodInfo) =>
        methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null && (typeof(Task).IsAssignableFrom(methodInfo.ReturnType) || typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType));
        public static object AsyncHelper(this IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        {
            return AsyncHelper(invocation, func, interceptorAsync);
        }
        public static object AsyncHelper(this IInterceptor _, IInvocation invocation, Func<object> func, IInterceptorAsync interceptorAsync)
        {
            return AsyncHelper(invocation, func, interceptorAsync);
        }
        public static object AsyncHelper(IInvocation invocation, Func<object> func, IInterceptorAsync interceptorAsync)
        {

            if (IsAsync(invocation.MethodInvocationTarget))
            {
                var rv = func();
                return interceptorAsync.InterceptAsync(invocation, (dynamic)rv);
            }
            else
            {
                return interceptorAsync.InterceptSync(invocation, func);
            }
        }
    }
    
}
