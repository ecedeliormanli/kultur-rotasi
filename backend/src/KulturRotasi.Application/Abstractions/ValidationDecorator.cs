using FluentValidation;

namespace KulturRotasi.Application.Abstractions;

/// <summary>
/// Wraps every query handler and runs the query's validators first (decorator pattern).
/// Invalid input never reaches the handler; the API turns the exception into a 400 response.
/// </summary>
public sealed class ValidationDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(query, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await inner.HandleAsync(query, cancellationToken);
    }
}
