namespace MarcinGajda.Foreaches;

public class MyCollection
{
    public Enumerator GetEnumerator()
        => new Enumerator();
    public class Enumerator
    {
        public int Current => 1;
        public bool MoveNext() => true;

    }
}
public static class CollectionTest
{
    public static void Test()
    {
        var coll = new MyCollection();
        foreach (int one in coll)
        {

            return;
        }
    }
}
