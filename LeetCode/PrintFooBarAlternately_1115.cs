using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;

public class FooBar
{
    private readonly int n;
    private volatile int foo = 0;
    private volatile int bar = 1;

    public FooBar(int n) => this.n = n;

    public void Foo(Action printFoo)
    {
        for (var i = 0; i < n; i++)
        {
            // printFoo() outputs "foo". Do not change or remove this line.
            while (Interlocked.CompareExchange(ref foo, 1, 0) != 0)
            {
                ;
            }

            printFoo();
            _ = Interlocked.Exchange(ref bar, 0);
        }
    }

    public void Bar(Action printBar)
    {
        for (var i = 0; i < n; i++)
        {
            // printBar() outputs "bar". Do not change or remove this line.
            while (Interlocked.CompareExchange(ref bar, 1, 0) != 0)
            {
                ;
            }

            printBar();
            _ = Interlocked.Exchange(ref foo, 0);
        }
    }
}

public class FooBar2
{
    private readonly int n;
    private readonly SemaphoreSlim foo = new(1, 1);
    private readonly SemaphoreSlim bar = new(0, 1);

    public FooBar2(int n) => this.n = n;

    public void Foo(Action printFoo)
    {
        for (var i = 0; i < n; i++)
        {
            // printFoo() outputs "foo". Do not change or remove this line.
            foo.Wait();
            printFoo();
            _ = bar.Release();
        }
    }

    public void Bar(Action printBar)
    {
        for (var i = 0; i < n; i++)
        {
            // printBar() outputs "bar". Do not change or remove this line.
            bar.Wait();
            printBar();
            _ = foo.Release();
        }
    }
}

public class FooBar1
{
    private enum Print : byte
    {
        None, Foo, Bar
    }

    private readonly int n;
    private readonly ActionBlock<(Print, Action)> printer; //no dataflow
    private Print lastPrint = Print.None;

    public FooBar1(int n)
    {
        this.n = n;
        printer = new ActionBlock<(Print Print, Action Action)>(printAction =>
        {
            if (printAction.Print == lastPrint)
            {
                _ = printer.Post(printAction);
            }
            else
            {
                printAction.Action();
                lastPrint = printAction.Print;
            }
        }, new ExecutionDataflowBlockOptions { EnsureOrdered = false });
    }

    public void Foo(Action printFoo)
    {
        for (var i = 0; i < n; i++)
        {
            // printFoo() outputs "foo". Do not change or remove this line.
            printFoo();
        }
    }

    public void Bar(Action printBar)
    {
        for (var i = 0; i < n; i++)
        {
            // printBar() outputs "bar". Do not change or remove this line.
            printBar();
        }
    }
}