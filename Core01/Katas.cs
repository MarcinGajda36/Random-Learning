public class Kata
{
    public static int[] AddingShifted(int[][] arrayOfArrays, int shift)
    {
        var elementCount = arrayOfArrays[0].Length + (arrayOfArrays.Length - 1) * shift;
        int[] result = new int[elementCount];

        for (int i = 0; i < arrayOfArrays.Length; ++i)
        {
            var currentOffset = shift * i;
            var currentArr = arrayOfArrays[i];
            for (int j = 0; j < currentArr.Length; ++j)
            {
                var idxToModif = j + currentOffset;
                result[idxToModif] += currentArr[j];
            }
        }
        return result;
    }
}