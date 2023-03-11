using System;
using System.Threading.Tasks;
using FTypesSharingTest;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using static Types;


namespace MarcinGajda.Fs
{
    internal class FTypesSharing
    {
        public static void Test()
        {
            var e = new Employee(Guid.NewGuid(), "Marcin", "mail", "234-345-645", true, false);
            Console.WriteLine(e);
            var userCreationResult = UserCreationResult.NewInvalidChars("invalid char result");
            FTypesSharingTest.Say.hello("Marcin");
            var vector = new FVector(2, 2);
            Console.WriteLine(vector);
            IFVector vector2 = new FVector(2, 2);
            IFVector vector3 = vector2.Scale(3);
            Console.WriteLine(vector3);
        }

        public static async Task SameSlnShare()
        {
            //string tst = await TypeProvidersTst.GetCoinsTask(TypeProvidersTst.coinmarketcap);

            var p = new Person("Marcin", "Gajda", 25);
            Person ma = Marcin;
            Person mi = Michal;
            var someOther = new SomeOther(1, "");
            Tuple<int, string> randT = someOther.RandomTuple;
            int randM = someOther.RandomMethod(1);

            //var a = TypeProvidersTests.Todos
        }
        public static void FsLib()
            => ListModule.MinBy(FSharpFunc<int, int>.FromConverter(x => x * 2), ListModule.OfArray(Array.Empty<int>()));
    }
}
