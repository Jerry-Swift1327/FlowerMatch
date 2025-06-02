using UnityEngine;
using System.Collections.Generic;

namespace Match3
{
    public class LevelBubbleMoves : Level
    {
        public int numMoves;
        public ObstacleType[] obstacleTypes;
        private const int ScorePerPieceCleared = 100;

        [HideInInspector] public int _movesUsed = 0;
        [HideInInspector] public int _numBubblesLeft;

        private void Start()
        {
            Type = LevelType.BubbleMoves;
            _numBubblesLeft = 0;

            ObstacleType[] bubbleTypes = { ObstacleType.Bubble_1, ObstacleType.Bubble_2, ObstacleType.Bubble_3 };

            for(int i=0;i<obstacleTypes.Length;i++)
            {
                _numBubblesLeft += gameGrid.GetPiecesOfObstacleType(obstacleTypes[i]).Count;
            }

            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetRemaining(numMoves);

            Debug.Log($"初始泡泡数量: {_numBubblesLeft}");
        }

        public override void OnMove()
        {
            if (GameCanvasManager.Instance.isHourglassMode) return;
            base.OnMove();
            _movesUsed++;
            hud.SetRemaining(numMoves - _movesUsed);

            if (numMoves - _movesUsed == 0 && _numBubblesLeft > 0) GameLose();
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);

            if(piece.PieceType == PieceType.Obstacle && piece.ObstacleType == ObstacleType.Bubble_3)
            {
                _numBubblesLeft--;
                Debug.Log($"剩余泡泡数量: {_numBubblesLeft}");
                hud.UpdateSpriteTarget();
                if(_numBubblesLeft == 0)
                {
                    currentScore += ScorePerPieceCleared * (numMoves - _movesUsed);
                    hud.SetScore(currentScore);
                    GameWin();
                }
            }
        }
    }
}