using System;
using System.Collections.Generic;
using System.Text;

namespace MarcinGajda.Foreaches
{
    public class MyCollection
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator();
        }
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

}
