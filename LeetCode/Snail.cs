namespace LeetCode;

using System.Collections.Generic;

// https://www.codewars.com/kata/521c2db8ddc89b9b7a0000c1/train/csharp
public class SnailSolution
{
    private enum Direction
    {
        Right,
        Down,
        Left,
        Up,
    }

    private record PathResult(int LastX, int LastY, Direction NextDirection, int LengthLeft);

    public static int[] Snail(int[][] array)
    {
        // I can probably avoid visited by making GoX methods assume if startY = 2 then last Y is one before end etc.
        var (visited, lengthLeft) = InitializeSameSizes<bool>(array);
        var snail = new List<int>(lengthLeft);
        var lastResult = new PathResult(0, 0, Direction.Right, lengthLeft);
        while (lastResult.LengthLeft > 0)
        {
            lastResult = lastResult.NextDirection switch
            {
                Direction.Right => GoRight(snail, visited, array, lastResult),
                Direction.Down => GoDown(snail, visited, array, lastResult),
                Direction.Left => GoLeft(snail, visited, array, lastResult),
                Direction.Up => GoUp(snail, visited, array, lastResult),
                var unknown => throw new System.NotSupportedException($"Unknown direction: {unknown}")
            };
        }

        return [.. snail];
    }

    private static PathResult GoRight(List<int> destination, bool[][] visited, int[][] source, PathResult lastResult)
    {
        const Direction NextDirection = Direction.Down;
        var (lastX, lastY, _, lengthLeft) = lastResult;
        var visitedSegment = visited[lastY];
        var sourceSegment = source[lastY];
        var startingX = visitedSegment[lastX]
            ? lastX + 1
            : lastX;
        for (var index = startingX; index < sourceSegment.Length; ++index)
        {
            var sourceElement = sourceSegment[index];
            ref var visited_ = ref visitedSegment[index];
            if (visited_)
            {
                return new PathResult(index - 1, lastY, NextDirection, lengthLeft);
            }
            visited_ = true;
            destination.Add(sourceElement);
            --lengthLeft;
        }

        return new PathResult(sourceSegment.Length - 1, lastY, NextDirection, lengthLeft);
    }

    private static PathResult GoDown(List<int> destination, bool[][] visited, int[][] source, PathResult lastResult)
    {
        const Direction NextDirection = Direction.Left;
        var (lastX, lastY, _, lengthLeft) = lastResult;
        var startingY = visited[lastY][lastX]
            ? lastY + 1
            : lastY;
        for (var index = startingY; index < source.Length; ++index)
        {
            var sourceElement = source[index][lastX];
            ref var visited_ = ref visited[index][lastX];
            if (visited_)
            {
                return new PathResult(lastX, index - 1, NextDirection, lengthLeft);
            }
            visited_ = true;
            destination.Add(sourceElement);
            --lengthLeft;
        }

        return new PathResult(lastX, source.Length - 1, NextDirection, lengthLeft);
    }

    private static PathResult GoLeft(List<int> destination, bool[][] visited, int[][] source, PathResult lastResult)
    {
        const Direction NextDirection = Direction.Up;
        var (lastX, lastY, _, lengthLeft) = lastResult;
        var visitedSegment = visited[lastY];
        var sourceSegment = source[lastY];
        var startingX = visitedSegment[lastX]
            ? lastX - 1
            : lastX;
        for (var index = startingX; index >= 0; --index)
        {
            var sourceElement = sourceSegment[index];
            ref var visited_ = ref visitedSegment[index];
            if (visited_)
            {
                return new PathResult(index + 1, lastY, NextDirection, lengthLeft);
            }
            visited_ = true;
            destination.Add(sourceElement);
            --lengthLeft;
        }

        return new PathResult(0, lastY, NextDirection, lengthLeft);
    }

    private static PathResult GoUp(List<int> destination, bool[][] visited, int[][] source, PathResult lastResult)
    {
        const Direction NextDirection = Direction.Right;
        var (lastX, lastY, _, lengthLeft) = lastResult;
        var startingY = visited[lastY][lastX]
            ? lastY - 1
            : lastY;
        for (var index = startingY; index >= 0; --index)
        {
            var sourceElement = source[index][lastX];
            ref var visited_ = ref visited[index][lastX];
            if (visited_)
            {
                return new PathResult(lastX, index + 1, NextDirection, lengthLeft);
            }
            visited_ = true;
            destination.Add(sourceElement);
            --lengthLeft;
        }

        return new PathResult(lastX, 0, NextDirection, lengthLeft);
    }

    private static (TType[][], int TotalLength) InitializeSameSizes<TType>(int[][] array)
    {
        var visited = new TType[array.Length][];
        var totalLength = 0;
        for (var i = 0; i < visited.Length; ++i)
        {
            var innerLength = array[i].Length;
            visited[i] = new TType[innerLength];
            totalLength += innerLength;
        }

        return (visited, totalLength);
    }
}