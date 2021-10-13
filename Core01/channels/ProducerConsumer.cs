using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarcinGajda.channels
{
    public class ProducerConsumer
    {
        private readonly Channel<int> channel = Channel.CreateBounded<int>(new BoundedChannelOptions(4096) { });
        private ChannelWriter<int> Writer => channel.Writer;

        public ChannelReader<int> Reader => channel.Reader;

        public ValueTask Produce(int toProduce) => Writer.WriteAsync(toProduce);

    }
}
