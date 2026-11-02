using System.Text.Json;
using FluentValidation;
using KulturRotasi.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KulturRotasi.Api.Infrastructure;

/// <summary>
/// Turns expected exceptions into RFC 9457 problem details responses (like a Spring <c>@ControllerAdvice</c>).
/// Anything else falls through to the default handler and becomes a 500 without internal details.
/// </summary>
public sealed class ProblemExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            ValidationException validation => new HttpValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
            },
            DomainException domain => new ProblemDetails
            {
                Title = "The request breaks a business rule.",
                Detail = domain.Message,
                Status = StatusCodes.Status400BadRequest,
            },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
