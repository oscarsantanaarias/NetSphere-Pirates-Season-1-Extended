using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using ProudNet.Codecs;
using ProudNet.Serialization.Messages.Core;

namespace ProudNet.Handlers
{
    internal class UdpHandler : ChannelHandlerAdapter
    {
        private const int MaxUdpPacketsPerWindow = 2000;

        private readonly UdpSocket _socket;
        private readonly ProudServer _server;

        public UdpHandler(UdpSocket socket, ProudServer server)
        {
            _socket = socket;
            _server = server;
        }

        public override void ChannelRead(IChannelHandlerContext context, object obj)
        {
            var message = obj as UdpMessage;
            Debug.Assert(message != null);

            try
            {
                var session = _server.SessionsByUdpId.GetValueOrDefault(message.SessionId);
                if (session == null)
                {
                    if (message.Content == null || message.Content.ReadableBytes < 1)
                        return;

                    if (message.Content.GetByte(0) != (byte)ProudCoreOpCode.ServerHolepunch)
                        return;

                    var holepunch = (ServerHolepunchMessage)CoreMessageDecoder.Decode(message.Content);

                    session = _server.Sessions.Values.FirstOrDefault(x =>
                        x.HolepunchMagicNumber.Equals(holepunch.MagicNumber));
                    if (session == null)
                        return;

                    if (session.UdpSocket != _socket)
                        return;

                    session.UdpSessionId = message.SessionId;
                    session.UdpEndPoint = message.EndPoint;
                    _server.SessionsByUdpId[session.UdpSessionId] = session;

                    session.SendUdpAsync(new ServerHolepunchAckMessage(session.HolepunchMagicNumber, session.UdpEndPoint));
                    return;
                }

                if (session.UdpSocket != _socket)
                    return;

                var nowTick = Environment.TickCount;
                if (nowTick - session.UdpWindowStart >= 1000)
                {
                    session.UdpWindowStart = nowTick;
                    session.UdpPacketCount = 0;
                }
                if (++session.UdpPacketCount > MaxUdpPacketsPerWindow)
                    return;

                session.Channel.Pipeline.Context<ProudFrameDecoder>().FireChannelRead(message.Content.Retain());
            }
            finally
            {
                message.Content.Release();
            }
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var sendContext = message as SendContext;
            Debug.Assert(sendContext != null);
            var coreMessage = sendContext.Message as ICoreMessage;
            Debug.Assert(coreMessage != null);

            var buffer = context.Allocator.Buffer();
            try
            {
                CoreMessageEncoder.Encode(coreMessage, buffer);

                return base.WriteAsync(context, new UdpMessage
                {
                    Flag = 43981,
                    Content = buffer,
                    EndPoint = sendContext.UdpEndPoint
                });
            }
            catch (Exception ex)
            {
                buffer.Release();
                ex.Rethrow();
                throw;
            }
        }
    }
}
