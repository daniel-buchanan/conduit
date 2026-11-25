using conduit.Exceptions;
using Xunit;

namespace conduit.tests.Exceptions;

public class StageNotFoundExceptionTests
{
    [Fact]
    public void DefaultMessage_Names_A_Stage_Not_A_Pipe()
        => Assert.Equal("Stage not found", new StageNotFoundException().Message);
}
