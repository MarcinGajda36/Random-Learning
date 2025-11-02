using HighPerf;

namespace TestProject1;

public class VectoresTest
{
    [Test]
    public void SumVectorized_Int_ReturnsCorrectSum()
    {
        // Arrange
        int[] numbers = [1, 2, 3, 4, 5];

        // Act
        var result = Vectores.SumVectorized<int>(numbers.AsSpan());

        // Assert
        Assert.That(result, Is.EqualTo(numbers.Sum()));
    }

    [Test]
    public void SumVectorized_Int_To_Long_ReturnsCorrectLongSum()
    {
        // Arrange
        int[] numbers = [-100, -1, 0, 1, 50, 100];
        long expected = numbers.Select(n => (long)n).Sum();

        // Act
        var result = Vectores.SumVectorized<int, long>(numbers.AsSpan(), 0L);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void SumVectorized_Double_TResult_Double_ReturnsCorrectSum()
    {
        // Arrange
        double[] numbers = [1.5, 2.25, -0.75, 4.0];
        double expected = numbers.Sum();

        // Act
        var result = Vectores.SumVectorized<double, double>(numbers.AsSpan(), 0d);

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(1e-12));
    }

    [Test]
    public void AverageVectorized_Int_To_Double_ReturnsCorrectAverage()
    {
        // Arrange
        int[] numbers = [1, 2, 3, 4, 5, 6];
        double expected = numbers.Average();

        // Act
        var result = Vectores.AverageVectorized<int, double>(numbers.AsSpan(), 0d);

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(1e-12));
    }

    [Test]
    public void AverageVectorized_Double_TResult_Double_ReturnsCorrectAverage()
    {
        // Arrange
        double[] numbers = [-2.5, 0.0, 2.5, 5.0];
        double expected = numbers.Average();

        // Act
        var result = Vectores.AverageVectorized<double, double>(numbers, 0d);

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(1e-12));
    }
}