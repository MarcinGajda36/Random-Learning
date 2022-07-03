using Microsoft.Toolkit.HighPerformance.Buffers;
using System;

namespace HighPerf
{
    ///https://github.com/feature23/StringSimilarity.NET/blob/master/src/F23.StringSimilarity/Levenshtein.cs
    public static class Levenshtein
    {
        public static double Distance(string s1, string s2)
        {
            if (s1.Equals(s2))
            {
                return 0.0;
            }

            int m_len = Math.Max(s1.Length, s2.Length);

            if (m_len == 0)
            {
                return 0.0;
            }

            return ChangesRequired(s1, s2) / m_len;
        }

        public static double ChangesRequired(string s1, string s2)
        {
            if (s1.Equals(s2))
            {
                return 0d;
            }

            if (s1.Length == 0)
            {
                return s2.Length;
            }

            if (s2.Length == 0)
            {
                return s1.Length;
            }

            using SpanOwner<int> v0Owner = SpanOwner<int>.Allocate(s2.Length + 1);
            using SpanOwner<int> v1Owner = SpanOwner<int>.Allocate(s2.Length + 1);
            Span<int> v0 = v0Owner.Span;
            Span<int> v1 = v1Owner.Span;

            for (int i = 0; i < v0.Length; i++)
            {
                v0[i] = i;
            }

            for (int i = 0; i < s1.Length; i++)
            {
                v1[0] = i + 1;

                int minv1 = v1[0];

                for (int j = 0; j < s2.Length; j++)
                {
                    int cost = 1;
                    if (s1[i] == s2[j])
                    {
                        cost = 0;
                    }
                    v1[j + 1] = Math.Min(
                            v1[j] + 1,              // Cost of insertion
                            Math.Min(
                                    v0[j + 1] + 1,  // Cost of remove
                                    v0[j] + cost)); // Cost of substitution

                    minv1 = Math.Min(minv1, v1[j + 1]);
                }

                Span<int> vtemp = v0;
                v0 = v1;
                v1 = vtemp;
            }

            return v0[s2.Length];
        }
    }
}
