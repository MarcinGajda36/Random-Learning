﻿using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarcinGajda.channels
{
    public class ChannelTests
    {

        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        public async Task WriteThenRead()
        {
            ChannelWriter<int> writer = _channel.Writer;
            ChannelReader<int> reader = _channel.Reader;
            for (int i = 0; i < 10_000_000; i++)
            {
                writer.TryWrite(i);
                await writer.WriteAsync(i);
                await reader.ReadAsync();
                bool read = reader.TryRead(out int element);
            }
            await reader.ReadAllAsync().Select(x => x).ForEachAsync(Console.WriteLine);
        }
    }
}
