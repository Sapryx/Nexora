using BenchmarkDotNet.Running;

namespace Core.Benchmarks;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<FileTrackLoaderBenchmark>();
    }
}
