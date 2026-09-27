using BenchmarkDotNet.Attributes;
using conduit.Pipes.Stages;

namespace conduit.benchmarks;

/// <summary>
/// Cost of the validator's own rule check, with no DI, no pipe, no stage plumbing involved at
/// all — just <c>new ValidatedRequestValidator().ValidateAsync(request)</c>. Whatever's left of
/// DispatchBenchmarks' NoValidation-to-WithValidation delta after subtracting this and
/// StageOverheadBenchmarks' one-extra-stage cost is the validation *stage's* overhead specifically
/// (resolving IModelValidator&lt;,&gt; from DI, wrapping the result) rather than the rule itself.
/// </summary>
[MemoryDiagnoser]
public class ValidatorBenchmarks
{
    private readonly ValidatedRequestValidator _validator = new();
    private readonly ValidatedRequest _request = new() { Message = "hello" };

    [Benchmark]
    public Task<ValidationResult<ValidatedRequest>> ValidateAsync()
        => _validator.ValidateAsync(_request);

    /// <summary>
    /// The validator is DI-registered Transient, so this constructor — including its AddRules call —
    /// runs fresh on every dispatch. <c>Should(...)</c> takes an <c>Expression&lt;Func&lt;,&gt;&gt;</c> and
    /// calls <see cref="System.Linq.Expressions.LambdaExpression.Compile()"/> to get a delegate back out
    /// of it; that compile step is the suspected dominant cost, so this measures construction alone.
    /// </summary>
    [Benchmark]
    public ValidatedRequestValidator ConstructValidator()
        => new();
}
