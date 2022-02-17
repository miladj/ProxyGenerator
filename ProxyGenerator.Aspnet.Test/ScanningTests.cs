using System.Runtime.CompilerServices;
using ProxyGenerator.Aspnet.Test;


namespace ProxyGenerator.Aspnet.Test
{


    // ReSharper disable UnusedTypeParameter

    public interface ITransientService { }

    
    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService { }

    public class TransientService : ITransientService { }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }

    public interface IOpenGeneric<T> : IOtherInheritance { }

    public class OpenGeneric<T> : IOpenGeneric<T> { }

    public interface IPartiallyClosedGeneric<T1, T2> { }

    public class PartiallyClosedGeneric<T> : IPartiallyClosedGeneric<T, int> { }

    public interface ITransientServiceToCombine { }

    public interface IScopedServiceToCombine { }

    public interface ISingletonServiceToCombine { }
    
    public class CombinedService : ITransientServiceToCombine, IScopedServiceToCombine, ISingletonServiceToCombine { }

    public interface IWrongInheritanceA { }

    public interface IWrongInheritanceB { }

    
    public class WrongInheritance : IWrongInheritanceB { }

    public interface IDuplicateInheritance { }

    public interface IOtherInheritance { }

   
    public class DuplicateInheritance : IDuplicateInheritance, IOtherInheritance { }
    
    public interface IDefault1 { }

    public interface IDefault2 { }

    public interface IDefault3Level1 { }

    public interface IDefault3Level2 : IDefault3Level1 { }

    public class DefaultAttributes : IDefault3Level2, IDefault1, IDefault2 { }

    [CompilerGenerated]
    public class CompilerGenerated { }

    public class CombinedService2: IDefault1, IDefault2, IDefault3Level2 { }
}

namespace ProxyGenerator.Aspnet.Test.ChildNamespace
{
    public class ClassInChildNamespace { }
}

namespace UnwantedNamespace
{
    public class TransientService : ITransientService
    {
    }
}
