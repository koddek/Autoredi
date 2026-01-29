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
        Console.WriteLine("  1. ContainerBuildBenchmarks - Basic build performance");
        Console.WriteLine("  2. ServiceResolutionBenchmarks - Service resolution performance");
        Console.WriteLine("  3. ScopedResolutionBenchmarks - Scoped service resolution");
        Console.WriteLine("  4. GroupingBenchmarks - Basic grouping performance");
        Console.WriteLine("  5. ComprehensiveRegistrationBenchmarks - Detailed Manual vs Autoredi (Groups, Priorities, Lifetimes)");
        Console.WriteLine("  6. All - Run all benchmarks\n");

        if (args.Length == 0)
        {
            Console.WriteLine("Running all benchmarks...\n");
            BenchmarkRunner.Run<ContainerBuildBenchmarks>();
            BenchmarkRunner.Run<ServiceResolutionBenchmarks>();
            BenchmarkRunner.Run<ScopedResolutionBenchmarks>();
            BenchmarkRunner.Run<GroupingBenchmarks>();
            BenchmarkRunner.Run<ComprehensiveRegistrationBenchmarks>();
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
                case "grouping":
                    BenchmarkRunner.Run<GroupingBenchmarks>();
                    break;
                case "5":
                case "comprehensive":
                    BenchmarkRunner.Run<ComprehensiveRegistrationBenchmarks>();
                    break;
                case "6":
                case "all":
                default:
                    BenchmarkRunner.Run<ContainerBuildBenchmarks>();
                    BenchmarkRunner.Run<ServiceResolutionBenchmarks>();
                    BenchmarkRunner.Run<ScopedResolutionBenchmarks>();
                    BenchmarkRunner.Run<GroupingBenchmarks>();
                    BenchmarkRunner.Run<ComprehensiveRegistrationBenchmarks>();
                    break;
            }
        }

        Console.WriteLine("\n=== Benchmarks Complete ===");
    }
}
