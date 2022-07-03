namespace MarcinGajda.GenericsTests
{
    public class GenericUse<TGeneric>
        where TGeneric : IGeneric
    {
        private readonly TGeneric generic;

        public GenericUse(TGeneric generic) => this.generic = generic;


        public int GetPropVal => generic.Prop;
    }
}
