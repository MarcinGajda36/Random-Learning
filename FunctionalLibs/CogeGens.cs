using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using LanguageExt;
using LanguageExt.ClassInstances;
using static LanguageExt.Prelude;

namespace FunctionalLibs
{
    ///////////////
    [Record]
    public partial struct Person1
    {
        public readonly string Forename;
        public readonly string Surname;
    }
    ///////////////

    ///////////////
    [Union]
    public interface Shape
    {
        Shape Rectangle(float width, float length);
        Shape Circle(float radius);
        Shape Prism(float width, float height);
    }
    ///////////////

    ///////////////
    public interface IO
    {
        Seq<string> ReadAllLines(string fileName);
        Unit WriteAllLines(string fileName, Seq<string> lines);
        Person1 ReadFromDB();
        int Zero { get; }
    }
    [Reader(Env: typeof(IO))]
    public partial struct Subsystem<A>
    {
    }
    ///////////////

    ///////////////
    [RWS(WriterMonoid: typeof(MSeq<string>),
         Env: typeof(IO),
         State: typeof(Person1),
         Constructor: "Pure",
         Fail: "Error")]
    public partial struct Subsys<T>
    {
    }
}
