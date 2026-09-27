using conduit.logging;
using conduit.validation;

namespace conduit.benchmarks;

/// <summary>
/// Locator type for assembly scans (<c>RegisterHandlersAsImplementedFrom</c>, <c>WithValidatorsFromAssembly</c>).
/// </summary>
public sealed class BenchmarkAssemblyMarker;

public class NoOpRequest : IRequest<NoOpResponse>
{
    public string Message { get; set; } = string.Empty;
}

public class NoOpResponse
{
    public string Value { get; set; } = string.Empty;
}

public class NoOpRequestHandler(ILog logger) : RequestHandler<NoOpRequest, NoOpResponse>(logger)
{
    public override Task<NoOpResponse> HandleAsync(NoOpRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new NoOpResponse { Value = request.Message });
}

public class ValidatedRequest : IRequest<ValidatedResponse>
{
    public string Message { get; set; } = string.Empty;
}

public class ValidatedResponse
{
    public string Value { get; set; } = string.Empty;
}

public class ValidatedRequestValidator : ModelValidator<ValidatedRequest, ValidatedResponse>
{
    protected override void AddRules(IRuleBuilder<ValidatedRequest> ruleBuilder)
    {
        ruleBuilder.Should(m => m.Message).NotBe().Null();
    }
}

public class ValidatedRequestHandler(ILog logger) : RequestHandler<ValidatedRequest, ValidatedResponse>(logger)
{
    public override Task<ValidatedResponse> HandleAsync(ValidatedRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ValidatedResponse { Value = request.Message });
}
