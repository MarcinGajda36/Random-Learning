﻿using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace FunctionalLibs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Lst<int> test = List(1, 2, 3, 4, 5);

            var rctngl = Rectangle.New(0, 0);

            Either<int, string> a = Right("");
        }

        [Union]
        public interface Shape
        {
            Shape Rectangle(float width, float length);
            Shape Circle(float radius);
            Shape Prism(float width, float height);
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
