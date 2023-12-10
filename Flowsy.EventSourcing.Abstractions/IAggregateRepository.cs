namespace Flowsy.EventSourcing.Abstractions;

public interface IAggregateRepository : IDisposable, IAsyncDisposable
{
    Task StoreAsync<TAggregate>(
        TAggregate aggregate,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
    
    Task StoreAsync<TAggregate>(
        IEnumerable<TAggregate> aggregates,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
    
    Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
    
    Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        long? version,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
}