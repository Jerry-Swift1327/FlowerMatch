using UnityEngine;
namespace Match3
{
    public class LevelGrassMoves : Level
    {
        public int numMoves;
        public ObstacleType[] obstacleTypes;
        private const int ScorePerPieceCleared = 100;

        [HideInInspector] public int _movesUsed = 0;
        [HideInInspector] public int _numGrassLeft;

        private void Start()
        {
            Type = LevelType.GrassMoves;

            for (int i = 0; i < obstacleTypes.Length; i++)
             {
                _numGrassLeft += gameGrid.GetPiecesOfObstacleType(obstacleTypes[i]).Count;
             }

            GameCanvasManager.Instance.targetSprite.gameObject.SetActive(true);

            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetRemaining(numMoves);
        }

        public override void OnMove()
        {
            if (gameCanvasManager.isHourglassMode) return;
            base.OnMove();
            _movesUsed++;
            hud.SetRemaining(numMoves - _movesUsed);

            if (numMoves - _movesUsed == 0 && _numGrassLeft > 0)
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

                _numGrassLeft--;
                hud.UpdateSpriteTarget();
                if (_numGrassLeft != 0) continue;

                currentScore += ScorePerPieceCleared * (numMoves - _movesUsed);
                hud.SetScore(currentScore);
                GameWin();
            }
        }
    }
}