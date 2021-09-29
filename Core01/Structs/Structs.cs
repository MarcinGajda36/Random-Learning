using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace MarcinGajda.Structsssss
{
    public struct MutationsTests
    {
        public int X { get; set; }
    }
    public record MutationTest2
    {
        public MutationsTests MutationsTests { get; set; }

        public static void Test(Index index)
        {
            Test(^1);
            var idx = ^1;
            var newTest = new MutationTest2 { MutationsTests = new MutationsTests() { X = 1 } };
            var dupa = newTest.MutationsTests;
            dupa.X = 5;
            var a = new HttpStyleUriParser() { };
            var cd = new CompositeDisposable(Disposable.Empty, Disposable.Empty) { };
        }
    }
    public readonly ref struct ReadOnlyRefStruct
    {
        private readonly string Y { get; }
        private readonly int X { get; }
        public ReadOnlyRefStruct(int x, string y)
        {
            Y = y;
            X = x;
        }
        public static void ReadOnlyRefStructTEst()
        {
            var readOnlyRefStruct1 = new ReadOnlyRefStruct(1, "ABC");
            ref ReadOnlyRefStruct readOnlyRefStruct2 = ref readOnlyRefStruct1;
            Span<int> span = stackalloc int[] { 1, 2, 3 };
            ReadOnlyRefStruct test = readOnlyRefStruct2;
        }
    }
    public readonly struct Point
    {

        public static readonly Point random = new Point(5, 5);
        public static ref readonly Point GetRandom() => ref random;

        public Point(double x, double y)
            => (this.x, Y) = (x, y);

        private readonly double x;
        public double X { get => x; }
        //public double X { get; }
        public double Y { get; }

        public void TestOuts(out object x, out object y, out string t)
            => (x, y, t) = (X, Y, "");
        public void Deconstruct(out double x, out double y)
            => (x, y) = (X, Y);
    }
    public static class Structs
    {
        static void TestSpan()
        {
            int[] arr = new int[] { 1, 2, 3, 4 };
            Span<int> span = arr.AsSpan(1, 2);
            int first = span[0];
        }
        public static void TestParams(params (string, object)[] parameters)
        {
            (string fst1, object scnd1) = new ValueTuple<string, object>();
            (string? fst2, object? scnd2) = new ValueTuple<string, object>();
            (string fst, object scnd) = new KeyValuePair<string, object>();
        }

        public class BlogPost : IEquatable<BlogPost>
        {
            public string Slug { get; }
            public string Title { get; }
            public string Body { get; }
            public DateTime DatePublished { get; }

            //public BlogPost(string Slug, string Title, string Body, DateTime DatePublished)
            //{
            //    this.Slug = Slug;
            //    this.Title = Title;
            //    this.Body = Body;
            //    this.DatePublished = DatePublished;
            //}
            public BlogPost(string slug, string title, string body, DateTime datePublished)
                => (Slug, Title, Body, DatePublished) = (slug, title, body, datePublished);

            public bool Equals(BlogPost other)
            {
                return Equals(Slug, other.Slug) && Equals(Title, other.Title) && Equals(Body, other.Body) && Equals(DatePublished, other.DatePublished);
            }

            public override bool Equals(object? other)
            {
                return (other as BlogPost)?.Equals(this) == true;
            }

            public override int GetHashCode()
            {
                return (Slug.GetHashCode() * 17 + Title.GetHashCode() + Body.GetHashCode() + DatePublished.GetHashCode());
            }

            public void Deconstruct(out string Slug, out string Title, out string Body, out DateTime DatePublished)
            {
                Slug = this.Slug;
                Title = this.Title;
                Body = this.Body;
                DatePublished = this.DatePublished;
            }

            //public BlogPost With(string Slug = this.Slug, string Title = this.Title, string Body = this.Body, DateTime DatePublished = this.DatePublished)
            //    => new BlogPost(Slug, Title, Body, DatePublished);
        }
    }
}
