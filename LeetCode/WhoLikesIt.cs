namespace LeetCode;

using System;

public static class Kata2
{
    //https://www.codewars.com/kata/5266876b8f4bf2da9b000362
    public static string Likes(string[] name)
        => name.AsSpan() switch
        {
            [] => "no one likes this",
            [var one] => $"{one} likes this",
            [var first, var second] => $"{first} and {second} like this",
            [var first, var second, var third] => $"{first}, {second} and {third} like this",
            [var first, var second, .. var rest] => $"{first}, {second} and {rest.Length} others like this",
        };
}
