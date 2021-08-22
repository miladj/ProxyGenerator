using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public class Invocation:IInvocation
    {
        public object[] Arguments { get; set; }
    }
}