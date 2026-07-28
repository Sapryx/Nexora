namespace Core.Storage;

public interface IDegreeOfParallelismProvider
{
    public int Value { get; }
}

public interface IDegreeOfParallelismProvider<T> : IDegreeOfParallelismProvider
{
    public new int Value { get; }
}
