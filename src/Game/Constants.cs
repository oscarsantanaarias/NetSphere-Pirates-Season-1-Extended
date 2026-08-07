 // ReSharper disable once CheckNamespace
namespace Netsphere
{
    internal enum PlayerSetting
    {
        AllowCombiInvite,
        AllowFriendRequest,
        AllowRoomInvite,
        AllowInfoRequest
    }

    internal enum GameRuleState
    {
        Waiting,
        Playing,
        EnteringResult,
        Result,

        Neutral,
        FirstHalf,
        EnteringHalfTime,
        HalfTime,
        SecondHalf,
        FullGame,

        Preparing
    }

    enum GameRuleStateTrigger
    {
        StartGame,
        EndGame,
        StartResult,
        StartHalfTime,
        StartSecondHalf,
        StartPrepare
    }
}
