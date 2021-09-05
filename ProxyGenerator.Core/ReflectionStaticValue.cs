using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public static partial class ReflectionStaticValue
    {
        #region Types

        public static readonly Type MethodInfoType = typeof(MethodInfo);
        public static readonly Type TypeInvocation = typeof(Invocation);
        public static readonly Type TypeInterceptorHelper = typeof(InterceptorHelper);
        public static readonly Type TypeIInterceptor = typeof(IInterceptor);
        public static readonly Type TypeObject = typeof(object);
        public static readonly Type TypeIServiceProvider = typeof(IServiceProvider);
        public static readonly Type TypeIInvocation = typeof(IInvocation);
        public static readonly Type TypeArrayOfIInterceptor = typeof(IInterceptor[]);
        public static readonly Type TypeVoid = typeof(void);

        #endregion

        #region Constructors

        public static readonly ConstructorInfo Object_Constructor = TypeObject.GetConstructor(Type.EmptyTypes)!;

        public static readonly ConstructorInfo InterceptorHelper_Constructor =
            TypeInterceptorHelper.GetConstructors()[0];

        #endregion

        #region MethodInfo

        public static readonly MethodInfo MethodBase_GetMethodFromHandle =
            typeof(MethodBase).GetMethod(nameof(MethodInfo.GetMethodFromHandle),
                new[] {typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle)});

        public static readonly MethodInfo InterceptorHelper_Intercept =
            TypeInterceptorHelper.GetMethod(nameof(InterceptorHelper.Intercept));

        
        public static readonly MethodInfo IServiceProvider_GetService =
            TypeIServiceProvider.GetMethod(nameof(IServiceProvider.GetService));

        public static readonly MethodInfo Type_GetTypeFromHandle =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new Type[] {typeof(RuntimeTypeHandle)});

        public static readonly MethodInfo Invocation_Method_Get =
            TypeInvocation.GetProperty(nameof(Invocation.Method))!.GetMethod!;
        public static readonly MethodInfo Invocation_Method_Set =
            TypeInvocation.GetProperty(nameof(Invocation.Method))!.SetMethod!;

        public static readonly MethodInfo Invocation_Original_Set =
            TypeInvocation.GetProperty(nameof(Invocation.Original))!.SetMethod!;

        public static readonly MethodInfo Invocation_Arguments_Set =
            TypeInvocation.GetProperty(nameof(Invocation.Arguments))!.SetMethod!;

        #endregion

    }
}
