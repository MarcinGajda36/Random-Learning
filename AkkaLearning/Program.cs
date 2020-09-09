using System;
using Akka.Actor;
using Akka.Routing;

namespace AkkaLearning
{
    class Program
    {
        protected static ActorSystem ActorSystem;
        //here you would store your toplevel actor-refs
        protected static IActorRef MyActor;
        static void Main(string[] args)
        {
            //your mvc config. Does not really matter if you initialise
            //your actor system before or after

        }

    }
}
