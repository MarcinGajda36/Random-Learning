using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace MarcinGajda.RXTests
{
    public class MyQueue
    {
        private readonly ISubject<int> subject = Subject.Synchronize(new Subject<int>());

        public IObservable<int> Observable => subject;

        public void Enqueue(int toEnqueue) => subject.OnNext(toEnqueue);

    }
}
