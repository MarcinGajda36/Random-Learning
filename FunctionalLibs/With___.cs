using System;
using System.Collections.Generic;
using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace FunctionalLibs.W
{
    [With]
    public partial class Role
    {
        public readonly string Title;
        public readonly int Salary;

        public Role(string title, int salary)
        {
            Title = title;
            Salary = salary;
        }

        public static Lens<Role, string> title =>
            Lens<Role, string>.New(
                Get: p => p.Title,
                Set: x => p => p.With(Title: x));

        public static Lens<Role, int> salary =>
            Lens<Role, int>.New(
                Get: p => p.Salary,
                Set: x => p => p.With(Salary: x));
    }

    [With]
    public partial class Person
    {
        public readonly string Name;
        public readonly string Surname;
        public readonly Role Role;

        public Person(string name, string surname, Role role)
        {
            Name = name;
            Surname = surname;
            Role = role;
        }

        public static Lens<Person, string> name =>
            Lens<Person, string>.New(
                Get: p => p.Name,
                Set: x => p => p.With(Name: x));

        public static Lens<Person, string> surname =>
            Lens<Person, string>.New(
                Get: p => p.Surname,
                Set: x => p => p.With(Surname: x));

        public static Lens<Person, Role> role =>
            Lens<Person, Role>.New(
                Get: p => p.Role,
                Set: x => p => p.With(Role: x));

        public static Lens<Person, int> salary()
        {
            return lens(Person.role, Role.salary);
        }
    }

}
