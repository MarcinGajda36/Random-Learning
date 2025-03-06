namespace MarcinGajda;

internal class PasteJsonAsClass
{
    public class Rootobject
    {
        public Story[] Stories { get; set; }
    }

    public class Story
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public Scenario[] Scenarios { get; set; }
    }

    public class Scenario
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public Step[] Steps { get; set; }
    }

    public class Step
    {
        public string Name { get; set; }
        public int Duration { get; set; }
    }
}
