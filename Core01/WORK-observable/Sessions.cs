using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;

namespace MarcinGajda.WORK_observable
{
    class Sessions
    {
        readonly ISubject<Session> session = Subject.Synchronize(new Subject<Session>());
        static readonly TimeSpan interval = TimeSpan.FromSeconds(15);

        public Sessions()
        {
            session
                .Scan(ImmutableSortedSet.Create<Session>(Session.DateComparer), (set, next) => {
                    var now = DateTimeOffset.UtcNow;
                    return set.Except(set.TakeWhile(session => now - session.Date > interval));
                });
        }
    }
    class Session
    {
        public static readonly Comparer<Session> DateComparer = Comparer<Session>.Create(DateComparison);
        private static int DateComparison(Session left, Session right) => left.Date.CompareTo(right.Date);

        public DateTimeOffset Date { get; }
    }
}
