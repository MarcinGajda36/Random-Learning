using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

namespace GrpcClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var channel = GrpcChannel.ForAddress("https://localhost:5001");
        var greeterClient = new Greeter.GreeterClient(channel);

        var request = new HelloRequest() { Name = "Marcin" };
        HelloReply reply = await greeterClient.SayHelloAsync(request);
        Console.WriteLine(reply);

        Console.WriteLine();
        var customerClient = new Customer.CustomerClient(channel);

        using AsyncServerStreamingCall<CustomerModel> custommers = customerClient.GetNewCustomers(new Unit());
        while (await custommers.ResponseStream.MoveNext())
        {
            CustomerModel currentCustomer = custommers.ResponseStream.Current;
            Console.WriteLine(currentCustomer);
        }

        _ = Console.ReadLine();
    }
}
