using Core.Common.Results;

namespace Core.Common.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var result = Result.Failure("Something failed", "ERR_CODE");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something failed", result.Error);
        Assert.Equal("ERR_CODE", result.ErrorCode);
    }

    [Fact]
    public void Failure_WithoutErrorCode_ShouldHaveNullCode()
    {
        var result = Result.Failure("Something failed");

        Assert.True(result.IsFailure);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ShouldHaveDefaultValue()
    {
        var result = Result.Failure<int>("Failed", "ERR");

        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
        Assert.Equal("Failed", result.Error);
    }

    [Fact]
    public void GenericSuccess_WithStringValue_ShouldWork()
    {
        var result = Result<string>.Success("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void GenericFailure_WithStringType_ShouldHaveNullValue()
    {
        var result = Result<string>.Failure("Not found");

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertValueToResult()
    {
        Result<string> result = "implicit value";

        Assert.True(result.IsSuccess);
        Assert.Equal("implicit value", result.Value);
    }

    [Fact]
    public void ImplicitOperator_WithComplexType_ShouldWork()
    {
        var list = new List<int> { 1, 2, 3 };
        Result<List<int>> result = list;

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
    }

    [Fact]
    public void Success_WithNullableReferenceType_ShouldWork()
    {
        var result = Result.Success<string?>(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
