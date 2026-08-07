namespace Core.Storage;

public interface IDegreeOfParallelismProvider
{
    public int Value { get; }
}

// Type parameter is here just so you can register different providers with the DI framework
public interface IDegreeOfParallelismProvider<T> : IDegreeOfParallelismProvider
{
    
}
