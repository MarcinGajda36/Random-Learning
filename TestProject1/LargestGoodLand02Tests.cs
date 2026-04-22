namespace TestProject1;

using LeetCode;

public class LargestGoodLand02Tests
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
        _ = await Verify(LargestGoodLand02.LargestGoodSquare(arr));
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
        _ = await Verify(LargestGoodLand02.LargestGoodSquare(arr));
    }
}
