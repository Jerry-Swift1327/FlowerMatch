using UnityEngine;
namespace Match3
{
    public class LevelMoves : Level
    {

        public int numMoves;
        public int targetScore;

        [HideInInspector]
        public int _movesUsed = 0;

        private void Start()
        {
            Type = LevelType.Moves;           
            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetTarget(targetScore);
            hud.SetRemaining(numMoves);
        }

        public override void OnMove()
        {
            if (uiManager.isHourglassMode) return;
            base.OnMove();
            _movesUsed++;
            hud.SetRemaining(numMoves - _movesUsed);
            if (numMoves - _movesUsed == 0)
            {
                if (currentScore < targetScore)
                    GameLose();
            }
        
            
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);

            if(currentScore>=targetScore)
            {
                if (numMoves - _movesUsed >= 0)
                    GameWin();
            }
                
        }
    }
}
