using System;

namespace Functional_CS_V2;

internal record Address(string Country);

internal record UsAddress(string State) : Address("us");

internal record Product(string Name, decimal Price, bool IsFood);

internal record Order(Product Product, int Quantity)
{
    public decimal NetPrice => Product.Price * Quantity;
}

public static class Chapter01
{
    private static decimal RateByCountry(string country)
       => country switch
       {
           "it" => 0.22m,
           "jp" => 0.08m,
           _ => throw new ArgumentException($"Missing rate for {country}")
       };

    private static decimal Vat(decimal rate, Order order)
       => order.NetPrice * rate;

    private static decimal Vat(Address address, Order order)
       => address switch
       {
           Address("xD") adrs => DeVat(order),
           ("cd") adrs => Vat(RateByCountry("idk"), order),
           { Country: "lol" } adrs => Vat(RateByCountry(adrs.Country), order),
           UsAddress(var state) => Vat(RateByState(state), order),
           ("de") _ => DeVat(order),
           (var country) _ => Vat(RateByCountry(country), order),
       };

    private static decimal DeVat(Order order)
       => order.NetPrice * (order.Product.IsFood ? 0.08m : 0.2m);

    private static decimal RateByState(string state)
       => state switch
       {
           "ca" => 0.1m,
           "ma" => 0.0625m,
           "ny" => 0.085m,
           _ => throw new ArgumentException($"Missing rate for {state}")
       };
}
