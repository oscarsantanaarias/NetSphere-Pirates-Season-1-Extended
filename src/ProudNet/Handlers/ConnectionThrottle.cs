using System.Net;
using DotNetty.Transport.Channels;
using NLog;

namespace ProudNet.Handlers
{
    internal class ConnectionThrottle : ChannelHandlerAdapter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ProudServer _server;
        private IPAddress _ip;
        private bool _counted;

        public ConnectionThrottle(ProudServer server)
        {
            _server = server;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _ip = (context.Channel.RemoteAddress as IPEndPoint)?.Address;

            if (!_server.TryAddConnection(_ip))
            {
                Logger.Warn($"Too many connections from {_ip}, refusing (cap {ProudServer.MaxConnectionsPerIp})");
                context.CloseAsync();
                return;
            }

            _counted = _ip != null;
            base.ChannelActive(context);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (_counted)
                _server.RemoveConnection(_ip);
            base.ChannelInactive(context);
        }
    }
}
