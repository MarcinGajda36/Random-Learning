namespace LeetCode;

using System.Collections.Generic;
using System.Text;

// We get input like: (a+b)-c-(a+b) and need to return the simplified form of if so
// (a+b)-c-(a+b) -> -c
// a+b-b-a+c -> c
public class SimplifyEquation
{
    // Idea 1: brute-force
    // go through input, when i find any character then parse for just that 1 char and save that it was parse then look for other chars until done
    // Idea 2: one iter
    // as you go keep track of something like Dictionary<variable, final count? or List<(sign, count?)>>
    public static string Simplify(string toSimplify)
    {
        var countByVariable = new Dictionary<char, int>();
        var sign = 1;
        var parenthesisDepth = 0;
        for (var idx = 0; idx < toSimplify.Length; idx++)
        {
            var currentChar = toSimplify[idx];
            switch (currentChar)
            {
                case >= 'a' and <= 'z':
                    var newCount = countByVariable.GetValueOrDefault(currentChar) + sign;
                    countByVariable[currentChar] = newCount;
                    if (parenthesisDepth is 0 && sign is -1)
                    {
                        sign = 1;
                    }
                    break;
                case '-':
                    sign *= -1; // Too naive for nested parenthesis, i would need maybe dictionary<parenthesisDepth, sign>?
                    break;
                case ')':
                    parenthesisDepth--;
                    sign = 1;
                    break;
                case '(':
                    parenthesisDepth++;
                    break;
                default:
                    break;
            }
        }

        var response = new StringBuilder();
        foreach (var (variable, count) in countByVariable)
        {
            switch (count, response.Length)
            {
                case (0, _):
                    break;
                case (-1, _):
                    _ = response.Append($"-{variable}");
                    break;
                case (1, > 0):
                    _ = response.Append($"+{variable}");
                    break;
                case (1, 0):
                    _ = response.Append(variable);
                    break;
                case ( < -1, _):
                    _ = response.Append($"-{count}{variable}");
                    break;
                case ( > 1, 0):
                    _ = response.Append($"{count}{variable}");
                    break;
                case ( > 1, > 0):
                    _ = response.Append($"+{count}{variable}");
                    break;
            }
        }
        return response.ToString();
    }
}
