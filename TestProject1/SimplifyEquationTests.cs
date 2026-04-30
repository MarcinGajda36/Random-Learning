namespace TestProject1;

using LeetCode;

internal class SimplifyEquationTests
{
    [Test]
    public void SimplifyEquationTests01()
    {
        // Arrange
        var input = "(a+b)-c-(a+b)";

        // Act
        var result = SimplifyEquation.Simplify(input);

        // Assert
        Assert.That(result, Is.EqualTo("-c"));
    }

    [Test]
    public void SimplifyEquationTests02()
    {
        // Arrange
        var input = "a+b";

        // Act
        var result = SimplifyEquation.Simplify(input);

        // Assert
        Assert.That(result, Is.EqualTo("a+b"));
    }

    [Test]
    public void SimplifyEquationTests03()
    {
        // Arrange
        var input = "a-c";

        // Act
        var result = SimplifyEquation.Simplify(input);

        // Assert
        Assert.That(result, Is.EqualTo("a-c"));
    }

    [Test]
    public void SimplifyEquationTests04()
    {
        // Arrange
        var input = "a+b-b-a+c";

        // Act
        var result = SimplifyEquation.Simplify(input);

        // Assert
        Assert.That(result, Is.EqualTo("c"));
    }
}
