using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyGenerator.Core;

namespace ProxyGenerator.Aspnet
{
    public partial class ProxyMakerAspnet
    {
        /// <summary>
        /// Create A proxy resolve everything from IServiceProvider including <paramref name="implementType"/> and <paramref name="interceptorTypes"/>
        /// </summary>
        /// <param name="typeToProxy">Base type to create proxy</param>
        /// <param name="implementType">Implement type to resolve from IServiceProvider to act as InnerObject</param>
        /// <param name="interceptorTypes">Interceptor Types</param>
        /// <returns>Proxy Type</returns>
        public static Type CreateProxyTypeWithInterceptors(Type typeToProxy, Type implementType,params Type[] interceptorTypes)
        {
            return new ProxyMakerAspnet(typeToProxy,implementType,interceptorTypes).CreateProxy();
        }
        /// <summary>
        /// Create a proxy so open generic with a decorator
        /// this method is specifically useful when using open generic and ServiceCollection
        /// </summary>
        /// <param name="typeToProxy">Base type to create proxy</param>
        /// <param name="implementType">Implement type to resolve from IServiceProvider to act as InnerObject</param>
        /// <param name="decoratorType">Decorator Type</param>
        /// <returns>Proxy Type</returns>
        public static Type CreateProxyTypeWithDecorator(Type typeToProxy, Type implementType, Type decoratorType)
        {
            return new ProxyMakerAspnet(typeToProxy, implementType, decoratorType).CreateProxy();
        }

    }
}
