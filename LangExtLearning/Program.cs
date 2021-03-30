using System;
using System.Collections.Generic;
using LanguageExt.ClassInstances;
using static LanguageExt.Prelude;

namespace LangExtLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            //var p1 = Person.New("asdasd", "dasdasd");
            //var refp1 = Ref(p1);
            //sync(() =>
            //{
            //    return refp1.Swap(p => p.With(Name: "Kappa"));
            //});

            var l = "asdasd" switch
            {
                "xd" or "cx" => 1,
                _ => 2,
            };

            var map = HashMap<EqStringOrdinalIgnoreCase, string, int>(("a", 1), ("b", 2));
            var set = Set<OrdInt, int>(1, 2, 3);
            var querey = Query(1, 2, 3);
            var queue = Queue<int>(2, 3, 4);
        }
        // you can use C# pattern matching like F#
        public static double GetArea(Shape shape)
            => shape switch
            {
                Rectangle rec => rec.Length * rec.Width,
                Circle circle => 2 * Math.PI * circle.Radius,
                _ => throw new NotImplementedException()
            };
    }
}
