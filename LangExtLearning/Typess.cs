using System;
using System.Collections.Generic;
using System.Text;
using LanguageExt;

namespace LangExtLearning
{

    [Union]
    public interface Shape
    {
        Shape Rectangle(float width, float length);
        Shape Circle(float radius);
        Shape Prism(float width, float height);
    }

    [Record]
    public partial class Person
    {
        public readonly string Name;
        public readonly string Surname;

        public Person(string name, string surname)
        {
            Name = name;
            Surname = surname;
        }
    }

}
