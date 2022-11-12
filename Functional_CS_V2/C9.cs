using System;

namespace Functional_CS_V2;
internal class C9
{
    /// 1)
    public static int Remainder(int dividend, int divisor) => dividend % divisor;

    // (T1, T2) -> TResult
    // T1 -> T2 -> TResult
    public static Func<T1, TResult> ApplyR<T1, T2, TResult>(T2 t2, Func<T1, T2, TResult> func)
        => t1 => func(t1, t2);

    public static Func<int, int> QuotientOf5 = ApplyR<int, int, int>(5, Remainder);

    public static Func<T1, T2, TResult> ApplyR<T1, T2, T3, TResult>(T3 t3, Func<T1, T2, T3, TResult> func)
        => (t1, t2) => func(t1, t2, t3);

    /// 2)
    public enum NumberType
    {
        Home = 0,
        Mobile,
    }

    public class CountryCode
    {
        private readonly string code;
        private CountryCode(string code) => this.code = code;
        public static CountryCode Create(string code) => new(code);

        public static explicit operator string(CountryCode countryCode) => countryCode.code;
        public static explicit operator CountryCode(string code) => Create(code);
    }

    public class PhoneNumber
    {
        public NumberType NumberType { get; }
        public CountryCode CountryCode { get; }
        public string Number { get; }

        public PhoneNumber(NumberType numberType, CountryCode countryCode, string number)
        {
            NumberType = numberType;
            CountryCode = countryCode;
            Number = number;
        }

        public static PhoneNumber CreateUk(NumberType numberType, string number)
            => new(numberType, CountryCode.Create("uk"), number);

        public static PhoneNumber CreateUkMobile(string number)
            => CreateUk(NumberType.Mobile, number);
    }
}

