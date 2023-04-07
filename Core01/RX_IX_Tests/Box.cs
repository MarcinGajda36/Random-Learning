using System;
using System.Reflection.Emit;
using LanguageExt;

namespace MarcinGajda.RX_IX_Tests;
public class Box<TValue>
{
    public readonly TValue Value;

    public static readonly Func<TValue, object> New = typeof(TValue).IsValueType
        ? MakeNewStruct()
        : MakeNewClass();

    public static readonly Func<object, TValue> GetValue = typeof(TValue).IsValueType
        ? GetValueStruct()
        : GetValueClass();

    public Box(TValue value)
    {
        Value = value;
    }

    static Func<object, TValue> GetValueClass()
    {
        if (ILCapability.Available)
        {
            var dynamic = new DynamicMethod("GetValue_Class", typeof(TValue), new[] { typeof(object) }, typeof(TValue).Module, true);
            var il = dynamic.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);

            return (Func<object, TValue>)dynamic.CreateDelegate(typeof(Func<object, TValue>));
        }
        else
        {
            return (object x) => (TValue)x;
        }
    }

    static Func<object, TValue> GetValueStruct()
    {
        if (ILCapability.Available)
        {
            var field = typeof(Box<TValue>).GetField("Value");
            var dynamic = new DynamicMethod("GetValue_Struct", typeof(TValue), new[] { typeof(object) }, typeof(TValue).Module, true);
            var il = dynamic.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, typeof(Box<TValue>));
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            return (Func<object, TValue>)dynamic.CreateDelegate(typeof(Func<object, TValue>));
        }
        else
        {
            return (object x) => ((Box<TValue>)x).Value;
        }
    }

    static Func<TValue, object> MakeNewClass()
    {
        if (ILCapability.Available)
        {
            var dynamic = new DynamicMethod("New_Class", typeof(object), new[] { typeof(TValue) }, typeof(TValue).Module, true);
            var il = dynamic.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);

            return (Func<TValue, object>)dynamic.CreateDelegate(typeof(Func<TValue, object>));
        }
        else
        {
            return static (TValue x) => x;
        }
    }

    static Func<TValue, object> MakeNewStruct()
    {
        if (ILCapability.Available)
        {
            var ctor = typeof(Box<TValue>).GetConstructor(new[] { typeof(TValue) });
            var dynamic = new DynamicMethod("New_Struct", typeof(object), new[] { typeof(TValue) }, typeof(TValue).Module, true);
            var il = dynamic.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return (Func<TValue, object>)dynamic.CreateDelegate(typeof(Func<TValue, object>));
        }
        else
        {
            return static (TValue x) => new Box<TValue>(x);
        }
    }
}