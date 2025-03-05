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

/*
GPT refactor:
namespace LeetCode;

using System.Collections.Generic;

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
        var (visited, lengthLeft) = InitializeSameSizes<bool>(array);
        var snail = new List<int>(lengthLeft);
        var lastResult = new PathResult(0, 0, Direction.Right, lengthLeft);
        while (lastResult.LengthLeft > 0)
        {
            lastResult = Traverse(snail, visited, array, lastResult);
        }
        return [.. snail];
    }

    private static PathResult Traverse(List<int> destination, bool[][] visited, int[][] source, PathResult lastResult)
    {
        int dx = 0, dy = 0;
        Direction nextDirection;
        switch (lastResult.NextDirection)
        {
            case Direction.Right: dx = 1; nextDirection = Direction.Down; break;
            case Direction.Down: dy = 1; nextDirection = Direction.Left; break;
            case Direction.Left: dx = -1; nextDirection = Direction.Up; break;
            case Direction.Up: dy = -1; nextDirection = Direction.Right; break;
            default: throw new System.NotSupportedException($"Unknown direction: {lastResult.NextDirection}");
        }
        
        var (x, y, _, lengthLeft) = lastResult;
        int startX = x, startY = y;
        if (visited[y][x])
        {
            startX += dx;
            startY += dy;
        }
        
        while (startX >= 0 && startX < source[0].Length && startY >= 0 && startY < source.Length && !visited[startY][startX])
        {
            destination.Add(source[startY][startX]);
            visited[startY][startX] = true;
            lengthLeft--;
            x = startX;
            y = startY;
            startX += dx;
            startY += dy;
        }
        
        return new PathResult(x, y, nextDirection, lengthLeft);
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
*/
