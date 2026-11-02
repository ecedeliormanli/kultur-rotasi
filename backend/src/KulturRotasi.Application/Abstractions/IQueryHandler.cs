namespace KulturRotasi.Application.Abstractions;

/// <summary>
/// Handles one read-only use case (the "Q" in CQRS). Each query has exactly one handler,
/// and controllers depend on this interface instead of on a database.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
