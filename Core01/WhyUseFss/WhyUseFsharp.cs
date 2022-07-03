using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcinGajda.WhyUseFss
{

    public class ShoppingCart<TItem>
    {
        #region ShoppingCart State classes

        /// <summary>         
        /// Represents the Empty state         
        /// </summary>         
        public class EmptyState
        {
            public ShoppingCart<TItem> Add(TItem item)
            {
                TItem[] newItems = new[] { item };
                var newState = new ActiveState(newItems);
                return FromState(newState);
            }
        }

        /// <summary>         
        /// Represents the Active state         
        /// </summary>         
        public class ActiveState
        {
            public ActiveState(IEnumerable<TItem> items)
                => Items = items;

            public IEnumerable<TItem> Items { get; private set; }

            public ShoppingCart<TItem> Add(TItem item)
            {
                var newItems = new List<TItem>(Items) { item };
                var newState = new ActiveState(newItems);
                return FromState(newState);
            }

            public ShoppingCart<TItem> Remove(TItem item)
            {
                var newItems = new List<TItem>(Items);
                newItems.Remove(item);
                if (newItems.Count > 0)
                {
                    var newState = new ActiveState(newItems);
                    return FromState(newState);
                }
                else
                {
                    var newState = new EmptyState();
                    return FromState(newState);
                }
            }

            public ShoppingCart<TItem> Pay(decimal amount)
            {
                var newState = new PaidForState(Items, amount);
                return FromState(newState);
            }


        }

        /// <summary>         
        /// Represents the Paid state        
        /// </summary>         
        public class PaidForState
        {
            public PaidForState(IEnumerable<TItem> items, decimal amount)
            {
                Items = items.ToList();
                Amount = amount;
            }

            public IEnumerable<TItem> Items { get; private set; }
            public decimal Amount { get; private set; }
        }

        #endregion ShoppingCart State classes

        //====================================         
        // Execute of shopping cart proper         
        //==================================== 
        private enum Tag { Empty, Active, PaidFor }
        private readonly Tag _tag = Tag.Empty;
        private readonly object _state;       //has to be a generic object 
        /// <summary>         
        /// Private ctor. Use FromState instead         
        /// </summary>         
        private ShoppingCart(Tag tagValue, object state)
        {
            _state = state;
            _tag = tagValue;
        }

        public static ShoppingCart<TItem> FromState(EmptyState state)
            => new ShoppingCart<TItem>(Tag.Empty, state);

        public static ShoppingCart<TItem> FromState(ActiveState state)
            => new ShoppingCart<TItem>(Tag.Active, state);

        public static ShoppingCart<TItem> FromState(PaidForState state)
            => new ShoppingCart<TItem>(Tag.PaidFor, state);

        /// <summary>         
        /// Create a new empty cart         
        /// </summary>         
        public static ShoppingCart<TItem> NewCart()
        {
            var newState = new EmptyState();
            return FromState(newState);
        }

        /// <summary>         
        /// Call a function for each case of the state         
        /// </summary>        
        /// <remarks>         
        /// Forcing the caller to pass a function for each possible case means that all cases are handled at all times.         
        /// </remarks>         
        public TResult Do<TResult>(
            Func<EmptyState, TResult> emptyFn,
            Func<ActiveState, TResult> activeFn,
            Func<PaidForState, TResult> paidForyFn)
            => _tag switch
            {
                Tag.Empty => emptyFn(_state as EmptyState),
                Tag.Active => activeFn(_state as ActiveState),
                Tag.PaidFor => paidForyFn(_state as PaidForState),
                _ => throw new InvalidOperationException(string.Format("Tag {0} not recognized", _tag)),
            };

        /// <summary>         
        /// Do an action without a return value         
        /// </summary>         
        public void Do(
            Action<EmptyState> emptyFn,
            Action<ActiveState> activeFn,
            Action<PaidForState> paidForyFn)
            =>
            //convert the Actions into Funcs by returning a dummy value
            Do(
                state => { emptyFn(state); return 0; },
                state => { activeFn(state); return 0; },
                state => { paidForyFn(state); return 0; });



    }

    /// <summary>     
    /// Extension methods for my own personal library     
    /// </summary>     
    public static class ShoppingCartExtension
    {
        /// <summary>         
        /// Helper method to Add         
        /// </summary>         
        public static ShoppingCart<TItem> Add<TItem>(this ShoppingCart<TItem> cart, TItem item)
            => cart.Do(
                empty => empty.Add(item), //empty case                 
                active => active.Add(item), //active case                 
                paid => { Console.WriteLine("ERROR: The cart is paid for and items cannot be added"); return cart; }); //paid for case             

        /// <summary>         
        /// Helper method to Remove         
        /// </summary>         
        public static ShoppingCart<TItem> Remove<TItem>(this ShoppingCart<TItem> cart, TItem item)
            => cart.Do(
                empty => { Console.WriteLine("ERROR: The cart is empty and items cannot be removed"); return cart; }, //empty case                 
                active => active.Remove(item), //active case                 
                paid => { Console.WriteLine("ERROR: The cart is paid for and items cannot be removed"); return cart; }); //paid for case             

        /// <summary>         
        /// Helper method to Display         
        /// </summary>         
        public static void Display<TItem>(this ShoppingCart<TItem> cart)
            => cart.Do(
                empty => Console.WriteLine("The cart is empty"),
                active => Console.WriteLine("The active cart contains {0} items", active.Items.Count()),
                paid => Console.WriteLine("The paid cart contains {0} items. Amount paid {1}", paid.Items.Count(), paid.Amount));
    }

    [NUnit.Framework.TestFixture]
    public class CorrectShoppingCartTest
    {
        [NUnit.Framework.Test]
        public void TestCart()
        {
            var emptyCart = ShoppingCart<string>.NewCart();
            emptyCart.Display();

            ShoppingCart<string> cartA = emptyCart.Add("A");  //one item             
            cartA.Display();

            ShoppingCart<string> cartAb = cartA.Add("B");  //two items             
            cartAb.Display();

            ShoppingCart<string> cartB = cartAb.Remove("A"); //one item             
            cartB.Display();

            ShoppingCart<string> emptyCart2 = cartB.Remove("B"); //empty             
            emptyCart2.Display();

            Console.WriteLine("Removing from emptyCart");
            emptyCart.Remove("B"); //error 

            // try to pay for cartA             
            Console.WriteLine("paying for cartA");
            ShoppingCart<string> paidCart = cartA.Do(
                state => cartA,
                state => state.Pay(100),
                state => cartA);
            paidCart.Display();

            Console.WriteLine("Adding to paidCart");
            paidCart.Add("C");

            // try to pay for emptyCart             
            Console.WriteLine("paying for emptyCart");
            ShoppingCart<string> emptyCartPaid = emptyCart.Do(
                state => emptyCart,
                state => state.Pay(100),
                state => emptyCart);
            emptyCartPaid.Display();
        }
    }
}