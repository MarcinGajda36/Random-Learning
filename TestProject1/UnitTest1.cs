namespace TestProject1;

using System.Threading.Tasks;
using LeetCode;

public class Tests
{
    public class SnailTest
    {
        [Test, Order(1)]
        public async Task SnailTest1()
        {
            int[][] array =
            {
                new []{1, 2, 3},
                new []{4, 5, 6},
                new []{7, 8, 9}
            };
            var r = new[] { 1, 2, 3, 6, 9, 8, 7, 4, 5 };
            var snail = SnailSolution.Snail(array);
            Test(snail, r);
            await Verify(snail);
        }

        public string Int2dToString(int[][] a)
        {
            return $"[{string.Join("\n", a.Select(row => $"[{string.Join(",", row)}]"))}]";
        }

        public void Test(int[] snail, int[] result)
        {
            Assert.That(snail, Is.EqualTo(result));
        }

        [Test]
        public async Task FirstSnapshotTest()
        {
            await Verify(new { x = 1, y = 5, z = "abc" });
        }

        [Test]
        public async Task FirstThrowingSnapshotTest()
        {
            await ThrowsTask(() => throw new Exception());
        }
    }
}