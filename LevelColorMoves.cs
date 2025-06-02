using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{

    public class LevelColorMoves : Level
    {
        public int numMoves;
        public int numSpritesToClear;
        public ColorType targetColor;
        private int _spritesCleared = 0;

        [HideInInspector] public int _movesUsed = 0;

        private void Start()
        {
            Type = LevelType.ColorMoves;
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

            if (numMoves - _movesUsed == 0 && numSpritesToClear>0)
            {
                GameLose();
            }
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);
            bool isRainbowWithTargetColor = (piece.PieceType == PieceType.Rainbow && piece.ColorComponent.Color == targetColor);

            if (piece.IsColored() && piece.ColorComponent.Color == targetColor||isRainbowWithTargetColor)
            {
                _spritesCleared++;
                numSpritesToClear = Mathf.Max(0, numSpritesToClear - 1);
                hud.UpdateSpriteTarget();

                if (numSpritesToClear == 0)
                {
                    currentScore += 1000 * (numMoves - _movesUsed);
                    hud.SetScore(currentScore);
                    GameWin();
                }
            }
        }

    }
}
