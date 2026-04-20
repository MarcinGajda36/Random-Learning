namespace TestProject1;

using LeetCode;

public class LargestGoodSquareTests
{
    [Test]
    public async Task LargestGoodSquareTests01()
    {
        int[][] arr =
            [
            [0,1,1],
            [0,1,1],
            [0,0,0]
            ];
        _ = await Verify(LargestGoodLand.LargestGoodSquare(arr));
    }

    [Test]
    public async Task LargestGoodSquareTests02()
    {
        int[][] arr =
            [
            [0,1,1,0,0],
            [0,1,1,1,1],
            [0,0,1,1,1],
            [0,0,1,1,1],
            [0,0,0,0,0],
            ];
        _ = await Verify(LargestGoodLand.LargestGoodSquare(arr));
    }
}
