using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlubLib.DotNetty.Handlers.MessageHandling;
using Dapper.FastCrud;
using Netsphere.Database.Game;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using NLog;
using NLog.Fluent;
using ProudNet.Handlers;

namespace Netsphere.Network.Services
{
    internal class MissionService : ProudMessageHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Random Random = new Random();

        public static async Task SendMissionInfo(GameSession session)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            TaskDto[] tasks = Array.Empty<TaskDto>();

            try
            {
                var rows = await LoadRows(plr).ConfigureAwait(false);
                await FillEmptySlots(plr, rows).ConfigureAwait(false);
                var resource = GameServer.Instance.ResourceCache.GetTasks();

                tasks = rows
                    .Select(row => new { row, info = resource.FirstOrDefault(t => t.Id == row.MissionId) })
                    .Where(x => x.info != null)
                    .Select(x => new TaskDto
                    {
                        Id = (uint)x.row.MissionId,
                        Slot = (byte)x.row.Slot,
                        Progress = (ushort)x.row.Progress,
                        RewardType = MissionRewardType.PEN,
                        Reward = x.info.Reward
                    })
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to load missions: {ex.Message}")
                    .Write();
            }

            await session.SendAsync(new STaskInfoAckMessage { Tasks = tasks })
                .ConfigureAwait(false);
        }

        [MessageHandler(typeof(CTaskRequestReqMessage))]
        public async Task TaskRequestReq(GameSession session, CTaskRequestReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
            {
                await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                    .ConfigureAwait(false);
                return;
            }

            if ((message.Type != 1 && message.Type != 2) || message.Slot > 2 || message.Level > 4)
            {
                await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                    .ConfigureAwait(false);
                return;
            }

            Resource.TaskInfo picked = null;
            try
            {
                var rows = await LoadRows(plr).ConfigureAwait(false);
                var taken = rows.Select(row => (uint)row.MissionId).ToArray();

                var candidates = GameServer.Instance.ResourceCache.GetTasks()
                    .Where(t => t.Type == message.Type && t.Level == message.Level)
                    .Where(t => !taken.Contains(t.Id))
                    .Where(t => t.MinLevel == 0 || plr.Level >= t.MinLevel)
                    .Where(t => t.MaxLevel == 0 || plr.Level <= t.MaxLevel)
                    .ToArray();

                picked = Pick(candidates, plr.Level);
                if (picked == null)
                {
                    await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                        .ConfigureAwait(false);
                    return;
                }

                using (var db = GameDatabase.Open())
                {
                    await db.InsertAsync(new PlayerMissionDto
                    {
                        PlayerId = (int)plr.Account.Id,
                        MissionId = (int)picked.Id,
                        Slot = message.Slot,
                        Progress = 0,
                        Completed = false
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to assign mission: {ex.Message}")
                    .Write();

                await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                    .ConfigureAwait(false);
                return;
            }

            await session.SendAsync(new STaskRequestAckMessage
            {
                TaskId = picked.Id,
                RewardType = MissionRewardType.PEN,
                Reward = picked.Reward,
                Slot = message.Slot
            }).ConfigureAwait(false);
        }

        [MessageHandler(typeof(CTaskNotifyReqMessage))]
        public async Task TaskNotifyReq(GameSession session, CTaskNotifyReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            var info = GameServer.Instance.ResourceCache.GetTasks().FirstOrDefault(t => t.Id == message.TaskId);
            if (info == null)
                return;

            var progress = message.Progress > info.Goal ? info.Goal : message.Progress;
            var completed = false;

            try
            {
                using (var db = GameDatabase.Open())
                {
                    var row = (await db.FindAsync<PlayerMissionDto>(statement => statement
                            .Where($"{nameof(PlayerMissionDto.PlayerId):C} = @PlayerId AND {nameof(PlayerMissionDto.MissionId):C} = @MissionId")
                            .WithParameters(new { PlayerId = (int)plr.Account.Id, MissionId = (int)message.TaskId }))
                        .ConfigureAwait(false)).FirstOrDefault();

                    if (row == null || row.Completed)
                        return;

                    row.Progress = progress;
                    completed = info.Goal > 0 && progress >= info.Goal;
                    row.Completed = completed;
                    await db.UpdateAsync(row).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to store mission progress: {ex.Message}")
                    .Write();
                return;
            }

            await session.SendAsync(new STaskUpdateAckMessage { TaskId = message.TaskId, Progress = (ushort)progress })
                .ConfigureAwait(false);

            if (!completed)
                return;

            plr.PEN += info.Reward;
            await session.SendAsync(new SRefreshCashInfoAckMessage { PEN = plr.PEN, AP = plr.AP })
                .ConfigureAwait(false);
        }

        // the client hardcodes "???" / "ERROR" for a slot without a task, so every slot of
        // every level is kept filled: 5x3 compulsory + 4x3 weekly
        private static async Task FillEmptySlots(Player plr, List<PlayerMissionDto> rows)
        {
            var resource = GameServer.Instance.ResourceCache.GetTasks();
            var mine = rows
                .Select(row => new { row, info = resource.FirstOrDefault(t => t.Id == row.MissionId) })
                .Where(x => x.info != null)
                .ToList();

            var added = new List<PlayerMissionDto>();

            for (byte type = 1; type <= 2; type++)
            {
                var maxLevel = type == 1 ? 4 : 3;
                for (byte level = 0; level <= maxLevel; level++)
                {
                    var here = mine.Where(x => x.info.Type == type && x.info.Level == level).ToList();
                    if (here.Count >= 3)
                        continue;

                    var taken = mine.Select(x => x.info.Id).ToArray();
                    var candidates = resource
                        .Where(t => t.Type == type && t.Level == level)
                        .Where(t => !taken.Contains(t.Id))
                        .Where(t => t.MinLevel == 0 || plr.Level >= t.MinLevel)
                        .Where(t => t.MaxLevel == 0 || plr.Level <= t.MaxLevel)
                        .ToList();

                    for (var slot = 0; slot < 3; slot++)
                    {
                        if (here.Any(x => x.row.Slot == slot))
                            continue;

                        var picked = Pick(candidates.ToArray(), plr.Level);
                        if (picked == null)
                            break;

                        candidates.Remove(picked);
                        var row = new PlayerMissionDto
                        {
                            PlayerId = (int)plr.Account.Id,
                            MissionId = (int)picked.Id,
                            Slot = slot,
                            Progress = 0,
                            Completed = false
                        };
                        added.Add(row);
                        here.Add(new { row, info = picked });
                        mine.Add(new { row, info = picked });
                    }
                }
            }

            if (added.Count == 0)
                return;

            using (var db = GameDatabase.Open())
            {
                foreach (var row in added)
                    await db.InsertAsync(row).ConfigureAwait(false);
            }

            rows.AddRange(added);
        }

        private static async Task<List<PlayerMissionDto>> LoadRows(Player plr)
        {
            using (var db = GameDatabase.Open())
            {
                return (await db.FindAsync<PlayerMissionDto>(statement => statement
                        .Where($"{nameof(PlayerMissionDto.PlayerId):C} = @PlayerId")
                        .WithParameters(new { PlayerId = (int)plr.Account.Id }))
                    .ConfigureAwait(false)).ToList();
            }
        }

        private static Resource.TaskInfo Pick(Resource.TaskInfo[] candidates, byte playerLevel)
        {
            if (candidates.Length == 0)
                return null;

            var weights = candidates
                .Select(t => Math.Max(1, t.Chance + (playerLevel >= t.AddChanceLimitLevel ? t.AddChance : 0)))
                .ToArray();

            var roll = Random.Next(weights.Sum());
            for (var i = 0; i < candidates.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                    return candidates[i];
            }

            return candidates[candidates.Length - 1];
        }
    }
}
