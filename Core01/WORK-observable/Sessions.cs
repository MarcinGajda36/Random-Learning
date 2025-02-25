using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MarcinGajda.WORK_observable;

internal class Sessions
{
    private readonly ISubject<Session> session = Subject.Synchronize(new Subject<Session>());
    private static readonly TimeSpan interval = TimeSpan.FromSeconds(15);

    public Sessions()
        => session
        .Scan(ImmutableSortedSet.Create<Session>(Session.DateComparer), (set, next) =>
        {
            var now = DateTimeOffset.UtcNow;
            return set.Except(set.TakeWhile(session => now - session.Date > interval));
        });
}

internal class Session
{
    public static readonly Comparer<Session> DateComparer = Comparer<Session>.Create(DateComparison);
    private static int DateComparison(Session left, Session right) => left.Date.CompareTo(right.Date);

    public DateTimeOffset Date { get; }
}
