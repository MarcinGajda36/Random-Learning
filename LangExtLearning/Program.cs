﻿using System;
using LanguageExt;
using LanguageExt.ClassInstances;
using static LanguageExt.Prelude;

namespace LangExtLearning
{
    class Program
    {
        record Person
        {
            public string Name { get; init; }
        }
        static void Main(string[] args)
        {
            var p1 = new Person { Name = "asdasd" };
            var refp1 = Ref(p1);
            var refp2 = Ref(p1);
            _ = sync(() =>
            {
                refp1.Swap(p => p with { Name = "Kappa" });
                refp2.Swap(p => p with { Name = "LOL" });
            });

            var (pp1, pp2) = sync(() => (refp1.Value, refp2.Value));

            var l = "asdasd" switch
            {
                "xd" or "cx" => 1,
                _ => 2,
            };

            var atomMap = Atom(HashMap<EqStringOrdinal, string, HashSet<object>>());

            atomMap.Swap("", new object(),
                static (key, value, map) => map.AddOrUpdate(key,
                    previous => previous.Add(value), HashSet(value)));

            var map = HashMap<EqStringOrdinalIgnoreCase, string, int>(("a", 1), ("b", 2));
            map = map.TryAdd("c", 3);
            var set = Set<OrdInt, int>(1, 2, 3);
            set = set.TryAdd(4);
            var hset = HashSet<EqInt, int>(1, 2, 3);
            hset = hset.TryAdd(4);
            var querey = Query(1, 2, 3);
            var queue = Queue(2, 3, 4);
            queue = queue.Enqueue(5);
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
