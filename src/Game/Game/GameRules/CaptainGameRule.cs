using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netsphere.Game.Systems;
using Netsphere.Network.Data.GameRule;
using Netsphere.Network.Message.GameRule;

namespace Netsphere.Game.GameRules
{
    internal class CaptainGameRule : GameRuleBase
    {
        private static readonly TimeSpan s_captainNextroundTime = TimeSpan.FromSeconds(12);
        private static readonly TimeSpan s_captainRoundTime = TimeSpan.FromMinutes(5);
        private readonly CaptainHelper _captainHelper;
        private uint _currentRound;
        private TimeSpan _nextRoundTime = TimeSpan.Zero;
        private TimeSpan _subRoundTime = TimeSpan.Zero;
        private bool _waitingNextRound;

        public override GameRule GameRule => GameRule.Captain;
        public override Briefing Briefing { get; }

        public CaptainGameRule(Room room)
            : base(room)
        {
            Briefing = new CaptainBriefing(this);
            _captainHelper = new CaptainHelper(room);

            StateMachine.Configure(GameRuleState.Waiting)
                .PermitIf(GameRuleStateTrigger.StartPrepare, GameRuleState.Preparing, CanStartGame);

            StateMachine.Configure(GameRuleState.Preparing)
                .Permit(GameRuleStateTrigger.StartGame, GameRuleState.Neutral);

            StateMachine.Configure(GameRuleState.Neutral)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.EnteringResult)
                .OnEntry(StartMatch);

            StateMachine.Configure(GameRuleState.EnteringResult)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.Result);

            StateMachine.Configure(GameRuleState.Result)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.EndGame, GameRuleState.Waiting);
        }

        public override void Initialize()
        {
            var teamMgr = Room.TeamManager;
            teamMgr.Add(Team.Alpha, (uint)(Room.Options.MatchKey.PlayerLimit / 2), (uint)(Room.Options.MatchKey.SpectatorLimit / 2));
            teamMgr.Add(Team.Beta, (uint)(Room.Options.MatchKey.PlayerLimit / 2), (uint)(Room.Options.MatchKey.SpectatorLimit / 2));
            _currentRound = 0;
            base.Initialize();
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
                var min = teamMgr.Values.Min(team =>
                    team.Values.Count(plr =>
                        plr.RoomInfo.State != PlayerState.Lobby &&
                        plr.RoomInfo.State != PlayerState.Spectating));
                if (min == 0)
                    StateMachine.Fire(GameRuleStateTrigger.StartResult);

                if (StateMachine.IsInState(GameRuleState.Neutral))
                {
                    if (teamMgr.Values.Any(team => team.Score >= Room.Options.ScoreLimit))
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);

                    if (_currentRound >= Room.Options.TimeLimit.Minutes)
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);

                    _captainHelper.Update(delta);

                    if (_waitingNextRound)
                    {
                        _nextRoundTime += delta;
                        if (_nextRoundTime >= s_captainNextroundTime)
                        {
                            _captainHelper.Reset();
                            _waitingNextRound = false;
                        }
                    }
                    else
                    {
                        if (_captainHelper.Any())
                        {
                            SubRoundEnd();
                            return;
                        }

                        _subRoundTime += delta;
                        if (_subRoundTime >= s_captainRoundTime)
                            SubRoundEnd();
                    }
                }
            }
        }

        public override void Cleanup()
        {
            var teamMgr = Room.TeamManager;
            teamMgr.Remove(Team.Alpha);
            teamMgr.Remove(Team.Beta);
            base.Cleanup();
        }

        public override void PlayerLeft(object room, RoomPlayerEventArgs e)
        {
            base.PlayerLeft(room, e);
        }

        public override PlayerRecord GetPlayerRecord(Player plr)
        {
            return new CaptainPlayerRecord(plr);
        }

        public override void OnScoreTeamKill(Player killer, Player target, AttackAttribute attackAttribute)
        {
            base.OnScoreTeamKill(killer, target, attackAttribute);
            _captainHelper.Dead(target);
        }

        public override void OnScoreKill(Player killer, Player assist, Player target, AttackAttribute attackAttribute)
        {
            base.OnScoreKill(killer, assist, target, attackAttribute);

            if (_captainHelper.Dead(target))
            {
                var killerRecord = GetRecord(killer);
                killerRecord.KillCaptains++;
                if (killerRecord.Kills > 0)
                    killerRecord.Kills--;

                if (assist != null)
                {
                    var assistRecord = GetRecord(assist);
                    assistRecord.KillAssistCaptains++;
                    if (assistRecord.KillAssists > 0)
                        assistRecord.KillAssists--;
                }
            }
        }

        public override void OnScoreHeal(Player plr)
        {
            base.OnScoreHeal(plr);
            GetRecord(plr).Heal++;
        }

        public override void OnScoreSuicide(Player plr)
        {
            base.OnScoreSuicide(plr);
            _captainHelper.Dead(plr);
            GetRecord(plr).Suicides++;
        }

        private bool CanStartGame()
        {
            if (!StateMachine.IsInState(GameRuleState.Waiting))
                return false;

            var teams = Room.TeamManager.Values.ToArray();
            if (teams.Any(team => team.Count == 0))
                return false;

            return teams.All(team => team.Players.Any(plr => plr.RoomInfo.IsReady || Room.Master == plr));
        }

        private void StartMatch()
        {
            _currentRound = 0;
            _subRoundTime = TimeSpan.Zero;
            _nextRoundTime = TimeSpan.Zero;
            _waitingNextRound = false;
            _captainHelper.Reset();
        }

        private void SubRoundEnd()
        {
            var teamwin = _captainHelper.TeamWin();
            _currentRound++;
            _subRoundTime = TimeSpan.Zero;

            if (teamwin != null && teamwin.Team != Team.Neutral)
            {
                teamwin.Score++;
                foreach (var plr in teamwin.PlayersPlaying)
                    GetRecord(plr).WinRound++;

                Room.Broadcast(new SCaptainSubRoundEndReasonAckMessage
                {
                    Unk1 = 0,
                    Unk2 = (byte)(teamwin.Team == Team.Alpha ? 1 : 2)
                });

                Room.BroadcastBriefing();
            }

            var teamMgr = Room.TeamManager;
            if (_currentRound >= Room.Options.TimeLimit.Minutes
                || teamMgr.Values.Any(team => team.Score >= Room.Options.ScoreLimit))
            {
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
                return;
            }

            Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.NextRoundIn, (ulong)s_captainNextroundTime.TotalMilliseconds, 0, 0, ""));

            _nextRoundTime = TimeSpan.Zero;
            _waitingNextRound = true;
        }

        private static CaptainPlayerRecord GetRecord(Player plr)
        {
            return (CaptainPlayerRecord)plr.RoomInfo.Stats;
        }

        internal class CaptainHelper
        {
            public Room Room { get; }

            private IEnumerable<Player> _alpha;
            private IEnumerable<Player> _beta;
            private float _teamLife;

            public CaptainHelper(Room room)
            {
                Room = room;
                _alpha = from plr in Room.TeamManager.PlayersPlaying
                         where plr.RoomInfo.Team.Team == Team.Alpha
                         select plr;

                _beta = from plr in Room.TeamManager.PlayersPlaying
                        where plr.RoomInfo.Team.Team == Team.Beta
                        select plr;
            }

            public void Reset()
            {
                _alpha = (from plr in Room.TeamManager.PlayersPlaying
                          where plr.RoomInfo.Team.Team == Team.Alpha
                          select plr).ToArray();

                _beta = (from plr in Room.TeamManager.PlayersPlaying
                         where plr.RoomInfo.Team.Team == Team.Beta
                         select plr).ToArray();

                float max = (_alpha.Count() > _beta.Count()) ? _alpha.Count() : _beta.Count();

                _teamLife = max * 500.0f;

                var players = (from plr in Room.TeamManager.PlayersPlaying
                               select new CaptainLifeDto { AccountId = plr.Account.Id, HP = _teamLife / plr.RoomInfo.Team.Count() })
                              .ToArray();

                foreach (var plr in Room.TeamManager.PlayersPlaying)
                    plr.RoomInfo.State = PlayerState.Alive;

                Room.Broadcast(new SCaptainLifeRoundSetUpAckMessage { Players = players });
                Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.ResetRound, 0, 0, 0, ""));
            }

            public bool Dead(Player target)
            {
                if (target == null || target.RoomInfo.Team == null)
                    return false;

                if (target.RoomInfo.Team.Team == Team.Alpha)
                {
                    var isCaptain = _alpha.Any(plr => plr == target);
                    _alpha = _alpha.Where(plr => plr != target).ToArray();
                    Room.Broadcast(new SCurrentRoundInformationAckMessage { Unk1 = _alpha.Count(), Unk2 = _beta.Count() });
                    return isCaptain;
                }

                if (target.RoomInfo.Team.Team == Team.Beta)
                {
                    var isCaptain = _beta.Any(plr => plr == target);
                    _beta = _beta.Where(plr => plr != target).ToArray();
                    Room.Broadcast(new SCurrentRoundInformationAckMessage { Unk1 = _alpha.Count(), Unk2 = _beta.Count() });
                    return isCaptain;
                }

                return false;
            }

            public bool Any()
            {
                return !_alpha.Any() || !_beta.Any();
            }

            public PlayerTeam TeamWin()
            {
                if (!_alpha.Any())
                    return Room.TeamManager[Team.Beta];

                if (!_beta.Any())
                    return Room.TeamManager[Team.Alpha];

                return (_alpha.Count() > _beta.Count())
                    ? Room.TeamManager[Team.Alpha]
                    : Room.TeamManager[Team.Beta];
            }

            public void Update(TimeSpan delta)
            {
                _alpha = (from plr in Room.TeamManager.PlayersPlaying
                          join oplr in _alpha on plr equals oplr
                          select plr).ToArray();

                _beta = (from plr in Room.TeamManager.PlayersPlaying
                         join oplr in _beta on plr equals oplr
                         select plr).ToArray();
            }
        }

        internal class CaptainBriefing : Briefing
        {
            private int Unk1;
            private int Unk2;
            private int Unk3;
            private int Unk4;
            private int Unk5;
            private int Unk6;

            public CaptainBriefing(GameRuleBase RuleBase)
                : base(RuleBase)
            {
                Unk1 = 1;
                Unk2 = 2;
                Unk3 = 3;
                Unk4 = 4;
                Unk5 = 5;
                Unk6 = 6;
            }

            protected override void WriteData(BinaryWriter w, bool isResult)
            {
                base.WriteData(w, isResult);

                w.Write(Unk1);
                w.Write(Unk2);
                w.Write(Unk3);
                w.Write(Unk4);
                w.Write(Unk5);
                w.Write(Unk6);
            }
        }

        internal class CaptainPlayerRecord : PlayerRecord
        {
            public override uint TotalScore
            {
                get
                {
                    var earned = (5 * (WinRound + KillCaptains)) + (2 * Kills) + KillAssists + Heal;
                    return Suicides >= earned ? 0 : earned - Suicides;
                }
            }

            public uint KillCaptains { get; set; }
            public uint KillAssistCaptains { get; set; }
            public uint WinRound { get; set; }
            public uint Heal { get; set; }
            public uint Domination { get; set; }

            public CaptainPlayerRecord(Player plr)
                : base(plr)
            {
            }

            public override void Serialize(BinaryWriter w, bool isResult)
            {
                base.Serialize(w, isResult);

                w.Write(KillCaptains);
                w.Write(KillAssistCaptains);
                w.Write(Kills);
                w.Write(KillAssists);
                w.Write(Heal);
                w.Write(WinRound);
                w.Write(Domination);
            }

            public override void Reset()
            {
                base.Reset();
                KillCaptains = 0;
                KillAssistCaptains = 0;
                WinRound = 0;
                Heal = 0;
                Domination = 0;
            }
        }
    }
}
