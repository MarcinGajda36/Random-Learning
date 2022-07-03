using Grpc.Core;
using System.Threading.Tasks;

namespace GrpcService1.Services
{
    public class CustomersService : Customer.CustomerBase
    {
        public override async Task GetNewCustomers(
            Unit request,
            IServerStreamWriter<CustomerModel> responseStream,
            ServerCallContext context)
        {
            var customers = new CustomerModel[] {
                new CustomerModel() { FirstName = "Marcin", LastName = "A" },
                new CustomerModel() { FirstName = "Michał", LastName = "B" },
                new CustomerModel() { FirstName = "Ola" },
            };

            foreach (CustomerModel cust in customers)
            {
                await responseStream.WriteAsync(cust);
            }
        }
    }
}
