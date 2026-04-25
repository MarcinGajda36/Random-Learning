namespace TestProject1;

using LeetCode;

internal class BinarySearchTests
{
    [Test]
    public void BinarySearchTests01()
    {
        Assert.That(BinarySearch.FindIndexOf01([1, 2, 3, 4, 5], 6), Is.EqualTo(-1));
    }

    [Test]
    public void BinarySearchTests02()
    {
        Assert.That(BinarySearch.FindIndexOf01([1, 2, 3, 4, 5, 6], 7), Is.EqualTo(-1));
    }

    [Test]
    public void BinarySearchTests03()
    {
        Assert.That(BinarySearch.FindIndexOf01([1, 2, 3, 4, 5, 6], 1), Is.EqualTo(0));
    }

    [Test]
    public void BinarySearchTests04()
    {
        Assert.That(BinarySearch.FindIndexOf01([1, 2, 3, 4, 5, 6], 6), Is.EqualTo(5));
    }

    [Test]
    public void BinarySearchTests05()
    {
        Assert.That(BinarySearch.FindIndexOf01([], 1), Is.EqualTo(-1));
    }

    [Test]
    public void BinarySearchTests06()
    {
        Assert.That(BinarySearch.FindIndexOf01([1], 1), Is.EqualTo(0));
    }

    [Test]
    public void BinarySearchTests07()
    {
        Assert.That(BinarySearch.FindIndexOf01([1, 2, 3, 4, 5, 6], 4), Is.EqualTo(3));
    }
}
