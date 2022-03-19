using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public static class ProxyHelperMethods
    {
        public static MethodInfo GetImplMethodInfo(Type targetType,MethodInfo interfaceMethodInfo)
        {
            //TODO:
            if (!interfaceMethodInfo.DeclaringType.IsInterface)
                return null;
            var map = targetType.GetInterfaceMap(interfaceMethodInfo.DeclaringType);
            var index = Array.IndexOf(map.InterfaceMethods, interfaceMethodInfo);
            if (index < 0)
                return null;
            return map.TargetMethods[index];
        }

        public static bool NeedUnboxing(this Type type) => type.IsValueType || type.IsGenericParameter;
        

    }
}
