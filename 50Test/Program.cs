using System;

namespace _50Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var person = new Person("Bill", "Wagner");

            //var (first, last) = person;
            //Console.WriteLine(first);
            //Console.WriteLine(last);
            //Person brother = person with { FirstName = "Paul" };
        }

        public record Person
        {
            public string LastName { get; }
            public string FirstName { get; }

            public Person(string first, string last) => (FirstName, LastName) = (first, last);
        }
    }
}
