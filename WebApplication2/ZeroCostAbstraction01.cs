namespace WebApplication2;

public interface IClock { DateTimeOffset UtcNow(); }
public struct Clock : IClock
{
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}

public interface IValidator<T> { bool IsValid(T value); }
public record SomethingWithDate(DateTimeOffset SomeDate);
public class DateValidator<TClock> : IValidator<SomethingWithDate>
    where TClock : struct, IClock
{
    public bool IsValid(SomethingWithDate value)
        => default(TClock).UtcNow().Date > value.SomeDate.Date;
}

