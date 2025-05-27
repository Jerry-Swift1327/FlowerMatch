using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{ 
    public class LevelColorTimer : Level
    {
        public int timeInSeconds;
        public int numSpritesToClear;
        public ColorType targetColor;

        [HideInInspector] public float _timer;

        private void Start()
        {
            Type = LevelType.ColorTimer;
            UIManager.Instance.targetTextGroup.gameObject.SetActive(false);
            UIManager.Instance.targetSprite.gameObject.SetActive(true);

            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetTarget(numSpritesToClear);
            hud.SetRemaining($"{timeInSeconds / 60}:{timeInSeconds % 60:00}");
        }
        private void Update()
        {
            if (uiManager.isHourglassMode) return;
            _timer += Time.deltaTime;
            float remainingTime = Mathf.Max(timeInSeconds - _timer, 0);
            hud.SetRemaining($"{(int)(remainingTime / 60)}:{(int)(remainingTime % 60):00}");

            if (remainingTime == 0)
            {
                if (numSpritesToClear <= 0) GameWin(); 
                else GameLose(); 
            }
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);
            if (numSpritesToClear <= 0) return;

            if (piece.IsColored() && piece.ColorComponent.Color == targetColor)
            {
                numSpritesToClear = Mathf.Max(0, numSpritesToClear - 1);
                hud.UpdateSpriteTarget();

                if (numSpritesToClear == 0)
                {
                    currentScore += 1000 * (int)(timeInSeconds - _timer); 
                    hud.SetScore(currentScore);
                    GameWin();
                }
            }
        }

    }

}
