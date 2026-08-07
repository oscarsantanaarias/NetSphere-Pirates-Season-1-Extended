using System;
using System.Net.Sockets;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace ProudNet.Handlers
{

    internal class ErrorHandler : ChannelHandlerAdapter
    {
        private readonly ProudServer _server;
        public ErrorHandler(ProudServer server)
        {
            _server = server;
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (exception == null)
                return;

            var session = context.Channel.GetAttribute(ChannelAttributes.Session).Get();
            var baseException = exception.GetBaseException();

            _server.RaiseError(new ErrorEventArgs(session, exception));

            if (IsTransportException(exception) || IsTransportException(baseException))
                session?.CloseAsync();
        }

        private static bool IsTransportException(Exception e)
        {
            return e is SocketException
                || e is ClosedChannelException
                || e is DecoderException
                || e is CorruptedFrameException
                || e is TooLongFrameException;
        }
    }
}
