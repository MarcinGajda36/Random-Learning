namespace GrainInterfaces;
using System.Threading.Tasks;
using Orleans;

public interface IHello : IGrainWithIntegerKey
{
    ValueTask<string> SayHello(string greeting);
}
