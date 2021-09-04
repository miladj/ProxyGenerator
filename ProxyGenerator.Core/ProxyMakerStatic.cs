using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyGenerator.Core
{
    public partial class ProxyMaker
    {
        /// <summary>
        /// Create a type with ctor that accept an IServiceProvider Instance
        /// so create instances of <paramref name="interceptorTypes"/> by helping IServiceProvider
        /// </summary>
        /// <param name="typeToProxy">Base type</param>
        /// <param name="interceptorTypes">Interceptor types</param>
        /// <returns>Proxy Type</returns>
        public static Type CreateProxyTypeUseIServiceProvider(Type typeToProxy,params Type[] interceptorTypes)
        {
            return new ProxyMaker(typeToProxy, interceptorTypes, true).CreateProxy();
        }
        /// <summary>
        /// Create a type with ctor that accept an <paramref name="typeToProxy"/> Instance
        /// and an array of <paramref name="interceptorTypes"/>
        /// </summary>
        /// <param name="typeToProxy">Base type</param>
        /// <param name="interceptorTypes">Interceptor types</param>
        /// <returns>Proxy Type</returns>
        public static Type CreateProxyType(Type typeToProxy,params Type[] interceptorTypes)
        {
            return new ProxyMaker(typeToProxy, interceptorTypes, false).CreateProxy();
        }
    }
}
