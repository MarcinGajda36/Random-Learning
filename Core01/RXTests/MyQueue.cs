using System;
using System.Reactive.Subjects;

namespace MarcinGajda.RXTests
{
    public class MyQueue
    {
        private readonly ISubject<int> subject = Subject.Synchronize(new Subject<int>());

        public IObservable<int> Observable => subject;

        public void Enqueue(int toEnqueue) => subject.OnNext(toEnqueue);

    }
}
