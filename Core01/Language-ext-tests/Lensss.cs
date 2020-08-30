using System;
using System.Collections.Generic;
using System.Text;
using LanguageExt;
using LanguageExt.ClassInstances;
using static LanguageExt.Prelude;

namespace MarcinGajda.Language_ext_tests
{
    //[WithLens]
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

    //[WithLens]
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

    public static class Testasd
    {
        public static void Teasdasdasd()
        {

            var asdasd = HashSet<EqStringOrdinalIgnoreCase, string>();
        }
        //public static void TEst()
        //{
        //    // Generate a Person with three Appts in a Map
        //    var person = new Person("Paul", "Louth", Map(
        //        (1, new Appt(1, DateTime.Parse("1/1/2010"), ApptState.NotArrived)),
        //        (2, new Appt(2, DateTime.Parse("2/1/2010"), ApptState.NotArrived)),
        //        (3, new Appt(3, DateTime.Parse("3/1/2010"), ApptState.NotArrived))));

        //    // Local function for composing a new lens from 3 other lenses
        //    Lens<Person, ApptState> setState(int id) =>
        //        lens(Person.appts, Map<int, Appt>.item(id), Appt.state);

        //    // Transform
        //    var person2 = setState(2).Set(ApptState.Arrived, person);
        //}
    }
}
