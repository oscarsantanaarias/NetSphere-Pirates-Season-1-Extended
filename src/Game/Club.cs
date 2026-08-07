using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper.FastCrud;
using Netsphere.Database.Auth;
using Netsphere.Database.Game;
using Netsphere.Network;

namespace Netsphere
{
    internal enum ClubRank : byte
    {
        Master = 0,
        Staff = 1,
        Member = 2
    }

    internal class ClubMember
    {
        public ulong AccountId { get; set; }
        public ClubRank Rank { get; set; }
        public string Nickname { get; set; }
    }

    internal class Club
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public ulong MasterId { get; set; }
        public string Notice { get; set; }
        public string CreatedDate { get; set; }
        public int Level { get; set; }
        public List<ClubMember> Members { get; set; } = new List<ClubMember>();

        public string MasterName
        {
            get
            {
                var master = Members.FirstOrDefault(x => x.Rank == ClubRank.Master);
                return master?.Nickname ?? "";
            }
        }
    }

    internal static class ClubManager
    {
        private static int _clubIdCounter = -1;
        private static int _memberIdCounter = -1;
        private static readonly object IdLock = new object();

        private static int NextClubId(IDbConnection db)
        {
            lock (IdLock)
            {
                if (_clubIdCounter < 0)
                {
                    var rows = db.Find<ClubDto>();
                    _clubIdCounter = rows.Any() ? rows.Max(c => c.Id) : 0;
                }
                return ++_clubIdCounter;
            }
        }

        private static int NextMemberId(IDbConnection db)
        {
            lock (IdLock)
            {
                if (_memberIdCounter < 0)
                {
                    var rows = db.Find<ClubPlayerDto>();
                    _memberIdCounter = rows.Any() ? rows.Max(c => c.Id) : 0;
                }
                return ++_memberIdCounter;
            }
        }

        public static string GetNickname(ulong accountId)
        {
            var online = GameServer.Instance.PlayerManager[accountId];
            if (online != null)
                return online.Account.Nickname ?? "";

            using (var authDb = AuthDatabase.Open())
            {
                var account = authDb.Get(new AccountDto { Id = (int)accountId });
                return account?.Nickname ?? "";
            }
        }

        public static ClubPlayerDto GetMembershipRow(IDbConnection db, ulong accountId)
        {
            var playerId = (int)accountId;
            return db.Find<ClubPlayerDto>(statement => statement
                    .Where($"{nameof(ClubPlayerDto.PlayerId):C} = @{nameof(playerId)}")
                    .WithParameters(new { playerId }))
                .FirstOrDefault();
        }

        public static Club GetClub(uint clubId)
        {
            if (clubId == 0)
                return null;

            using (var db = GameDatabase.Open())
            {
                var id = (int)clubId;
                var row = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Id):C} = @{nameof(id)}")
                        .WithParameters(new { id }))
                    .FirstOrDefault();
                if (row == null)
                    return null;

                return Build(db, row);
            }
        }

        public static Club GetClubByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            using (var db = GameDatabase.Open())
            {
                var row = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Name):C} = @{nameof(name)}")
                        .WithParameters(new { name }))
                    .FirstOrDefault();
                if (row == null)
                    return null;

                return Build(db, row);
            }
        }

        public static Club GetClubByPlayer(ulong accountId)
        {
            using (var db = GameDatabase.Open())
            {
                var membership = GetMembershipRow(db, accountId);
                if (membership == null)
                    return null;

                var id = membership.ClubId;
                var row = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Id):C} = @{nameof(id)}")
                        .WithParameters(new { id }))
                    .FirstOrDefault();
                if (row == null)
                    return null;

                return Build(db, row);
            }
        }

        private static Club Build(IDbConnection db, ClubDto row)
        {
            var clubId = row.Id;
            var memberRows = db.Find<ClubPlayerDto>(statement => statement
                    .Where($"{nameof(ClubPlayerDto.ClubId):C} = @{nameof(clubId)}")
                    .WithParameters(new { clubId }))
                .ToArray();

            var club = new Club
            {
                Id = (uint)row.Id,
                Name = row.Name ?? "",
                Icon = string.IsNullOrWhiteSpace(row.Icon) ? "1-1-1" : row.Icon,
                MasterId = (ulong)row.MasterId,
                Notice = row.Notice ?? "",
                CreatedDate = row.CreatedDate ?? "",
                Level = row.Level
            };

            foreach (var member in memberRows.OrderBy(x => x.Rank).ThenBy(x => x.Id))
            {
                club.Members.Add(new ClubMember
                {
                    AccountId = (ulong)member.PlayerId,
                    Rank = (ClubRank)member.Rank,
                    Nickname = GetNickname((ulong)member.PlayerId)
                });
            }

            return club;
        }

        public static uint Create(string name, string icon, ulong masterAccountId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            using (var db = GameDatabase.Open())
            {
                var existing = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Name):C} = @{nameof(name)}")
                        .WithParameters(new { name }))
                    .FirstOrDefault();
                if (existing != null)
                    return 0;

                if (GetMembershipRow(db, masterAccountId) != null)
                    return 0;

                var clubId = NextClubId(db);
                db.Insert(new ClubDto
                {
                    Id = clubId,
                    Name = name,
                    Icon = string.IsNullOrWhiteSpace(icon) ? "1-1-1" : icon,
                    MasterId = (int)masterAccountId,
                    Notice = "",
                    CreatedDate = DateTimeOffset.Now.ToString("yyyyMMddHH"),
                    Level = 1
                });

                db.Insert(new ClubPlayerDto
                {
                    Id = NextMemberId(db),
                    ClubId = clubId,
                    PlayerId = (int)masterAccountId,
                    Rank = (byte)ClubRank.Master,
                    JoinedDate = DateTimeOffset.Now.ToString("yyyyMMddHH")
                });

                return (uint)clubId;
            }
        }

        public static bool Join(uint clubId, ulong accountId, ClubRank rank = ClubRank.Member)
        {
            if (clubId == 0)
                return false;

            using (var db = GameDatabase.Open())
            {
                if (GetMembershipRow(db, accountId) != null)
                    return false;

                var id = (int)clubId;
                var club = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Id):C} = @{nameof(id)}")
                        .WithParameters(new { id }))
                    .FirstOrDefault();
                if (club == null)
                    return false;

                db.Insert(new ClubPlayerDto
                {
                    Id = NextMemberId(db),
                    ClubId = (int)clubId,
                    PlayerId = (int)accountId,
                    Rank = (byte)rank,
                    JoinedDate = DateTimeOffset.Now.ToString("yyyyMMddHH")
                });

                return true;
            }
        }

        public static bool Leave(ulong accountId)
        {
            using (var db = GameDatabase.Open())
            {
                var membership = GetMembershipRow(db, accountId);
                if (membership == null)
                    return false;

                var clubId = membership.ClubId;
                var wasMaster = (ClubRank)membership.Rank == ClubRank.Master;
                db.Delete(membership);

                var remaining = db.Find<ClubPlayerDto>(statement => statement
                        .Where($"{nameof(ClubPlayerDto.ClubId):C} = @{nameof(clubId)}")
                        .WithParameters(new { clubId }))
                    .OrderBy(x => x.Id)
                    .ToArray();

                if (remaining.Length == 0)
                {
                    var club = db.Find<ClubDto>(statement => statement
                            .Where($"{nameof(ClubDto.Id):C} = @{nameof(clubId)}")
                            .WithParameters(new { clubId }))
                        .FirstOrDefault();
                    if (club != null)
                        db.Delete(club);
                    return true;
                }

                if (wasMaster)
                {
                    var promoted = remaining.First();
                    promoted.Rank = (byte)ClubRank.Master;
                    db.Update(promoted);

                    var club = db.Find<ClubDto>(statement => statement
                            .Where($"{nameof(ClubDto.Id):C} = @{nameof(clubId)}")
                            .WithParameters(new { clubId }))
                        .FirstOrDefault();
                    if (club != null)
                    {
                        club.MasterId = promoted.PlayerId;
                        db.Update(club);
                    }
                }

                return true;
            }
        }

        public static bool SetRank(uint clubId, ulong accountId, ClubRank rank)
        {
            if (clubId == 0)
                return false;

            using (var db = GameDatabase.Open())
            {
                var membership = GetMembershipRow(db, accountId);
                if (membership == null || membership.ClubId != (int)clubId)
                    return false;
                if ((ClubRank)membership.Rank == ClubRank.Master)
                    return false;

                membership.Rank = (byte)rank;
                db.Update(membership);
                return true;
            }
        }

        public static bool SetNotice(uint clubId, string notice)
        {
            if (clubId == 0)
                return false;

            using (var db = GameDatabase.Open())
            {
                var id = (int)clubId;
                var club = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Id):C} = @{nameof(id)}")
                        .WithParameters(new { id }))
                    .FirstOrDefault();
                if (club == null)
                    return false;

                club.Notice = notice ?? "";
                db.Update(club);
                return true;
            }
        }

        public static bool Disband(uint clubId)
        {
            if (clubId == 0)
                return false;

            using (var db = GameDatabase.Open())
            {
                var id = (int)clubId;
                var club = db.Find<ClubDto>(statement => statement
                        .Where($"{nameof(ClubDto.Id):C} = @{nameof(id)}")
                        .WithParameters(new { id }))
                    .FirstOrDefault();
                if (club == null)
                    return false;

                db.Delete(club);
                return true;
            }
        }

        public static IEnumerable<Player> OnlineMembers(Club club)
        {
            if (club == null)
                yield break;

            foreach (var member in club.Members)
            {
                var player = GameServer.Instance.PlayerManager[member.AccountId];
                if (player != null)
                    yield return player;
            }
        }
    }
}
