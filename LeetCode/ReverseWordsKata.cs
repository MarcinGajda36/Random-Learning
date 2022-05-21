using System;
using System.Linq;

namespace LeetCode
{
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
