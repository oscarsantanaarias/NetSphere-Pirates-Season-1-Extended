using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlubLib.IO;
using Netsphere.Network;
using Netsphere.Network.Message.GameRule;

namespace Netsphere.Game.GameRules
{
    internal class ChaserGameRule : GameRuleBase
    {
        private const uint PlayersNeededToStart = 2; // Allow starting with just 2 players

        private static readonly TimeSpan s_nextChaserWaitTime = TimeSpan.FromSeconds(10); //Delay between chaser rounds
        private static readonly TimeSpan s_spanTime = TimeSpan.FromSeconds(1); //Buffer after chaser is chosen
        private readonly Random _random = new Random();

        private TimeSpan _chaserRoundTime; // Time allowed per round
        public TimeSpan _chaserTimer; // Timer for current chaser
        private TimeSpan _nextChaserTimer; // Countdown until next chaser selection

        private bool _waitingNextChaser;
        private bool _roundComplete;
        private Player _bonus;

        private Player LastChaser;

        public override GameRule GameRule => GameRule.Chaser;
        public override Briefing Briefing { get; }

        public Player Chaser { get; private set; }

        public Player Bonus
        {
            get { return _bonus; }
            private set
            {
                if (_bonus == value)
                    return;
                _bonus = value;

                // Notify all players of new bonus target
                if (StateMachine.IsInState(GameRuleState.Playing))
                    Room.Broadcast(new SChangeBonusTargetAckMessage(_bonus?.Account.Id ?? 0));
            }
        }

        public ChaserGameRule(Room room)
            : base(room)
        {
            Briefing = new ChaserBriefing(this);

            // Game state transitions
            StateMachine.Configure(GameRuleState.Waiting)
                .PermitIf(GameRuleStateTrigger.StartGame, GameRuleState.Neutral, CanStartGame);

            StateMachine.Configure(GameRuleState.Neutral)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.EnteringResult)
                .OnEntry(() =>
                {
                    _waitingNextChaser = true;
                    NextChaser();
                    notInitialBriefing = true;
                });

            StateMachine.Configure(GameRuleState.EnteringResult)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.Result)
            .OnEntry(() =>
            {
                Bonus = null;
                Chaser = null;
            });

            StateMachine.Configure(GameRuleState.Result)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.EndGame, GameRuleState.Waiting)
                .OnEntry(() =>
                {
                    Bonus = null;
                    Chaser = null;
                    _waitingNextChaser = false;
                    // Fix for chaser display lingering after match ends
                    Room.Broadcast(new SChangeSlaughtererAckMessage(0));
                });
        }

        public override void Initialize()
        {
            var playersPerTeam = Room.Options.MatchKey.PlayerLimit / 2;
            var spectatorsPerTeam = Room.Options.MatchKey.SpectatorLimit / 2;
            Room.TeamManager.Add(Team.Alpha, (uint)Room.Options.MatchKey.PlayerLimit, (uint)Room.Options.MatchKey.SpectatorLimit);
            Room.TeamManager.Add(Team.Beta, (uint)Room.Options.MatchKey.PlayerLimit, (uint)Room.Options.MatchKey.SpectatorLimit);

            base.Initialize();
        }

        public override void Cleanup()
        {
            Room.TeamManager.Remove(Team.Alpha);
            Room.TeamManager.Remove(Team.Beta);
            base.Cleanup();
        }

        public override void PlayerLeft(object room, RoomPlayerEventArgs e)
        {
            if (StateMachine.IsInState(GameRuleState.Playing))
            {
                base.PlayerLeft(room, e);

                if (e.Player == Chaser)
                {
                    Chaser = null;
                    ChaserLose();
                }
                else if (e.Player == Bonus)
                {
                    Bonus = null;
                    NextTarget();
                }
            }
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            var teamMgr = Room.TeamManager;

            if (StateMachine.IsInState(GameRuleState.Playing) &&
                !StateMachine.IsInState(GameRuleState.EnteringResult) &&
                !StateMachine.IsInState(GameRuleState.Result) &&
                RoundTime >= TimeSpan.FromSeconds(5))
            {
                // Prevent premature result trigger if not enough players
                if (teamMgr.PlayersPlaying.Count() < PlayersNeededToStart)
                    StateMachine.Fire(GameRuleStateTrigger.StartResult);

                if (RoundTime >= Room.Options.TimeLimit)
                    StateMachine.Fire(GameRuleStateTrigger.StartResult);

                // Chaser round time logic
                if (RoundTime >= Room.Options.TimeLimit - _chaserRoundTime)
                {
                    if (!GetPlayersAlive().Any())
                    {
                        ChaserWin();
                    }
                    if (_chaserTimer >= _chaserRoundTime)
                    {
                        ChaserLose();
                    }
                    if (_roundComplete)
                    {
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);
                    }
                }

                if (_waitingNextChaser)
                {
                    _nextChaserTimer += delta;

                    if (_nextChaserTimer >= s_nextChaserWaitTime)
                    {
                        NextChaser();
                    }
                }
                else
                {
                    _chaserTimer += delta;

                    if (_chaserTimer >= _chaserRoundTime)
                    {
                        var diff = Room.Options.TimeLimit - RoundTime;
                        if (diff >= _chaserRoundTime + s_nextChaserWaitTime)
                            ChaserLose();
                    }

                    // Chaser wins if no players are alive
                    if (_chaserTimer > s_spanTime && !GetPlayersAlive().Any())
                        ChaserWin();
                }
            }
        }

        public override PlayerRecord GetPlayerRecord(Player plr)
        {
            return new ChaserPlayerRecord(plr);
        }

        public void OnScoreAttack(Player plr, float unk1, float unk2)
        {
            var stats = GetRecord(plr);
            stats.Kills++;
            stats.SwordRanking += unk1;
            stats.GunRanking += unk2;

            foreach (var plrInRoom in Room.TeamManager.PlayersPlaying)
            {
                if (Chaser == plrInRoom)
                {
                    // Do nothing, if you send score data to chaser it will duplicate the score
                }
                else
                {
                    // Send Score update packets to remaining players
                    plrInRoom.Session.SendAsync(new SSlaughterAttackPointAckMessage
                    {
                        AccountId = plr.Account.Id,
                        Unk1 = unk1, // Send sword ranking
                        Unk2 = unk2 // Send gun ranking

                    });
                }
            }
        }

        public override void OnScoreKill(Player killer, Player assist, Player target, AttackAttribute attackAttribute)
        {
            if (_waitingNextChaser || !StateMachine.IsInState(GameRuleState.Playing))
                return;

            if (target == null)
                return;

            base.OnScoreKill(killer, null, target, attackAttribute);

            var stats = GetRecord(killer);

            if (killer == Chaser && target == Bonus)
            {
                if (stats.Kills > 0)
                    stats.Kills--;
                stats.BonusKills++;
            }

            if (target != Chaser)
                target.RoomInfo.State = PlayerState.Dead;

            if (!GetPlayersAlive().Any())
                ChaserWin();

            if (Chaser == target)
                ChaserLose();

            if (Bonus == target)
                Bonus = null;

            NextTarget();
        }

        public override void OnScoreSuicide(Player plr)
        {
            if (_waitingNextChaser || !StateMachine.IsInState(GameRuleState.Playing))
                return;

            base.OnScoreSuicide(plr);

            if (Chaser == plr)
            {
                ChaserLose();
            }
            else if (Chaser != null)
            {
                var chaserStats = GetRecord(Chaser);
                if (plr == Bonus)
                    chaserStats.BonusKills++;
                else
                    chaserStats.Kills++;
            }

            if (plr != Chaser)
                plr.RoomInfo.State = PlayerState.Dead;

            if (!GetPlayersAlive().Any())
                ChaserWin();

            if (Bonus == plr)
                Bonus = null;

            NextTarget();
        }

        public void NextTarget()
        {
            if (!StateMachine.IsInState(GameRuleState.Playing))
                return;

            if (Bonus != null && Bonus != Chaser && Bonus.RoomInfo.State == PlayerState.Alive)
                return;

            Bonus = GetBonus();
        }

        private Player GetBonus()
        {
            return GetPlayersAlive()
                .OrderBy(plr => plr.RoomInfo.Stats.TotalScore)
                .FirstOrDefault();
        }



        public void RoundEnd()
        {
            _roundComplete = true;
            _waitingNextChaser = true;
            _nextChaserTimer = TimeSpan.Zero;

            //Check remaining room time against chaser round time
            var diff = Room.Options.TimeLimit - RoundTime;
            if (diff <= TimeSpan.FromSeconds(30))
            {
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
                return;
            }

            Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.ChaserIn, (ulong)s_nextChaserWaitTime.TotalMilliseconds, 0, 0, ""));
        }

        public void NextChaser()
        {
            _roundComplete = false;
            //Round duration based on player count, TODO: Needs adjusting to specific times per player #
            _chaserRoundTime = Room.Players.Count < 7
                ? TimeSpan.FromSeconds(60)
                : TimeSpan.FromSeconds(Room.Players.Count * 10);
            _chaserRoundTime += TimeSpan.FromSeconds(Chaser != null ? 3 : 6);

            var chaserCandidates = Room.TeamManager.PlayersPlaying.ToList();
            _chaserTimer = TimeSpan.Zero;

            //Search for valid new chaser
            for (var trys = 0; trys < 10; trys++)
            {
                var index = _random.Next(0, chaserCandidates.Count);
                var candidate = chaserCandidates[index];

                if (candidate != null && candidate != LastChaser)
                {
                    Chaser = candidate;
                    break;
                }
            }

            if (Chaser == null)
            {
                var index = _random.Next(0, chaserCandidates.Count);
                Chaser = chaserCandidates[index];
            }

            // Reset player states
            foreach (var plr in Room.TeamManager.PlayersPlaying)
                plr.RoomInfo.State = PlayerState.Alive;

            GetRecord(Chaser).ChaserCount++;
            LastChaser = Chaser;

            if (GetPlayersAlive() == null)
            {
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
                return;
            }

            Bonus = GetBonus();

            Room.Broadcast(new SChangeSlaughtererAckMessage(
                Chaser.Account.Id,
                Room.TeamManager.PlayersPlaying
                    .Where(plr => plr != Chaser)
                    .Select(plr => plr.Account.Id).ToArray()
            ));

            NextTarget();
            _waitingNextChaser = false;
        }


        public void ChaserWin()
        {
            if (_waitingNextChaser || Chaser == null)
                return;

            GetRecord(Chaser).Wins++;

            // Broadcast the round win message
            Room.Broadcast(new SScoreSLRoundWinAckMessage());
            RoundEnd();
        }

        public void ChaserLose()
        {
            if (_waitingNextChaser)
                return;

            foreach (var plr in GetPlayersAlive())
            {
                GetRecord(plr).Survived++;
            }

            //Chaser loss message
            Room.Broadcast(new SScoreRoundWinAckMessage());
            RoundEnd();
        }

        private bool CanStartGame()
        {
            if (!StateMachine.IsInState(GameRuleState.Waiting))
                return false;

            var countReady = Room.TeamManager.Values.Sum(team => team.Values.Count(plr => plr.RoomInfo.IsReady));

            // Check all players in room
            if (countReady < PlayersNeededToStart - 1) //Excluding room master
                return false;

            return true;
        }

        public IEnumerable<Player> GetPlayersAlive()
        {
            return Room.TeamManager.PlayersPlaying.Where(plr => plr != Chaser && plr.RoomInfo.State == PlayerState.Alive);
        }

        private static ChaserPlayerRecord GetRecord(Player plr)
        {
            return (ChaserPlayerRecord)plr.RoomInfo.Stats;
        }
    }

// Chaser Briefing Section
internal class ChaserBriefing : Briefing
    {
        public long CurrentChaser { get; set; }
        public long CurrentChaserTarget { get; set; }

        public int Unk3 { get; set; }
        public int Unk4 { get; set; }
        public int Unk5 { get; set; }
        public int Unk6 { get; set; }

        public IList<int> Unk7 { get; set; }
        public IList<long> Unk8 { get; set; }
        public IList<long> Unk9 { get; set; } // Players alive

        public int RoundTime { get; set; }

        public ChaserBriefing(GameRuleBase gameRule)
            : base(gameRule)
        {
            Unk7 = new List<int>();
            Unk8 = new List<long>();
            Unk9 = new List<long>();
        }

        protected override void WriteData(BinaryWriter w, bool isResult)
        {
            base.WriteData(w, isResult);

            var gameRule = (ChaserGameRule)GameRule;

            CurrentChaser = (long)(gameRule.Chaser?.Account.Id ?? 0);
            CurrentChaserTarget = (long)(gameRule.Bonus?.Account.Id ?? 0);

            //List of chasers
            Unk8 = new List<long> { CurrentChaser };

            //Alive player list (exclude chaser)
            Unk9 = gameRule.GetPlayersAlive()
                .Where(player => player != gameRule.Chaser)
                .Select(player => (long)player.Account.Id)
                .ToList();

            Unk6 = 1;

            w.Write(CurrentChaser);
            w.Write(CurrentChaserTarget);
            w.Write(Unk3);
            w.Write(Unk4);
            w.Write(Unk5);
            w.Write(Unk6);

            w.Write(Unk7.Count);
            w.Write(Unk7);

            w.Write(Unk8.Count);
            w.Write(Unk8);

            w.Write(Unk9.Count);
            w.Write(Unk9);
        }
    }


    //Chaser Player Record
    internal class ChaserPlayerRecord : PlayerRecord
    {
        public ChaserPlayerRecord(Player plr) : base(plr) { }

        public override uint TotalScore => GetTotalScore();

        // Additional stats
        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int Unk4 { get; set; }
        public uint BonusKills { get; set; }
        public int Unk5 { get; set; }
        public int Unk6 { get; set; }
        public int Unk7 { get; set; }
        public int Unk8 { get; set; }
        public uint Wins { get; set; }
        public uint Survived { get; set; }
        public int Unk9 { get; set; }
        public int Unk10 { get; set; }
        public uint ChaserCount { get; set; }
        public int Unk11 { get; set; }
        public int Unk12 { get; set; }
        public int Unk13 { get; set; }
        public int Unk14 { get; set; }
        public int Unk15 { get; set; }
        public int Unk16 { get; set; }

        // Sword & Gun Ranks
        public float SwordRanking { get; set; }
        public float GunRanking { get; set; }

        public float Unk19 { get; set; }
        public float Unk20 { get; set; }

        public byte Unk21 { get; set; }

        public override void Serialize(BinaryWriter w, bool isResult)
        {
            base.Serialize(w, isResult);

            w.Write(Unk1);
            w.Write(Unk2);
            w.Write(Unk3);
            w.Write(Unk4);
            w.Write(Kills);
            w.Write(BonusKills);
            w.Write(Unk5);
            w.Write(Unk6);
            w.Write(Unk7);
            w.Write(Unk8);
            w.Write(Wins);
            w.Write(Survived);
            w.Write(Unk9);
            w.Write(Unk10);
            w.Write(ChaserCount);
            w.Write(Unk11);
            w.Write(Unk12);
            w.Write(Unk13);
            w.Write(Unk14);
            w.Write(Unk15);
            w.Write(Unk16);
            w.Write(SwordRanking);
            w.Write(GunRanking);
            w.Write(Unk19);
            w.Write(Unk20);
            w.Write(Unk21);
        }

        public override void Reset()
        {
            base.Reset();

            Unk1 = 0;
            Unk2 = 0;
            Unk3 = 0;
            Unk4 = 0;
            Kills = 0;
            BonusKills = 0;
            Unk5 = 0;
            Unk6 = 0;
            Unk7 = 0;
            Unk8 = 0;
            Wins = 0;
            Survived = 0;
            Unk9 = 0;
            Unk10 = 0;
            ChaserCount = 0;
            Unk11 = 0;
            Unk12 = 0;
            Unk13 = 0;
            Unk14 = 0;
            Unk15 = 0;
            Unk16 = 0;
            SwordRanking = 0;
            GunRanking = 0;
            Unk19 = 0;
            Unk20 = 0;
            Unk21 = 0;
        }

        private uint GetTotalScore()
        {
            var totalScore = Kills * 2 +
                             BonusKills * 4 +
                             Wins * 5 +
                             Survived * 10;
							 //+ (uint)(Unk17 + Unk18);
            return totalScore;
        }
    }
}