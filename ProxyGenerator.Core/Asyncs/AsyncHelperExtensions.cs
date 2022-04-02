using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace ProxyGenerator.Core.Asyncs
{
    public static class AsyncHelperExtensions
    {
        private static readonly MethodInfo CreateTaskFuncMethodInfo= typeof(AsyncHelperExtensions).GetMethod(nameof(CreateTaskFunc));
        private static readonly MethodInfo CreateValueTaskFuncMethodInfo = typeof(AsyncHelperExtensions).GetMethod(nameof(CreateValueTaskFunc));

        public static bool IsAsync(this MethodInfo methodInfo) =>
            methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null
            && methodInfo.ReturnType.GetMethods().Any(x=>x.Name== "GetAwaiter");
        //&& (typeof(Task).IsAssignableFrom(methodInfo.ReturnType) || typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType));
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
            var invocationMethodInvocationTarget = invocation.MethodInvocationTarget;
            if (IsAsync(invocationMethodInvocationTarget))
            {
                Type returnType = invocationMethodInvocationTarget.ReturnType;
                if (returnType.GetTypeInfo().IsGenericType)
                {
                    if (typeof(Task).IsAssignableFrom(returnType))
                    {
                        return CreateFuncAndInvoke(CreateTaskFuncMethodInfo, returnType, interceptorAsync, invocation, func);
                    }
                    else if (returnType.GetGenericTypeDefinition()==typeof(ValueTask<>))
                    {
                        return CreateFuncAndInvoke(CreateValueTaskFuncMethodInfo, returnType, interceptorAsync, invocation, func);
                    }

                }
                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    return interceptorAsync.InterceptAsync(invocation, () => func() as Task);
                }
                else if (typeof(ValueTask).IsAssignableFrom(returnType))
                {
                    return interceptorAsync.InterceptAsync(invocation, () => (ValueTask)func());
                }

                throw new NotImplementedException($"Unhandled situation in {nameof(AsyncHelperExtensions)} it is definitely a bug");
            }
            else
            {
                return interceptorAsync.InterceptSync(invocation, func);
            }
        }

        private static object CreateFuncAndInvoke(MethodInfo methodInfo, Type returnType,
            IInterceptorAsync interceptorAsync,
            IInvocation invocation, Func<object> func)
        {
            return methodInfo.MakeGenericMethod(returnType.GetGenericArguments()[0]).Invoke(null, new object[] { interceptorAsync, invocation, func });
        }
        public static object CreateTaskFunc<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        {
            Task<TResult> WrapperFunc() => func() as Task<TResult>;
            return interceptorAsync.InterceptAsync(invocation, WrapperFunc);
        }
        public static object CreateValueTaskFunc<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        {
            ValueTask<TResult> WrapperFunc() => (ValueTask<TResult>)func();

            return interceptorAsync.InterceptAsync(invocation,WrapperFunc);
        }
    }
    
}
