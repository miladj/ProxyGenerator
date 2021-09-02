using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProxyGenerator.Core
{
    public static partial class ReflectionStaticValue
    {
        public static readonly MethodInfo Array_Empty_OfObjet =
            typeof(Array).GetMethod(nameof(Array.Empty))!.MakeGenericMethod(typeof(object));
    }
}
