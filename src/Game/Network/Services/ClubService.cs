using System.Threading.Tasks;
using BlubLib.DotNetty.Handlers.MessageHandling;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using NLog;
using NLog.Fluent;
using ProudNet.Handlers;

namespace Netsphere.Network.Services
{
    internal class ClubService : ProudMessageHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [MessageHandler(typeof(CClubAddressReqMessage))]
        public async Task CClubAddressReq(GameSession session, CClubAddressReqMessage message)
        {
            Logger.Debug()
                .Account(session)
                .Message($"RequestId:{message.RequestId} LanguageId:{message.LanguageId} Command:{message.Command}")
                .Write();

            await session.SendAsync(new SClubAddressAckMessage("Kappa", 123))
                .ConfigureAwait(false);
        }

        [MessageHandler(typeof(CClubInfoReqMessage))]
        public void CClubInfoReq(GameSession session)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            session.SendAsync(new SClubInfoAckMessage(BuildOwnClubInfo(plr)));
        }

        [MessageHandler(typeof(CGetClubInfoReqMessage))]
        public void CGetClubInfoReq(GameSession session, CGetClubInfoReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            var club = ClubManager.GetClubByName(message.Unk) ?? ClubManager.GetClubByPlayer(plr.Account.Id);
            session.SendAsync(new SGetClubInfoAckMessage { ClubInfo = BuildClubInfo(club) });
        }

        [MessageHandler(typeof(CGetClubInfoByNameReqMessage))]
        public void CGetClubInfoByNameReq(GameSession session, CGetClubInfoByNameReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            var club = ClubManager.GetClubByName(message.Unk);
            session.SendAsync(new SGetClubInfoAckMessage { ClubInfo = BuildClubInfo(club) });
        }

        [MessageHandler(typeof(CClubJoinReqMessage))]
        public void CClubJoinReq(GameSession session, CClubJoinReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            if (ClubManager.GetClubByPlayer(plr.Account.Id) != null)
            {
                session.SendAsync(new SClubJoinAckMessage { Unk = 1, Message = "Already in a club" });
                return;
            }

            var club = ClubManager.GetClubByName(message.Unk2);
            if (club == null)
            {
                session.SendAsync(new SClubJoinAckMessage { Unk = 1, Message = "Club not found" });
                return;
            }

            if (!ClubManager.Join(club.Id, plr.Account.Id))
            {
                session.SendAsync(new SClubJoinAckMessage { Unk = 1, Message = "Unable to join" });
                return;
            }

            session.SendAsync(new SClubJoinAckMessage { Unk = 0, Message = "" });
            session.SendAsync(new SClubInfoAckMessage(BuildOwnClubInfo(plr)));
        }

        [MessageHandler(typeof(CClubUnJoinReqMessage))]
        public void CClubUnJoinReq(GameSession session, CClubUnJoinReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            var left = ClubManager.Leave(plr.Account.Id);
            session.SendAsync(new SClubUnJoinAckMessage { Unk = left ? 0u : 1u });
            session.SendAsync(new SClubInfoAckMessage(BuildOwnClubInfo(plr)));
        }

        [MessageHandler(typeof(CClubNoticeChangeReqMessage))]
        public void CClubNoticeChangeReq(GameSession session, CClubNoticeChangeReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            var club = ClubManager.GetClubByPlayer(plr.Account.Id);
            if (club != null)
            {
                var member = FindMember(club, plr.Account.Id);
                if (member != null && member.Rank <= ClubRank.Staff)
                    ClubManager.SetNotice(club.Id, message.Unk);
            }

            session.SendAsync(new SClubInfoAckMessage(BuildOwnClubInfo(plr)));
        }

        [MessageHandler(typeof(CClubHistoryReqMessage))]
        public void CClubHistoryReq(GameSession session)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            session.SendAsync(new SClubHistoryAckMessage());
        }

        private static ClubMember FindMember(Club club, ulong accountId)
        {
            if (club == null)
                return null;

            foreach (var member in club.Members)
            {
                if (member.AccountId == accountId)
                    return member;
            }

            return null;
        }

        private static PlayerClubInfoDto BuildOwnClubInfo(Player plr)
        {
            var club = ClubManager.GetClubByPlayer(plr.Account.Id);
            if (club == null)
                return new PlayerClubInfoDto();

            return new PlayerClubInfoDto
            {
                Unk5 = club.Id,
                Unk9 = club.Name,
                ModeratorName = club.MasterName
            };
        }

        private static ClubInfoDto BuildClubInfo(Club club)
        {
            if (club == null)
                return new ClubInfoDto();

            return new ClubInfoDto
            {
                Unk1 = club.Name,
                Unk2 = club.MasterName,
                Unk3 = club.Notice ?? "",
                Unk6 = (ushort)club.Members.Count
            };
        }
    }
}
