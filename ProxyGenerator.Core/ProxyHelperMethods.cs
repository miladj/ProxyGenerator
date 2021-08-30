using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProxyGenerator.Core
{
    public static class ProxyHelperMethods
    {
        public static void FillInvocationProperties(Invocation invocation)
        {
            //TODO:Bottleneck
            invocation.TargetType = invocation.Original.GetType();
            invocation.MethodInvocationTarget = GetImplMethodInfo(invocation.TargetType, invocation.Method);

        }
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

    }
}
