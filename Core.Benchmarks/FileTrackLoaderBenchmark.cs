using BenchmarkDotNet.Attributes;
using Core.Storage;
using Infrastructure.Storage;

namespace Core.Benchmarks;

[MemoryDiagnoser]
public class FileTrackLoaderBenchmark
{
    [Params(16, 32, 64)]
    public int DegreeOfParallelism { get; set; }

    private FileTrackLoader loader = null!;

    [GlobalSetup]
    public void Setup()
    {
        loader = new FileTrackLoader(new TagLibMetadataLoader(), DegreeOfParallelism);
    }

    [Benchmark]
    public void LoadTracks()
    {
        loader.Load();
    }
}
