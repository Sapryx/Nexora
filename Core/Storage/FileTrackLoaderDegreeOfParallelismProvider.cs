namespace Core.Storage;

public class FileTrackLoaderDegreeOfParallelismProvider : IDegreeOfParallelismProvider<FileTrackLoader>
{
    // Degree of parallelism here is a result of benchmarking.
    public int Value { get; } = Environment.ProcessorCount * 2;
}
