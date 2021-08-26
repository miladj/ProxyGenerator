using System.Reflection;
using BenchmarkDotNet.Running;

namespace ProxyGenerator.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
        }
    }
}
