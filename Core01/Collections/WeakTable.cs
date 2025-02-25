using System;
using System.Runtime.CompilerServices;

namespace MarcinGajda.Collections;

public static class WeakRefTest
{
    private static readonly object first = new { Test = 1 };
    public static void Test1()
    {
        var cwt = new ConditionalWeakTable<object, string>();
        object? second = new { Test = 2 };
        object third = new { Test = 3 };
        cwt.Add(first, "First");
        cwt.Add(second, "Second");
        cwt.Add(third, "Third");
        second = null;

        GC.Collect();

        Console.WriteLine("dasdasdasd");
        foreach (var adasd in cwt)
        {
            Console.WriteLine(adasd.Value);
        }

        Console.ReadKey();
    }
    public static void Test()
    {

        var mc1 = new ManagedClass();
        ManagedClass? mc2 = new ManagedClass();
        var mc3 = new ManagedClass();

        var cwt = new ConditionalWeakTable<ManagedClass, ClassData>
        {
            { mc1, new ClassData() },
            { mc2, new ClassData() },
            { mc3, new ClassData() }
        };

        var wr2 = new WeakReference(mc2);
        mc2 = null;

        GC.Collect();

        ClassData? data = null;

        if (wr2.Target == null)
        {
            Console.WriteLine("No strong reference to mc2 exists.");
        }
        else if (cwt.TryGetValue((ManagedClass)wr2.Target, out data))
        {
            Console.WriteLine("Data created at {0}", data.CreationTime);
        }
        else
        {
            Console.WriteLine("mc2 not found in the table.");
        }
    }

    public class ManagedClass
    {
    }

    public class ClassData
    {
        public DateTime CreationTime;
        public object Data;

        public ClassData()
        {
            CreationTime = DateTime.Now;
            Data = new object();
        }
    }
}

