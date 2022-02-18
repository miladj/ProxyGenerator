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
        /// Create a type with ctor that accept an <paramref name="typeToProxy"/> Instance
        /// and an array of <paramref name="interceptorTypes"/>
        /// </summary>
        /// <param name="typeToProxy">Base type</param>
        /// <param name="interceptorTypes">Interceptor types</param>
        /// <returns>Proxy Type</returns>
        public static Type CreateProxyType(Type typeToProxy)
        {
            return new ProxyMaker(typeToProxy).CreateProxy();
        }
    }
}
