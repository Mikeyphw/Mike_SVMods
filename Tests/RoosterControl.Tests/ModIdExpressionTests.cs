using RoosterControl.Helpers;
using StardewModdingAPI;
using Xunit;
using NSubstitute;

namespace RoosterControl.Tests;

/// <summary>Tests for <see cref="ModIdExpression"/>.</summary>
public class ModIdExpressionTests
{
    private readonly IModRegistry _registry;

    public ModIdExpressionTests()
    {
        _registry = Substitute.For<IModRegistry>();
    }

    [Fact]
    public void Evaluate_DirectModId_WhenLoaded_ReturnsTrue()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate("ModA", _registry);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_DirectModId_WhenNotLoaded_ReturnsFalse()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(false);

        // Act
        var result = ModIdExpression.Evaluate("ModA", _registry);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_AndOperator_AllLoaded_ReturnsTrue()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(true);
        _registry.IsLoaded("ModB").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate("ModA&&ModB", _registry);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_AndOperator_OneNotLoaded_ReturnsFalse()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(true);
        _registry.IsLoaded("ModB").Returns(false);

        // Act
        var result = ModIdExpression.Evaluate("ModA&&ModB", _registry);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_OrOperator_OneLoaded_ReturnsTrue()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(false);
        _registry.IsLoaded("ModB").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate("ModA||ModB", _registry);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_OrOperator_NoneLoaded_ReturnsFalse()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(false);
        _registry.IsLoaded("ModB").Returns(false);

        // Act
        var result = ModIdExpression.Evaluate("ModA||ModB", _registry);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_WithSpaces_TrimsCorrectly()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(true);
        _registry.IsLoaded("ModB").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate(" ModA && ModB ", _registry);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_EmptyExpression_ReturnsFalse()
    {
        // Act
        var result = ModIdExpression.Evaluate("", _registry);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_WhitespaceOnly_ReturnsFalse()
    {
        // Act
        var result = ModIdExpression.Evaluate("   ", _registry);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_AndWithThreeMods_AllLoaded_ReturnsTrue()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(true);
        _registry.IsLoaded("ModB").Returns(true);
        _registry.IsLoaded("ModC").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate("ModA&&ModB&&ModC", _registry);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_OrWithThreeMods_OneLoaded_ReturnsTrue()
    {
        // Arrange
        _registry.IsLoaded("ModA").Returns(false);
        _registry.IsLoaded("ModB").Returns(false);
        _registry.IsLoaded("ModC").Returns(true);

        // Act
        var result = ModIdExpression.Evaluate("ModA||ModB||ModC", _registry);

        // Assert
        Assert.True(result);
    }
}

