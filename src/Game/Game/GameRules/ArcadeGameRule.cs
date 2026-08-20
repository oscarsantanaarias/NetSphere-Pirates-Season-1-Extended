using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netsphere.Network;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Data.GameRule;
using Netsphere.Network.Message.Game;
using Netsphere.Network.Message.GameRule;
using Netsphere.Shop;
using NLog;

namespace Netsphere.Game.GameRules
{
    internal class ArcadeGameRule : GameRuleBase
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int ReviveCost = 30;
        private const int RespawnsPerStage = 10;

        private readonly Dictionary<ulong, Player> _loadingOk = new Dictionary<ulong, Player>();
        private readonly Dictionary<ulong, int> _killedByAccount = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, ArcadeScoreSyncDto> _scoreByAccount = new Dictionary<ulong, ArcadeScoreSyncDto>();
        private readonly HashSet<ulong> _failedPlayers = new HashSet<ulong>();

        public byte Stage { get; set; }

        public byte SubStage { get; set; }

        // the room window sends the difficulty in the same request as the stage
        public byte Difficulty => SubStage >= 1 && SubStage <= 3 ? SubStage : (byte)1;

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
                .OnEntry(SendResult)
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

            // 21045 is the summary of a finished stage, with a record per player inside it. It
            // was going out on the way into the room with three bytes of nothing in it and the
            // client fell over reading it. What he needs here is the board of the mode
            SendStageInfo(e.Player);
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

            // arcade has its own request to start, the normal begin round never arrives, so
            // nobody was starting the match: the map loaded and everyone stood there waiting
            if (StateMachine.CanFire(GameRuleStateTrigger.StartGame))
                StateMachine.Fire(GameRuleStateTrigger.StartGame);

            Room.Broadcast(new SArcadeBeginRoundAckMessage
            {
                Unk1 = (byte)Math.Max(1, Room.TeamManager.PlayersPlaying.Count()),
                Unk2 = Stage
            });

            foreach (var player in Room.TeamManager.Players)
                player.RoomInfo.ArcadeRespawnCount = RespawnsPerStage;
        }

        // the stage he is really on travels in this one too, right after the round begins, and
        // it is the only one that arrives when he does not touch the selection
        public void StageInfo(byte stage, byte subStage)
        {
            if (stage >= 1 && stage <= ArcadeStats.Stages)
                Stage = stage;

            if (subStage >= 1 && subStage <= 3)
                SubStage = subStage;
        }

        public void StageSelect(byte stage, byte subStage)
        {
            StageInfo(stage, subStage);
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

            var difficulty = Difficulty;
            Console.WriteLine($"[arcade] stage clear: stage={Stage} difficulty={difficulty} " +
                              $"players={Room.TeamManager.PlayersPlaying.Count()}");

            foreach (var plr in Room.TeamManager.PlayersPlaying.ToArray())
            {
                plr.stats.Arcade.MarkStageCleared(difficulty, Stage);
                SendStageInfo(plr);
                GiveAllClearReward(plr, difficulty);
            }

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

        // the eight stages of a difficulty are worth a capsule, and the board starts over so it
        // can be earned again. Without the item in the shop nobody gets anything instead of the
        // match falling over
        private void GiveAllClearReward(Player plr, byte difficulty)
        {
            if (!plr.stats.Arcade.IsDifficultyCleared(difficulty))
                return;

            var itemNumber = new ItemNumber((uint)(4030022 + difficulty));
            var shop = GameServer.Instance.ResourceCache.GetShop();

            ShopItem shopItem;
            if (!shop.Items.TryGetValue(itemNumber, out shopItem))
            {
                Logger.Warn($"Arcade reward {itemNumber} is not in the shop");
                return;
            }

            var itemInfo = shopItem.ItemInfos.FirstOrDefault();
            var price = itemInfo?.PriceGroup.Prices.FirstOrDefault();
            if (price == null)
            {
                Logger.Warn($"Arcade reward {itemNumber} has no price to give it away with");
                return;
            }

            plr.Inventory.Create(itemInfo, price, 0, 0, 1);
            plr.Session?.SendAsync(new SArcadeRewardInfoAckMessage
            {
                Reward = new ArcadeRewardDto
                {
                    Unk1 = itemNumber,
                    Unk4 = 1
                }
            });

            plr.stats.Arcade.ResetClears(difficulty);
            SendStageInfo(plr);
        }

        // the board of the lobby: eight stages by three difficulties, with the ones he has
        // already cleared marked
        public static void SendStageInfo(Player plr)
        {
            plr.Session?.SendAsync(new SArcadeMapScoreAckMessage());
            plr.Session?.SendAsync(new SArcadeStageScoreAckMessage
            {
                Scores = (from stage in Enumerable.Range(1, ArcadeStats.Stages)
                          from difficulty in Enumerable.Range(1, 3)
                          select new ArcadeStageScoreDto
                          {
                              Unk1 = 50,
                              Unk2 = (uint)stage,
                              Unk3 = (uint)(difficulty - 1),
                              Unk13 = (byte)(plr.stats.Arcade.IsStageCleared((byte)difficulty, (byte)stage) ? 1 : 0)
                          }).ToArray()
            });
        }

        // the result screen of the arcade does not read the player record, it reads this block,
        // one row per player, and the row of whoever is looking has to come first. That is why
        // every column of it came out at zero
        private void SendResult()
        {
            var players = Room.TeamManager.Players.ToArray();

            foreach (var receiver in players)
            {
                using (var stream = new MemoryStream())
                using (var w = new BinaryWriter(stream))
                {
                    foreach (var plr in players.OrderByDescending(p => p == receiver))
                    {
                        var isReceiver = plr == receiver;
                        var record = plr.RoomInfo.Stats as ArcadePlayerRecord;

                        w.Write(isReceiver ? 1ul : plr.Account.Id);
                        w.Write(record != null ? (int)record.KilledMonster : 0);
                        w.Write(Math.Min(100, Math.Max(0, plr.RoomInfo.ArcadeRespawnCount * 10)));
                        w.Write((int)plr.RoomInfo.PlayTime.TotalSeconds);
                        w.Write(isReceiver ? 1 : 0);
                        w.Write(0);
                        w.Write(0);
                    }

                    receiver.Session?.SendAsync(new SArcadeStageBriefingAckMessage
                    {
                        Unk1 = Stage,
                        Unk2 = SubStage,
                        Data = stream.ToArray()
                    });
                }
            }
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

            // nine integers, which is what the record of the client reads. The result screen
            // takes the first as the HP points, the second as the battle points and the third
            // as the time points, and works out the total and the accumulated score itself
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

        // the mode paid nothing at all. The later seasons run it off the battle royal rates, the
        // same shape every other mode here uses
        public override uint GetExpGain(out uint bonusExp)
        {
            base.GetExpGain(out bonusExp);

            var config = Config.Instance.Game.BRExpRates;
            var place = 1;

            var plrs = Player.Room.TeamManager.Players
                .Where(plr => plr.RoomInfo.State == PlayerState.Waiting &&
                    plr.RoomInfo.Mode == PlayerGameMode.Normal)
                .ToArray();

            foreach (var plr in plrs.OrderByDescending(plr => plr.RoomInfo.Stats.TotalScore))
            {
                if (plr == Player)
                    break;

                place++;
                if (place > 3)
                    break;
            }

            var rankingBonus = 0f;
            switch (place)
            {
                case 1:
                    rankingBonus = config.FirstPlaceBonus;
                    break;

                case 2:
                    rankingBonus = config.SecondPlaceBonus;
                    break;

                case 3:
                    rankingBonus = config.ThirdPlaceBonus;
                    break;
            }

            return (uint)(TotalScore * config.ScoreFactor +
                rankingBonus +
                plrs.Length * config.PlayerCountFactor +
                Player.RoomInfo.PlayTime.TotalMinutes * config.ExpPerMin);
        }
    }
}
