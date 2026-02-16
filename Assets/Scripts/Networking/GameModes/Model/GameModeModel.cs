using MyToolz.Networking.GameModes.Presenter;

namespace MyToolz.Networking.GameModes.Model
{
    public class GameModeModel
    {
        public int CurrentStateIndex;
        public string WinningTeam;
        public GameStatus gameStatus;

        // Store exactly what the server sets
        public GameStatus GameStatus
        {
            get => gameStatus;
            set => gameStatus = value;
        }

        public GameStatus ClientRelativeStatus
        {
            get
            {
                if (gameStatus == GameStatus.NotEnoughPlayers) return GameStatus.NotEnoughPlayers;
                if (string.IsNullOrEmpty(WinningTeam)) return GameStatus.None;
                return WinningTeam.Equals(Core.NetworkPlayer.LocalPlayerInstance?.TeamGuid)
                    ? GameStatus.Win : GameStatus.Defeat;
            }
        }

        public string Result;

        public GameModeModel() { }

        public GameModeModel(GameModeModel other)
        {
            if (other == null) return;
            WinningTeam = other.WinningTeam;
            gameStatus = other.gameStatus;
            Result = other.Result;
        }
    }
}