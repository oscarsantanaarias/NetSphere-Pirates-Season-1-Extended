using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                .PermitIf(GameRuleStateTrigger.StartGame, GameRuleState.Neutral, CanStartGame);

            StateMachine.Configure(GameRuleState.Neutral)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.EnteringResult)
                .OnEntry(_captainHelper.Reset);

            StateMachine.Configure(GameRuleState.EnteringResult)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.Result);

            StateMachine.Configure(GameRuleState.Result)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.EndGame, GameRuleState.Waiting)
                .OnEntry(UpdatePlayerStats);
        }

        public override void Initialize()
        {

            var teamMgr = Room.TeamManager;
            teamMgr.Add(Team.Alpha, (uint)(Room.Options.MatchKey.PlayerLimit / 2), (uint)(Room.Options.MatchKey.SpectatorLimit / 2));
            teamMgr.Add(Team.Beta, (uint)(Room.Options.MatchKey.PlayerLimit / 2), (uint)(Room.Options.MatchKey.SpectatorLimit / 2));
            _currentRound = 0;
            _nextRoundTime = TimeSpan.Zero;
            _subRoundTime = TimeSpan.Zero;
            _waitingNextRound = false;
            base.Initialize();
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
                // Still have enough players?
                var min = teamMgr.Values.Min(team =>
                team.Values.Count(plr =>
                    plr.RoomInfo.State != PlayerState.Lobby &&
                    plr.RoomInfo.State != PlayerState.Spectating));
                if (min == 0)
                    StateMachine.Fire(GameRuleStateTrigger.StartResult);

                if (StateMachine.IsInState(GameRuleState.Neutral))
                {
                    // Did we reach ScoreLimit?
                    if (teamMgr.Values.Any(team => team.Score >= Room.Options.ScoreLimit))
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);

                    // Did we reach round limit?
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
                        if (_captainHelper.RoundOver())
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

            // the round is over when the last one of a side walks out, same as when he dies
            if (StateMachine.IsInState(GameRuleState.Playing) && !_waitingNextRound)
            {
                _captainHelper.Dead(e.Player);
                if (_captainHelper.RoundOver())
                    SubRoundEnd();
            }
        }

        public override PlayerRecord GetPlayerRecord(Player plr)
        {
            return new CaptainPlayerRecord(plr);
        }

        public override void OnScoreTeamKill(Player killer, Player target, AttackAttribute attackAttribute)
        {
            _captainHelper.Dead(target);
            GetRecord(target).Deaths++;
            base.OnScoreTeamKill(killer, target, attackAttribute);

            // a kill that lands during the twelve seconds between rounds does not end
            // the next one before it started
            if (!_waitingNextRound && _captainHelper.RoundOver())
                SubRoundEnd();
        }

        public override void OnScoreKill(Player killer, Player assist, Player target, AttackAttribute attackAttribute)
        {
            if (_captainHelper.Dead(target))
            {
                GetRecord(killer).KillCaptains++;
                if (assist != null)
                    GetRecord(assist).KillAssistCaptains++;
            }
            else
            {
                GetRecord(killer).Kills++;
                if (assist != null)
                    GetRecord(assist).KillAssists++;
            }

            GetRecord(target).Deaths++;

            base.OnScoreKill(killer, null, target, attackAttribute);

            if (!_waitingNextRound && _captainHelper.RoundOver())
                SubRoundEnd();
        }

        public override void OnScoreSuicide(Player plr)
        {
            _captainHelper.Dead(plr);
            GetPlayerRecord(plr).Suicides++;
            base.OnScoreSuicide(plr);

            if (!_waitingNextRound && _captainHelper.RoundOver())
                SubRoundEnd();
        }

        private bool CanStartGame()
        {
            if (!StateMachine.IsInState(GameRuleState.Waiting))
                return false;

            var teams = Room.TeamManager.Values.ToArray();
            if (teams.Any(team => team.Count == 0)) // Do we have enough players?
                return false;

            // Is atleast one player per team ready?
            return teams.All(team => team.Players.Any(plr => plr.RoomInfo.IsReady || Room.Master == plr));
        }

        private void SubRoundEnd()
        {
            var teamwin = _captainHelper.TeamWin();
            _currentRound++;

            // Increase teamwin score
            if (teamwin != null)
            {
                teamwin.Score++;

                // give all players winRound score
                foreach (var plr in teamwin.PlayersPlaying)
                    GetRecord(plr).WinRound++;
            }

            var teamMgr = Room.TeamManager;

            _nextRoundTime = TimeSpan.Zero;
            _subRoundTime = TimeSpan.Zero;
            _waitingNextRound = true;

            // Did we reach ScoreLimit or Round Limit?
            if (_currentRound >= Room.Options.TimeLimit.Minutes
                || teamMgr.Values.Any(team => team.Score >= Room.Options.ScoreLimit))
            {
                StateMachine.Fire(GameRuleStateTrigger.StartResult);
                return;
            }

            if (teamwin != null)
            {
                Room.Broadcast(
                    new SCaptainSubRoundEndReasonAckMessage
                    {
                        Unk1 = 0,
                        Unk2 = (byte)(teamwin.Team == Team.Alpha ? 1 : 2)
                    });
            }

            Room.Broadcast(
                new SEventMessageAckMessage(GameEventMessage.NextRoundIn, (ulong)s_captainNextroundTime.TotalMilliseconds, 0, 0, ""));
        }

        private static CaptainPlayerRecord GetRecord(Player plr)
        {
            return (CaptainPlayerRecord)plr.RoomInfo.Stats;
        }

        private void UpdatePlayerStats()
        {
            // todo

            /*
			var WinTeam = Room
                .TeamManager
                .PlayersPlaying
                .Aggregate(
                    (highestTeam, player) =>
                    (highestTeam == null || player.RoomInfo.Team.Score > highestTeam.RoomInfo.Team.Score) ?
                    player : highestTeam).RoomInfo.Team;
					*/

            /*foreach (var plr in Room.TeamManager.PlayersPlaying)
    {
        if (plr.RoomInfo.Team == WinTeam)
            plr.CaptainMode.Won++;
        else
            plr.CaptainMode.Loss++;
    }
}*/
        }

        internal class CaptainHelper
        {
            public Room Room { get; }

            // these used to be linq queries built on top of each other, one more layer every
            // frame and one more every death, so a long round spent its time walking a chain
            // of thousands of enumerables to answer how many are left
            private readonly List<Player> _alpha = new List<Player>();
            private readonly List<Player> _beta = new List<Player>();

            public CaptainHelper(Room room)
            {
                Room = room;
            }

            public void Reset()
            {
                _alpha.Clear();
                _beta.Clear();

                foreach (var plr in Room.TeamManager.PlayersPlaying)
                {
                    if (plr.RoomInfo.Team == null)
                        continue;

                    if (plr.RoomInfo.Team.Team == Team.Alpha)
                        _alpha.Add(plr);
                    else if (plr.RoomInfo.Team.Team == Team.Beta)
                        _beta.Add(plr);
                }

                var life = (_alpha.Count > _beta.Count ? _alpha.Count : _beta.Count) * 500.0f;

                var alphaLife = life / Math.Max(1, _alpha.Count);
                var betaLife = life / Math.Max(1, _beta.Count);

                var players = _alpha.Select(plr => new CaptainLifeDto { AccountId = plr.Account.Id, HP = alphaLife })
                    .Concat(_beta.Select(plr => new CaptainLifeDto { AccountId = plr.Account.Id, HP = betaLife }))
                    .ToArray();

                foreach (var plr in _alpha.Concat(_beta))
                    plr.RoomInfo.State = PlayerState.Alive;

                Room.Broadcast(new SCaptainLifeRoundSetUpAckMessage { Players = players });
                Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.ResetRound, 0, 0, 0, ""));
            }

            // both sides, and never through the player: somebody who left the room has no team
            // and no room any more, and reading them was a null reference on the way out
            public bool Dead(Player target)
            {
                var wasAlive = _alpha.Remove(target) | _beta.Remove(target);
                if (!wasAlive)
                    return false;

                Room.Broadcast(new SCurrentRoundInformationAckMessage { Unk1 = _alpha.Count, Unk2 = _beta.Count });
                return true;
            }

            public bool RoundOver()
            {
                return _alpha.Count == 0 || _beta.Count == 0;
            }

            public PlayerTeam TeamWin()
            {
                if (_alpha.Count == 0)
                    return Room.TeamManager.GetValueOrDefault(Team.Beta);

                if (_beta.Count == 0)
                    return Room.TeamManager.GetValueOrDefault(Team.Alpha);

                return _alpha.Count > _beta.Count
                    ? Room.TeamManager.GetValueOrDefault(Team.Alpha)
                    : Room.TeamManager.GetValueOrDefault(Team.Beta);
            }

            public void Update(TimeSpan delta)
            {
                var playing = Room.TeamManager.PlayersPlaying.ToArray();
                _alpha.RemoveAll(plr => !playing.Contains(plr));
                _beta.RemoveAll(plr => !playing.Contains(plr));
            }
        }

        internal class CaptainBriefing : Briefing
        {
            //int Unk1;
            int Unk2;
            int Unk3;
            int Unk4;
            int Unk5;
            int Unk6;
            public CaptainBriefing(GameRuleBase RuleBase)
                : base(RuleBase)
            {
                Unk2 = 2;
                Unk3 = 3;
                Unk4 = 4;
                Unk5 = 5;
                Unk6 = 6;
            }

            protected override void WriteData(BinaryWriter w, bool isResult)
            {
                base.WriteData(w, isResult);

                var gameRule = (CaptainGameRule)GameRule;

                w.Write((int)gameRule._currentRound);       // Current round number
                w.Write(Unk2);
                w.Write(Unk3);
                w.Write(Unk4);
                w.Write(Unk5);
                w.Write(Unk6);
            }
        }

        internal class CaptainPlayerRecord : PlayerRecord
        {
            public override uint TotalScore => (5 * (WinRound + KillCaptains)) + (2 * Kills) + KillAssists + Heal - Suicides;
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
                Heal = 0;
            }

            /*public override uint GetExpGain(out uint bonusExp)
            {
                return GetExpGain(Config.Instance.Game.CaptainExpRates, out bonusExp);
            }

            public override uint GetPenGain(out uint bonusPen)
            {
                return GetPenGain(Config.Instance.Game.CaptainExpRates, out bonusPen);
            }*/
        }
    }
}
