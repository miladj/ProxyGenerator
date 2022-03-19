using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public abstract class Invocation:IInvocation,IDefaultInvocation
    {
        public Invocation()
        {

        }

        public object[] Arguments
        {
            get => _arguments;
            set => _arguments = value;
        }

        public abstract MethodInfo Method { get; }

        public MethodInfo MethodInvocationTarget =>
            ProxyHelperMethods.GetImplMethodInfo(TargetType, Method);

        public object Target
        {
            get => _target;
            set => _target = value;
        }

        protected object _target;
        private object[] _arguments;

        public Type TargetType => Target?.GetType();

        public void SetArgument(uint index, object value)
        {
            if (_arguments!=null)
            {
                _arguments[index] = value;
                InternalSetArgument(index, value);
            }
        }

        protected internal abstract void InternalSetArgument(uint index, object value);
        public abstract object Invoke();
    }
}