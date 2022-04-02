using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public static class ProxyHelperMethods
    {
        public static MethodInfo GetImplMethodInfo(Type targetType,MethodInfo interfaceMethodInfo)
        {
            //TODO:
            var methodInfo = interfaceMethodInfo;
            if (!methodInfo.DeclaringType.IsInterface)
                return null;
            var map = targetType.GetInterfaceMap(methodInfo.DeclaringType);
            if (interfaceMethodInfo.IsGenericMethod)
                methodInfo = interfaceMethodInfo.GetGenericMethodDefinition();
            var index = Array.IndexOf(map.InterfaceMethods, methodInfo);
            if (index < 0)
                return null;
            MethodInfo mapTargetMethod = map.TargetMethods[index];
            if (interfaceMethodInfo.IsGenericMethod)
            {
                Type[] genericArguments = interfaceMethodInfo.GetGenericArguments();
                return mapTargetMethod.MakeGenericMethod(genericArguments);
            }
            return mapTargetMethod;
        }

        public static bool NeedUnboxing(this Type type) => type.IsValueType || type.IsGenericParameter;
        

    }
}
