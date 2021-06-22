using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcinGajda.Conversions
{
    public class ClassToIConvert
    {

        public static implicit operator ConvertTo(ClassToIConvert _) => ConvertTo.Empty;
    }
    public class ConvertTo
    {
        public static ConvertTo Empty = new ConvertTo();
    }
    public static class ConvertToExtention
    {
        public static void Random(this ConvertTo convertTo) { }
    }

    public static class Test
    {
        public static void Test1()
        {
            ClassToIConvert classToIConvert = new ClassToIConvert();
            //classToIConvert.Random(); //Doesn't work
        }
    }
}
