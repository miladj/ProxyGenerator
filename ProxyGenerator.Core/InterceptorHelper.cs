namespace ProxyGenerator.Core
{
    public class InterceptorHelper
    {
        private readonly IDefaultInvocation _defaultInvocation;
        private readonly IInterceptor[] _interceptors;
        private readonly IInvocation _invocation;
        private int _interceptorCounter = 0;

        public InterceptorHelper(IDefaultInvocation defaultInvocation,IInterceptor[] interceptors,IInvocation invocation)
        {
            _defaultInvocation = defaultInvocation;
            _interceptors = interceptors;
            _invocation = invocation;

        }

        public object Intercept()
        {
            if (_interceptorCounter == _interceptors.Length)
                return _defaultInvocation.Invoke();
            return _interceptors[_interceptorCounter++].Intercept(_invocation, Intercept);
        }
    }
}