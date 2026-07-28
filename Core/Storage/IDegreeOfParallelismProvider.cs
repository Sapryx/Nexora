namespace Core.Storage;

public interface IDegreeOfParallelismProvider
{
    public int Value { get; }
}

public interface IDegreeOfParallelismProvider<T> : IDegreeOfParallelismProvider
{
    public int Value { get; }
}
