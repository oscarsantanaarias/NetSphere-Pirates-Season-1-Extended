using System.Linq;
using System.Threading.Tasks;
using BlubLib.DotNetty.Handlers.MessageHandling;
using ExpressMapper.Extensions;
using Netsphere.Network.Data.Chat;
using Netsphere.Network.Message.Chat;
using ProudNet.Handlers;

namespace Netsphere.Network.Services
{
    internal class CommunityService : ProudMessageHandler
    {
        [MessageHandler(typeof(CSetUserDataReqMessage))]
        public async Task SetUserDataHandler(ChatSession session, CSetUserDataReqMessage message)
        {
            var plr = session.Player;
            if (message.UserData.ChannelId > 0 && !plr.SentPlayerList && plr.Channel != null)
            {
                // We can't send the channel player list in Channel.Join because the client only accepts it here :/
                plr.SentPlayerList = true;
                var data = plr.Channel.Players.Values.Select(p => p.Map<Player, UserDataWithNickDto>()).ToArray();
                await session.SendAsync(new SChannelPlayerListAckMessage(data))
                    .ConfigureAwait(false);
            }

            // Save settings if any of them changed
            var settings = plr.Settings;
            var name = nameof(UserDataDto.AllowCombiInvite);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowCombiInvite)
                settings.AddOrUpdate(name, message.UserData.AllowCombiInvite);

            name = nameof(UserDataDto.AllowFriendRequest);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowFriendRequest)
                settings.AddOrUpdate(name, message.UserData.AllowFriendRequest);

            name = nameof(UserDataDto.AllowRoomInvite);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowRoomInvite)
                settings.AddOrUpdate(name, message.UserData.AllowRoomInvite);

            name = nameof(UserDataDto.AllowInfoRequest);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowInfoRequest)
                settings.AddOrUpdate(name, message.UserData.AllowInfoRequest);
        }

        [MessageHandler(typeof(CGetUserDataReqMessage))]
        public async Task GetUserDataHandler(ChatSession session, CGetUserDataReqMessage message)
        {
            var plr = session.Player;
            if (plr.Account.Id == message.AccountId)
            {
                await session.SendAsync(new SUserDataAckMessage(plr.Map<Player, UserDataDto>()))
                    .ConfigureAwait(false);
                return;
            }

            Player target;
            if (!plr.Channel.Players.TryGetValue(message.AccountId, out target))
                return;

            switch (target.Settings.Get<CommunitySetting>(nameof(UserDataDto.AllowInfoRequest)))
            {
                case CommunitySetting.Deny:
                    // Not sure if there is an answer to this
                    return;

                case CommunitySetting.FriendOnly:
                    // ToDo
                    return;
            }

            await session.SendAsync(new SUserDataAckMessage(target.Map<Player, UserDataDto>()))
                .ConfigureAwait(false);
        }

        [MessageHandler(typeof(CDenyChatReqMessage))]
        public async Task DenyHandler(ChatServer service, ChatSession session, CDenyChatReqMessage message)
        {
            var plr = session.Player;

            if (message.Deny.AccountId == plr.Account.Id)
                return;

            Deny deny;
            switch (message.Action)
            {
                case DenyAction.Add:
                    if (plr.DenyManager.Contains(message.Deny.AccountId))
                        return;

                    var target = GameServer.Instance.PlayerManager[message.Deny.AccountId];
                    if (target == null)
                        return;

                    deny = plr.DenyManager.Add(target);
                    await session.SendAsync(new SDenyChatAckMessage(0, DenyAction.Add, deny.Map<Deny, DenyDto>()))
                        .ConfigureAwait(false);
                    break;

                case DenyAction.Remove:
                    deny = plr.DenyManager[message.Deny.AccountId];
                    if (deny == null)
                        return;

                    plr.DenyManager.Remove(message.Deny.AccountId);
                    await session.SendAsync(new SDenyChatAckMessage(0, DenyAction.Remove, deny.Map<Deny, DenyDto>()))
                        .ConfigureAwait(false);
                    break;
            }
        }
        [MessageHandler(typeof(CFriendReqMessage))]  //friend reqs
        public void FriendRequest(ChatSession session, CFriendReqMessage message)
        {
            var me = session.Player;
            if (me?.Account == null || message.AccountId == me.Account.Id)
                return;

            var target = GameServer.Instance.PlayerManager[message.AccountId];

            switch (message.Action)
            {
                case 0: // Add / request
                    if (target == null)
                    {
                        session.SendAsync(new SFriendAckMessage(1)); // UserNotExist
                        return;
                    }
                    if (me.Friends.ContainsKey(target.Account.Id))
                        return;

                    var setting = nameof(UserDataDto.AllowFriendRequest);
                    var allows = target.Settings.Contains(setting) &&
                                 target.Settings.Get<CommunitySetting>(setting) == CommunitySetting.Allow;
                    if (!allows)
                    {
                        session.SendAsync(new SFriendAckMessage(1));
                        return;
                    }

                    me.Friends[target.Account.Id] = 1;   // Requesting
                    target.Friends[me.Account.Id] = 3;   // RequestDialog

                    session.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = target.Account.Id, Nickname = target.Account.Nickname, State = 1 }
                    });
                    target.ChatSession?.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = me.Account.Id, Nickname = me.Account.Nickname, State = 3 }
                    });
                    break;

                case 2: // Update / accept
                    if (target == null)
                        return;
                    me.Friends[target.Account.Id] = 2;   // InList
                    target.Friends[me.Account.Id] = 2;

                    session.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = target.Account.Id, Nickname = target.Account.Nickname, State = 2 }
                    });
                    target.ChatSession?.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = me.Account.Id, Nickname = me.Account.Nickname, State = 2 }
                    });
                    break;

                case 1: // Remove
                case 3: // Decline
                    uint removed;
                    me.Friends.TryRemove(message.AccountId, out removed);
                    if (target != null)
                        target.Friends.TryRemove(me.Account.Id, out removed);

                    session.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = message.AccountId, State = 0 }
                    });
                    target?.ChatSession?.SendAsync(new SFriendAckMessage
                    {
                        Result = 0,
                        Friend = new FriendDto { AccountId = me.Account.Id, State = 0 }
                    });
                    break;
            }
        }
    }
}
