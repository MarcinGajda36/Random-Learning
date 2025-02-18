namespace LeetCode;
using System;
using System.Buffers;
using System.Text;

public class Kata1
{
    private static readonly SearchValues<char> wordDelimiters = SearchValues.Create(['-', '_']);
    public static string ToCamelCase(string str)
    {
        var soFar = new StringBuilder();
        var textLeft = str.AsSpan();
        while (textLeft.Length > 0)
        {
            var delimiterIndex = textLeft.IndexOfAny(wordDelimiters);
            switch (delimiterIndex, soFar.Length)
            {
                case (-1, 0):
                    _ = soFar.Append(textLeft);
                    textLeft = [];
                    break;
                case (-1, _):
                    _ = soFar.Append(char.ToUpperInvariant(textLeft[0])).Append(textLeft[1..]);
                    textLeft = [];
                    break;
                case ( > 0, 0):
                    _ = soFar.Append(textLeft[..delimiterIndex]);
                    textLeft = textLeft[SkipDelimiter(delimiterIndex)..];
                    break;
                case ( > 0, _):
                    _ = soFar.Append(char.ToUpperInvariant(textLeft[0])).Append(textLeft[1..delimiterIndex]);
                    textLeft = textLeft[SkipDelimiter(delimiterIndex)..];
                    break;
                case (0, _):
                    // starts with delimiters or multiple delimiters in a row
                    textLeft = textLeft.Length > 1
                        ? textLeft[1..]
                        : [];
                    break;
            }
        }
        return soFar.ToString();

        static int SkipDelimiter(int index)
            => index + 1;
    }
}
