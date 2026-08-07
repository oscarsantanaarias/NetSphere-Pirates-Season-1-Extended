using System;
using DotNetty.Transport.Channels;
using NLog;

namespace ProudNet.Handlers
{
    internal class FloodGuard : ChannelHandlerAdapter
    {
        private const int WindowMs = 1000;
        private const int MaxMessagesPerWindow = 1000;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                Logger.Warn($"Flood from {context.Channel.RemoteAddress} exceeded {MaxMessagesPerWindow} frames/s, closing");
                context.CloseAsync();
                return;
            }

            context.FireChannelRead(message);
        }
    }
}
