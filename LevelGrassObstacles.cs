using UnityEngine;
namespace Match3
{
    public class LevelGrassObstacles : Level
    {
        public int numMoves;
        public ObstacleType[] obstacleTypes;
        private const int ScorePerPieceCleared = 100;

        [HideInInspector] public int _movesUsed = 0;
        [HideInInspector] public int _numObstaclesLeft;

        private void Start()
        {
            Type = LevelType.Obstacle;

            for (int i = 0; i < obstacleTypes.Length; i++)
             {
                 _numObstaclesLeft += gameGrid.GetPiecesOfObstacleType(obstacleTypes[i]).Count;
             }

            UIManager.Instance.targetTextGroup.gameObject.SetActive(false);
            UIManager.Instance.targetSprite.gameObject.SetActive(true);

            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetTarget(_numObstaclesLeft);
            hud.SetRemaining(numMoves);
        }

        public override void OnMove()
        {
            if (uiManager.isHourglassMode) return;
            base.OnMove();
            _movesUsed++;
            hud.SetRemaining(numMoves - _movesUsed);

            if (numMoves - _movesUsed == 0 && _numObstaclesLeft > 0)
            {
                GameLose();
            }
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);

            for (int i = 0; i < obstacleTypes.Length; i++)
            {
                if (obstacleTypes[i] != piece.ObstacleType) continue;               

                _numObstaclesLeft--;
               hud.UpdateSpriteTarget();
                if (_numObstaclesLeft != 0) continue;

                currentScore += ScorePerPieceCleared * (numMoves - _movesUsed);
                hud.SetScore(currentScore);
                GameWin();
            }
        }
    }
}