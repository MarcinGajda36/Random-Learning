using System;
using LanguageExt;
using static LanguageExt.Prelude;


namespace LangExtLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            var p1 = Person.New("asdasd", "dasdasd");
            var refp1 = Ref(p1);
            sync(() =>
            {
                return refp1.Swap(p => p.With(Name: "Kappa"));
            });

        }
        // you can use C# pattern matching like F#
        public static double GetArea(Shape shape)
            => shape switch
            {
                Rectangle rec => rec.Length * rec.Width,
                Circle circle => 2 * Math.PI * circle.Radius,
                _ => throw new NotImplementedException()
            };
    }
}
