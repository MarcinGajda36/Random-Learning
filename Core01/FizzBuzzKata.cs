using System;
using System.Linq;

namespace MarcinGajda
{
    public class FizzBuzzKata
    {
        public void FizzBuzzOld()
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 5 == 0 && i % 3 == 0)
                {
                    Console.WriteLine("FizzBuzz");
                }
                else if (i % 5 == 0)
                {
                    Console.WriteLine("Fizz");
                }
                else if (i % 3 == 0)
                {
                    Console.WriteLine("Buzz");
                }
                else
                {
                    Console.WriteLine(i);
                }
            }
        }
        public static void FizzBuzzMy(int start = 0, int limit = 100)
        {
            const string FIZZ = "Fizz";
            const string BUZZ = "Buzz";
            const string FIZZBUZZ = FIZZ + BUZZ;
            static bool isDividableBy5(int toTest) => toTest % 5 == 0;
            static bool isDividableBy3(int toTest) => toTest % 3 == 0;
            static bool isDividableBy3and5(int toTest) => isDividableBy5(toTest) && isDividableBy3(toTest);

            static string GetFizzOrBuzz(int i) => i switch
            {
                int n when isDividableBy3and5(n) => FIZZBUZZ,
                int n when isDividableBy5(n) => FIZZ,
                int n when isDividableBy3(n) => BUZZ,
                int n => n.ToString(),
            };

            for (int i = start; i < limit; ++i)
            {
                string text = GetFizzOrBuzz(i);
                Console.WriteLine(text);
            }
        }

        /*
         * Presentation way
         */
        public static void Print() 
            => Console.WriteLine(FizzBuzzStr(0, 100));

        public static string FizzBuzzStr(int start = 0, int end = 100)
            => Enumerable
                .Range(start, end)
                .Select(FizzOrBuzz)
                .Aggregate((x, y) => x + Environment.NewLine + y);

        public static string FizzOrBuzz(int i) => i switch
        {
            int n when n % (3 * 5) == 0 => "FizzBuzz",
            int n when n % 5 == 0 => "Buzz",
            int n when n % 3 == 0 => "Fizz",
            _ => i.ToString(),
        };
    }
}
