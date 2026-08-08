using System;
using DotNetty.Transport.Channels;

namespace ProudNet.Handlers
{
    internal class FloodGuard : ChannelHandlerAdapter
    {
        private const int WindowMs = 1000;
        private const int MaxMessagesPerWindow = 1000;

        private int _windowStart = Environment.TickCount;
        private int _count;

        public FloodGuard(ProudServer server)
        {
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var now = Environment.TickCount;
            if (now - _windowStart >= WindowMs)
            {
                _windowStart = now;
                _count = 0;
            }

            if (++_count > MaxMessagesPerWindow)
            {
                context.CloseAsync();
                return;
            }

            context.FireChannelRead(message);
        }
    }
}
