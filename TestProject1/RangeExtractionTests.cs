namespace LeetCode.Tests;

public class RangeExtractionTests
{
    [Test]
    public void Extract_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        int[] input = Array.Empty<int>();

        // Act
        var result = RangeExtraction.Extract(input);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void Extract_SingleElementArray_ReturnsElementAsString()
    {
        // Arrange
        int[] input = { 5 };

        // Act
        var result = RangeExtraction.Extract(input);

        // Assert
        Assert.AreEqual("5", result);
    }

    [Test]
    public void Extract_ConsecutiveElements_ReturnsRange()
    {
        // Arrange
        int[] input = { 1, 2, 3, 4, 5 };

        // Act
        var result = RangeExtraction.Extract(input);

        // Assert
        Assert.AreEqual("1-5", result);
    }

    [Test]
    public void Extract_NonConsecutiveElements_ReturnsCommaSeparated()
    {
        // Arrange
        int[] input = { 1, 3, 5, 7 };

        // Act
        var result = RangeExtraction.Extract(input);

        // Assert
        Assert.AreEqual("1,3,5,7", result);
    }

    [Test]
    public void Extract_MixedElements_ReturnsCorrectRangesAndElements()
    {
        // Arrange
        int[] input = { 1, 2, 3, 5, 7, 8, 9, 11 };

        // Act
        var result = RangeExtraction.Extract(input);

        // Assert
        Assert.AreEqual("1-3,5,7-9,11", result);
    }
}
