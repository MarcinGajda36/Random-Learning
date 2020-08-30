using System;
using System.Collections.Generic;
using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace FunctionalLibs.WithLensasd
{
    [WithLens]
    public partial class Person : Record<Person>
    {
        public readonly string Name;
        public readonly string Surname;
        public readonly Map<int, Appt> Appts;

        public Person(string name, string surname, Map<int, Appt> appts)
        {
            Name = name;
            Surname = surname;
            Appts = appts;
        }
    }

    [WithLens]
    public partial class Appt : Record<Appt>
    {
        public readonly int Id;
        public readonly DateTime StartDate;
        public readonly ApptState State;

        public Appt(int id, DateTime startDate, ApptState state)
        {
            Id = id;
            StartDate = startDate;
            State = state;
        }
    }

    public enum ApptState
    {
        NotArrived,
        Arrived,
        DNA,
        Cancelled
    }
}
