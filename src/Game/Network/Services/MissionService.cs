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

        public static async Task SendMissionInfo(GameSession session)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            TaskDto[] tasks = System.Array.Empty<TaskDto>();

            try
            {
                using (var db = GameDatabase.Open())
                {
                    var rows = (await db.FindAsync<PlayerMissionDto>(statement => statement
                            .Where($"{nameof(PlayerMissionDto.PlayerId):C} = @PlayerId")
                            .WithParameters(new { PlayerId = (int)plr.Account.Id }))
                        .ConfigureAwait(false)).ToList();

                    tasks = rows.Select(r => new TaskDto
                    {
                        Id = (uint)r.MissionId,
                        Unk = 0,
                        Progress = (ushort)r.Progress,
                        RewardType = MissionRewardType.PEN,
                        Reward = 0
                    }).ToArray();
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to load missions: {ex.Message}")
                    .Write();
            }

            await session.SendAsync(new STaskInfoAckMessage { Tasks = tasks })
                .ConfigureAwait(false);
        }

        [MessageHandler(typeof(CTaskNotifyReqMessage))]
        public async Task TaskNotifyReq(GameSession session, CTaskNotifyReqMessage message)
        {
            var plr = session.Player;
            if (plr == null)
                return;

            if (message.TaskId == 0 || message.TaskId > 10000)
                return;

            try
            {
                using (var db = GameDatabase.Open())
                {
                    var row = (await db.FindAsync<PlayerMissionDto>(statement => statement
                            .Where($"{nameof(PlayerMissionDto.PlayerId):C} = @PlayerId AND {nameof(PlayerMissionDto.MissionId):C} = @MissionId")
                            .WithParameters(new { PlayerId = (int)plr.Account.Id, MissionId = (int)message.TaskId }))
                        .ConfigureAwait(false)).FirstOrDefault();

                    if (row == null)
                    {
                        row = new PlayerMissionDto
                        {
                            PlayerId = (int)plr.Account.Id,
                            MissionId = (int)message.TaskId,
                            Progress = message.Progress,
                            Completed = false
                        };
                        await db.InsertAsync(row).ConfigureAwait(false);
                    }
                    else
                    {
                        row.Progress = message.Progress;
                        await db.UpdateAsync(row).ConfigureAwait(false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to store mission progress: {ex.Message}")
                    .Write();
            }

            await session.SendAsync(new STaskUpdateAckMessage { TaskId = message.TaskId, Progress = message.Progress })
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

            if (message.TaskId == 0 || message.TaskId > 10000)
            {
                await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                    .ConfigureAwait(false);
                return;
            }

            try
            {
                using (var db = GameDatabase.Open())
                {
                    var row = (await db.FindAsync<PlayerMissionDto>(statement => statement
                            .Where($"{nameof(PlayerMissionDto.PlayerId):C} = @PlayerId AND {nameof(PlayerMissionDto.MissionId):C} = @MissionId")
                            .WithParameters(new { PlayerId = (int)plr.Account.Id, MissionId = (int)message.TaskId }))
                        .ConfigureAwait(false)).FirstOrDefault();

                    if (row == null || row.Completed)
                    {
                        await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                            .ConfigureAwait(false);
                        return;
                    }

                    row.Completed = true;
                    await db.UpdateAsync(row).ConfigureAwait(false);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warn()
                    .Account(session)
                    .Message($"Failed to request mission reward: {ex.Message}")
                    .Write();

                await session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask))
                    .ConfigureAwait(false);
                return;
            }

            await session.SendAsync(new STaskRequestAckMessage
            {
                TaskId = message.TaskId,
                RewardType = MissionRewardType.PEN,
                Reward = 0,
                Slot = message.Unk2
            }).ConfigureAwait(false);
        }
    }
}
