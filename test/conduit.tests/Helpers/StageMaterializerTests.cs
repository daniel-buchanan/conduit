using conduit.common;
using conduit.Helpers;
using conduit.Pipes;
using conduit.tests.Handlers;
using Xunit;

namespace conduit.tests.Helpers;

public class StageMaterializerTests
{
    public interface IMarkerValidationStage : IValidationPipeStage;

    [Fact]
    public void IsIncluded_Should_Exclude_A_Validation_Stage_When_Validation_Is_Excluded()
    {
        Assert.False(StageMaterializer.IsIncluded(typeof(IMarkerValidationStage), excludeValidation: true));
    }

    [Fact]
    public void IsIncluded_Should_Include_A_Validation_Stage_When_Validation_Is_Not_Excluded()
    {
        Assert.True(StageMaterializer.IsIncluded(typeof(IMarkerValidationStage), excludeValidation: false));
    }

    [Fact]
    public void IsIncluded_Should_Always_Include_A_Non_Validation_Stage()
    {
        Assert.True(StageMaterializer.IsIncluded(typeof(TestRequestHandler), excludeValidation: true));
    }

    public class TwoParamStage<TRequest, TResponse>;

    public class ThreeParamStage<TRequest, TResponse, THandler>;

    [Fact]
    public void Materialize_Should_Close_A_Two_Parameter_Open_Generic_Over_Request_And_Response()
    {
        var closed = StageMaterializer.Materialize(typeof(TwoParamStage<,>), typeof(TestRequest), typeof(TestResponse));

        Assert.Equal(typeof(TwoParamStage<TestRequest, TestResponse>), closed);
    }

    [Fact]
    public void Materialize_Should_Close_A_Three_Parameter_Open_Generic_Over_Request_Response_And_Handler()
    {
        var closed = StageMaterializer.Materialize(
            typeof(ThreeParamStage<,,>), typeof(TestRequest), typeof(TestResponse), typeof(TestRequestHandler));

        Assert.Equal(typeof(ThreeParamStage<TestRequest, TestResponse, TestRequestHandler>), closed);
    }

    [Fact]
    public void Materialize_Should_Return_A_Closed_Type_Unchanged()
    {
        var alreadyClosed = typeof(TwoParamStage<TestRequest, TestResponse>);

        var result = StageMaterializer.Materialize(alreadyClosed, typeof(TestRequest), typeof(TestResponse));

        Assert.Same(alreadyClosed, result);
    }
}
