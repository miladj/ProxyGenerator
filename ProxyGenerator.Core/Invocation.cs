using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public abstract class Invocation:IInvocation,IDefaultInvocation
    {
        public Invocation(int argumentCount)
        {
            ArgumentCount = argumentCount;
        }

        public long ArgumentCount { get; } 

        public abstract MethodInfo Method { get; }

        public MethodInfo MethodInvocationTarget =>
            ProxyHelperMethods.GetImplMethodInfo(TargetType, Method);

        public object Target
        {
            get => _target;
            set => _target = value;
        }

        protected object _target;

        public Type TargetType => Target?.GetType();

        public void SetArgument(uint index, object value)
        { 
            InternalSetArgument(index, value);
        }

        public object GetArgument(uint index)
        {
            return InternalGetArgument(index);
        }

        protected internal abstract void InternalSetArgument(uint index, object value);
        protected internal abstract object InternalGetArgument(uint index);
        public abstract object Invoke();
    }
}