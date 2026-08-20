using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netsphere.Network.Data.GameRule;
using Netsphere.Network.Message.Game;
using Netsphere.Network.Message.GameRule;

namespace Netsphere.Game.GameRules
{
    internal class ArcadeGameRule : GameRuleBase
    {
        private const int ReviveCost = 30;
        private const int RespawnsPerStage = 10;

        private readonly Dictionary<ulong, Player> _loadingOk = new Dictionary<ulong, Player>();
        private readonly Dictionary<ulong, int> _killedByAccount = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, ArcadeScoreSyncDto> _scoreByAccount = new Dictionary<ulong, ArcadeScoreSyncDto>();
        private readonly HashSet<ulong> _failedPlayers = new HashSet<ulong>();

        public byte Stage { get; set; }

        public byte SubStage { get; set; }

        public override Briefing Briefing { get; }

        public override GameRule GameRule => GameRule.Arcade;

        public ArcadeGameRule(Room room)
            : base(room)
        {
            Briefing = new Briefing(this);

            StateMachine.Configure(GameRuleState.Waiting)
                .PermitIf(GameRuleStateTrigger.StartGame, GameRuleState.Neutral, CanStart);

            StateMachine.Configure(GameRuleState.Neutral)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.EnteringResult);

            StateMachine.Configure(GameRuleState.EnteringResult)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.Result);

            StateMachine.Configure(GameRuleState.Result)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.EndGame, GameRuleState.Waiting);
        }

        public override void Initialize()
        {
            Room.TeamManager.Add(
                Team.Alpha,
                (uint)Room.Options.MatchKey.PlayerLimit,
                (uint)Room.Options.MatchKey.SpectatorLimit);

            ResetStage();
            _loadingOk.Clear();
            base.Initialize();
        }

        public override void Cleanup()
        {
            ResetStage();
            _loadingOk.Clear();
            Room.TeamManager.Remove(Team.Alpha);
            base.Cleanup();
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);
            var teamMgr = Room.TeamManager;

            if (StateMachine.IsInState(GameRuleState.Playing) &&
                !StateMachine.IsInState(GameRuleState.EnteringResult) &&
                !StateMachine.IsInState(GameRuleState.Result) &&
                RoundTime >= TimeSpan.FromSeconds(5)) // Let the round run for at least 5 seconds - Fixes StartResult trigger on game start(race condition)
            {
                if (StateMachine.IsInState(GameRuleState.Neutral))
                {
                    // this read "if there is anybody playing, go to the result screen", so every
                    // arcade ended five seconds after it started
                    if (!teamMgr.PlayersPlaying.Any())
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);

                    // Did we reach round limit?
                    if (RoundTime >= Room.Options.TimeLimit)
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);
                }
            }
        }

        public override PlayerRecord GetPlayerRecord(Player plr)
        {
            return new ArcadePlayerRecord(plr);
        }

        public override void PlayerJoined(object room, RoomPlayerEventArgs e)
        {
            base.PlayerJoined(room, e);
            e.Player.Session.SendAsync(new SArcadeStageBriefingAckMessage
            {
                Unk1 = Stage,
                Unk2 = SubStage,
                Data = new byte[] { 0, 0, 0 }
            });
        }

        public override void PlayerLeft(object room, RoomPlayerEventArgs e)
        {
            base.PlayerLeft(room, e);

            _loadingOk.Remove(e.Player.Account.Id);
            _killedByAccount.Remove(e.Player.Account.Id);
            _scoreByAccount.Remove(e.Player.Account.Id);
            _failedPlayers.Remove(e.Player.Account.Id);
        }

        public void OnLoadingOk(Player plr)
        {
            // this was an Add, so the second match in the same room threw on the duplicate key
            _loadingOk[plr.Account.Id] = plr;
            Room.Broadcast(new SArcadeLoadingSucceedAckMessage { AccountId = plr.Account.Id });

            if (_loadingOk.Count >= Room.Players.Count)
                Room.Broadcast(new SArcadeAllLoadingSucceedAckMessage());
        }

        // the host asks to start the stage. The whole room has to hear it, not only whoever
        // asked, and everybody starts the stage with ten revives in the pocket
        public void StageBegin(Player plr)
        {
            ResetStage();

            Room.Broadcast(new SArcadeBeginRoundAckMessage
            {
                Unk1 = (byte)Math.Max(1, Room.TeamManager.PlayersPlaying.Count()),
                Unk2 = Stage
            });

            foreach (var player in Room.TeamManager.Players)
                player.RoomInfo.ArcadeRespawnCount = RespawnsPerStage;
        }

        public void StageSelect(byte stage, byte subStage)
        {
            Stage = stage;
            SubStage = subStage;
            ResetStage();

            Room.Broadcast(new SArcadeStageSelectAckMessage { Unk1 = stage, Unk2 = subStage });
        }

        // what the host sends while the stage runs: how many monsters each one has put down. The
        // highest seen is kept so a late packet does not take kills away, the share of the work
        // is worked out from the total and the table goes back to the room
        public void ScoreSync(ArcadeScoreSyncReqDto[] scores)
        {
            if (scores == null)
                return;

            foreach (var entry in scores)
            {
                var previous = _killedByAccount.ContainsKey(entry.AccountId) ? _killedByAccount[entry.AccountId] : 0;
                _killedByAccount[entry.AccountId] = Math.Max(previous, Math.Max(0, entry.Unk3));
            }

            var total = _killedByAccount.Values.Sum(killed => (long)killed);

            foreach (var entry in scores)
            {
                var target = Room.TeamManager.Players.FirstOrDefault(p => p.Account.Id == entry.AccountId);
                if (target == null)
                    continue;

                var mine = _killedByAccount[entry.AccountId];

                _scoreByAccount[entry.AccountId] = new ArcadeScoreSyncDto
                {
                    AccountId = entry.AccountId,
                    Unk1 = entry.Unk1,
                    Unk2 = entry.Unk2,
                    Unk3 = mine,
                    Unk4 = total > 0 ? (int)Math.Min(100, (100 * mine) / total) : 0
                };

                var record = target.RoomInfo.Stats as ArcadePlayerRecord;
                if (record != null)
                    record.KilledMonster = (uint)mine;
            }

            Room.Broadcast(new SArcadeScoreSyncAckMessage { Scores = _scoreByAccount.Values.ToArray() });
        }

        public void StageClear(ArcadeScoreSyncReqDto[] scores)
        {
            ScoreSync(scores);

            foreach (var score in _scoreByAccount.Values)
                score.Unk4 = 100;

            Room.Broadcast(new SArcadeScoreSyncAckMessage { Scores = _scoreByAccount.Values.ToArray() });

            if (StateMachine.CanFire(GameRuleStateTrigger.StartResult))
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
        }

        // one man down does not end the stage, all of them being down does
        public void StageFailed(Player plr)
        {
            if (plr != null)
                _failedPlayers.Add(plr.Account.Id);

            var playing = Room.TeamManager.PlayersPlaying.Count();
            if (_failedPlayers.Count < Math.Max(1, playing))
                return;

            if (StateMachine.CanFire(GameRuleStateTrigger.StartResult))
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
        }

        // thirty pen and one of his ten revives. Without either of the two he is out of the stage
        public void Respawn(Player plr)
        {
            if (plr.RoomInfo.ArcadeRespawnCount <= 0 || plr.PEN < ReviveCost)
            {
                plr.Session?.SendAsync(new SArcadeRespawnFailAckMessage());
                StageFailed(plr);
                return;
            }

            plr.PEN -= ReviveCost;
            plr.RoomInfo.ArcadeRespawnCount--;
            plr.RoomInfo.State = PlayerState.Alive;

            plr.Session?.SendAsync(new SArcadeRespawnAckMessage { Unk = plr.RoomInfo.ArcadeRespawnCount });
            plr.Session?.SendAsync(new SRefreshCashInfoAckMessage(plr.PEN, plr.AP));
        }

        private void ResetStage()
        {
            _killedByAccount.Clear();
            _scoreByAccount.Clear();
            _failedPlayers.Clear();
        }

        private bool CanStart()
        {
            return !Room.TeamManager.Players.Any(p => p.RoomInfo.IsReady == false && p != Room.Master);
        }
    }

    internal class ArcadePlayerRecord : PlayerRecord
    {
        public override uint TotalScore => (5 * QueenKills) + BonusKillAssists + KilledMonster;

        public uint QueenKills { get; set; }
        public uint BonusKillAssists { get; set; }
        public uint KilledMonster { get; set; }

        public ArcadePlayerRecord(Player plr)
            : base(plr)
        { }

        public override void Serialize(BinaryWriter w, bool isResult)
        {
            base.Serialize(w, isResult);

            w.Write(Math.Min(100, Math.Max(0, Player.RoomInfo.ArcadeRespawnCount * 10)));
            w.Write((int)KilledMonster);
            w.Write((int)Player.RoomInfo.PlayTime.TotalSeconds);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
        }

        public override void Reset()
        {
            base.Reset();
            QueenKills = 0;
            BonusKillAssists = 0;
            KilledMonster = 0;
        }
    }
}
