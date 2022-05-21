using System;
using System.Linq;

namespace LeetCode
{
    //https://www.codewars.com/kata/5259b20d6021e9e14c0010d4/solutions/rust
    public static class ReverseWordsKata
    {
        public static string ReverseWords(string text)
        {
            const string space = " ";
            var words = text.Split(space);
            var reversedWords = words.Select(word => string.Concat(word.Reverse()));
            return string.Join(space, reversedWords);
        }
    }
}
