namespace GrainInterfaces;
using System.Threading.Tasks;

public interface IHello : IGrainWithIntegerKey
{
    ValueTask<string> SayHello(string greeting);
}
