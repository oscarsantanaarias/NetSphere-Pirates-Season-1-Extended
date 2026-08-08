using System.Net;
using DotNetty.Transport.Channels;

namespace ProudNet.Handlers
{
    internal class ConnectionThrottle : ChannelHandlerAdapter
    {
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
