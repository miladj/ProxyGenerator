using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace ProxyGenerator.Core.Asyncs
{
    public static class AsyncHelperExtensions
    {
        // private static readonly MethodInfo CreateTaskFuncMethodInfo= typeof(AsyncHelperExtensions).GetMethod(nameof(CreateTaskFunc));
        private static readonly MethodInfo CreateTaskFuncObjectMethodInfo = typeof(AsyncHelperExtensions).GetMethod(nameof(CreateTaskFuncObject));
        // private static readonly MethodInfo CreateValueTaskFuncMethodInfo = typeof(AsyncHelperExtensions).GetMethod(nameof(CreateValueTaskFunc));
        private static readonly MethodInfo CreateValueTaskFuncObjMethodInfo = typeof(AsyncHelperExtensions).GetMethod(nameof(CreateTaskFuncValueObject));

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
                        return CreateFuncAndInvoke(CreateTaskFuncObjectMethodInfo, returnType, interceptorAsync, invocation, func);
                    }
                    else 
                    {
                        return CreateFuncAndInvoke(CreateValueTaskFuncObjMethodInfo, returnType, interceptorAsync, invocation, func);
                    }

                }
                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    return (Task)interceptorAsync.InterceptAsync(invocation, async () =>
                    {
                        await (func() as Task);
                        return (object)null;
                    });
                }
                else if (typeof(ValueTask).IsAssignableFrom(returnType))
                {
                    Task<object> interceptAsync = interceptorAsync.InterceptAsync(invocation, async () =>
                    {
                        await (ValueTask)func();
                        return (object)null;
                    });
                    return new ValueTask(interceptAsync);
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
        //public static object CreateTaskFunc<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        //{
        //    Task<TResult> WrapperFunc() => func() as Task<TResult>;
        //    return interceptorAsync.InterceptAsync(invocation, WrapperFunc);
        //}
        //public static object CreateValueTaskFunc<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        //{
        //    async Task<TResult> WrapperFunc() => await (ValueTask<TResult>)func();

        //    return interceptorAsync.InterceptAsync(invocation,WrapperFunc);
        //}
        public static async Task<TResult> CreateTaskFuncObject<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        {
            async Task<object> WrapperFunc() => await (func() as Task<TResult>);
            return (TResult)await interceptorAsync.InterceptAsync(invocation, WrapperFunc);
        }
        public static async ValueTask<TResult> CreateTaskFuncValueObject<TResult>(IInterceptorAsync interceptorAsync, IInvocation invocation, Func<object> func)
        {
            async Task<object> WrapperFunc() => await (ValueTask<TResult>)func();
            return (TResult) await interceptorAsync.InterceptAsync(invocation, WrapperFunc);
        }

        public static IInterceptor CreateAsyncInterceptor(this IInterceptorAsync interceptorAsync)
        {
            return new AsyncWrapper(interceptorAsync);
        }
    }
    
}
