using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MarcinGajda.RX_IX_Tests;

internal class StatefullAsyncEnumerable
{

    public record Message(int Value);
    public record AddMessage(int Value) : Message(Value);
    public record RemoveMessage(int Value) : Message(Value);

    public ImmutableArray<int> State1 { get; set; } = ImmutableArray<int>.Empty;

    private ImmutableDictionary<int, string> state2 = ImmutableDictionary<int, string>.Empty;
    public ImmutableDictionary<int, string> State2 { get => state2; set => state2 = value; }

    private async Task<ImmutableDictionary<int, string>> SomeAsyncStaff(Message message)
    {
        return message switch
        {
            AddMessage(var value) => State2.Add(value, await Task.FromResult(value.ToString())),
            RemoveMessage(var value) => State2.Remove(value),
            var unknown => throw new NotSupportedException($"Unknown {unknown}"),
        };
    }

    public async IAsyncEnumerable<Message> GetIAsyncEnumerable(IAsyncEnumerable<Message> messages)
    {
        await foreach (var message in messages)
        {
            State1 = message switch
            {
                AddMessage(var value) => State1.Add(value),
                RemoveMessage(var value) => State1.Remove(value),
                var unknown => throw new NotSupportedException($"Unknown {unknown}"),
            };
            State2 = await SomeAsyncStaff(message);

            // Now we know that when this message returns then State1 and State2 are up to date
            yield return message;
        }
    }

    public IObservable<Message> GetIObservable(IObservable<Message> messages)
    {
        return messages
            .Do(message =>
            {
                State1 = message switch
                {
                    AddMessage(var value) => State1.Add(value),
                    RemoveMessage(var value) => State1.Remove(value),
                    var unknown => throw new NotSupportedException($"Unknown {unknown}"),
                };
            })
            .SelectMany(async message =>
            {
                _ = await SomeAsyncStaff2(message);
                return message;
            });
    }

    private async Task<Unit> SomeAsyncStaff2(Message message)
    {
        return message switch
        {
            AddMessage(var value) => await AddOrUpdate(value),
            RemoveMessage(var value) => Remove(value),
            var unknown => throw new NotSupportedException($"Unknown {unknown}"),
        };
    }

    private Unit Remove(int value)
    {
        ImmutableInterlocked.TryRemove(ref state2, value, out _);
        return Unit.Default;
    }

    private async Task<Unit> AddOrUpdate(int value)
    {
        var newValue = await Task.FromResult(value.ToString());
        ImmutableInterlocked.AddOrUpdate(ref state2, value, _ => newValue, (_, _) => newValue);
        return Unit.Default;
    }
}
