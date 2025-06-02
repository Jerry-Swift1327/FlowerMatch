using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class LevelDoubleColorMoves : Level
    {
        public int numMoves;
        public int numSpritesToClearColor1;
        public int numSpritesToClearColor2;
        public ColorType targetColor1;
        public ColorType targetColor2;

        [HideInInspector] public int _movesUsed = 0;

        private void Start()
        {
            Type = LevelType.DoubleColorMoves;
            GameCanvasManager.Instance.targetSprite.gameObject.SetActive(true);
            GameCanvasManager.Instance.targetSprite_1.gameObject.SetActive(true);

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

            if (numMoves - _movesUsed == 0)
            {
                if (numSpritesToClearColor1 == 0 && numSpritesToClearColor2 == 0) GameWin();
                else GameLose();
            }
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);

            if (piece.IsColored())
            {
                if (piece.ColorComponent.Color == targetColor1 && numSpritesToClearColor1 > 0)
                {
                    numSpritesToClearColor1 = Mathf.Max(0, numSpritesToClearColor1 - 1);
                    hud.UpdateSpriteTarget();

                    if (numSpritesToClearColor1 == 0 && numSpritesToClearColor2 == 0)
                    {
                        currentScore += 1000 * (numMoves - _movesUsed);
                        hud.SetScore(currentScore);
                        GameWin();
                    }
                }
                // 处理颜色2的消除
                else if (piece.ColorComponent.Color == targetColor2 && numSpritesToClearColor2 > 0)
                {
                    numSpritesToClearColor2 = Mathf.Max(0, numSpritesToClearColor2 - 1);
                    hud.UpdateSpriteTarget();

                    if (numSpritesToClearColor1 == 0 && numSpritesToClearColor2 == 0)
                    {
                        currentScore += 1000 * (numMoves - _movesUsed);
                        hud.SetScore(currentScore);
                        GameWin();
                    }
                }
            }
        }
    }
}

