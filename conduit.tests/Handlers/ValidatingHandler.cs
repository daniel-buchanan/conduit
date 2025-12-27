using conduit.logging;
using conduit.validation;

namespace conduit.tests.Handlers;

public class ValidatingRequest : IRequest<ValidatingResponse>
{
    public string Message { get; set; } = string.Empty;
}

public class ValidatingResponse
{
    public string Value { get; set; } = string.Empty;
}

public class ValidatingRequestValidator : ModelValidator<ValidatingRequest>
{
    protected override Task AddRules(IRuleBuilder<ValidatingRequest> ruleBuilder)
    {
        ruleBuilder.Should(m => m.Message).NotBe().Null();
        return Task.CompletedTask;
    }
}

public class ValidatingRequestHandler(ILog logger) : RequestHandler<ValidatingRequest, ValidatingResponse>(logger)
{
    public override Task<ValidatingResponse> HandleAsync(ValidatingRequest testRequest, CancellationToken cancellationToken = default)
    {
        Logger.Info(testRequest.Message);
        return Task.FromResult(new ValidatingResponse() { Value = testRequest.Message });
    }
}