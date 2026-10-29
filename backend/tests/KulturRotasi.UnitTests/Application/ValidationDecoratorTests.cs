using FluentValidation;
using KulturRotasi.Application.Abstractions;

namespace KulturRotasi.UnitTests.Application;

public class ValidationDecoratorTests
{
    [Fact]
    public async Task Invalid_query_never_reaches_the_handler()
    {
        var inner = new RecordingHandler();
        var decorator = new ValidationDecorator<EchoQuery, string>(inner, [new EchoQueryValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() => decorator.HandleAsync(new EchoQuery(""), CancellationToken.None));
        Assert.False(inner.WasCalled);
    }

    [Fact]
    public async Task Valid_query_is_passed_to_the_handler()
    {
        var inner = new RecordingHandler();
        var decorator = new ValidationDecorator<EchoQuery, string>(inner, [new EchoQueryValidator()]);

        var result = await decorator.HandleAsync(new EchoQuery("merhaba"), CancellationToken.None);

        Assert.Equal("merhaba", result);
        Assert.True(inner.WasCalled);
    }

    public sealed record EchoQuery(string Text);

    private sealed class EchoQueryValidator : AbstractValidator<EchoQuery>
    {
        public EchoQueryValidator() => RuleFor(q => q.Text).NotEmpty();
    }

    private sealed class RecordingHandler : IQueryHandler<EchoQuery, string>
    {
        public bool WasCalled { get; private set; }

        public Task<string> HandleAsync(EchoQuery query, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(query.Text);
        }
    }
}
