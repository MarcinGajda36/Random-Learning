using System;
using System.Threading;
public class PrintInOrder_1114
{
    private readonly SemaphoreSlim afterFirst = new SemaphoreSlim(0, 1);
    private readonly SemaphoreSlim afterSecond = new SemaphoreSlim(0, 1);

    public PrintInOrder_1114()
    {
    }

    public void First(Action printFirst)
    {
        // printFirst() outputs "first". Do not change or remove this line.
        printFirst();
        afterFirst.Release();
    }

    public void Second(Action printSecond)
    {
        // printSecond() outputs "second". Do not change or remove this line.
        afterFirst.Wait();
        printSecond();
        afterSecond.Release();
    }

    public void Third(Action printThird)
    {
        // printThird() outputs "third". Do not change or remove this line.
        afterSecond.Wait();
        printThird();
    }
}
public class PrintInOrder_1114_v2
{

    public PrintInOrder_1114_v2()
    {

    }

    private int first = 0;
    private int second = 0;
    public void First(Action printFirst)
    {
        // printFirst() outputs "first". Do not change or remove this line.
        printFirst();
        Interlocked.Exchange(ref first, 1);
    }

    public void Second(Action printSecond)
    {
        // printSecond() outputs "second". Do not change or remove this line.
        while (first == 0)
        {
            ;
        }

        printSecond();
        Interlocked.Exchange(ref second, 1);
    }

    public void Third(Action printThird)
    {
        // printThird() outputs "third". Do not change or remove this line.
        while (second == 0)
        {
            ;
        }

        printThird();
    }
}