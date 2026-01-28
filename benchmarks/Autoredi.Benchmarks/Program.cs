using BenchmarkDotNet.Running;
using Autoredi.Benchmarks.Benchmarks;

namespace Autoredi.Benchmarks;

/// <summary>
/// Entry point for running Autoredi benchmarks.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Autoredi Benchmarks ===");
        Console.WriteLine("Comparing Autoredi vs Manual DI Registration Performance\n");

        Console.WriteLine("Available Benchmarks:");
        Console.WriteLine("  1. ContainerBuildBenchmarks - Container build performance");
        Console.WriteLine("  2. ServiceResolutionBenchmarks - Service resolution performance");
        Console.WriteLine("  3. ScopedResolutionBenchmarks - Scoped service resolution");
        Console.WriteLine("  4. All - Run all benchmarks\n");

        if (args.Length == 0)
        {
            Console.WriteLine("Running all benchmarks...\n");
            BenchmarkRunner.Run<ContainerBuildBenchmarks>();
            BenchmarkRunner.Run<ServiceResolutionBenchmarks>();
            BenchmarkRunner.Run<ScopedResolutionBenchmarks>();
        }
        else
        {
            switch (args[0].ToLower())
            {
                case "1":
                case "build":
                    BenchmarkRunner.Run<ContainerBuildBenchmarks>();
                    break;
                case "2":
                case "resolution":
                    BenchmarkRunner.Run<ServiceResolutionBenchmarks>();
                    break;
                case "3":
                case "scoped":
                    BenchmarkRunner.Run<ScopedResolutionBenchmarks>();
                    break;
                case "4":
                case "all":
                default:
                    BenchmarkRunner.Run<ContainerBuildBenchmarks>();
                    BenchmarkRunner.Run<ServiceResolutionBenchmarks>();
                    BenchmarkRunner.Run<ScopedResolutionBenchmarks>();
                    break;
            }
        }

        Console.WriteLine("\n=== Benchmarks Complete ===");
    }
}
