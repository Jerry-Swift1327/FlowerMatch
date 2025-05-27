using UnityEngine;


namespace Match3
{
  

    public class LevelRainbowMoves : Level
    {
        public int numRainbowToClear; //需要消除的彩虹鱼数量
        public int numMoves; //指定的步数

        private int _rainbowFishCleared = 0;

        [HideInInspector]
        public int _movesUsed = 0;

        private void Start()
        {
            Type = LevelType.RainbowMoves;
            UIManager.Instance.targetTextGroup.gameObject.SetActive(false);
            UIManager.Instance.targetSprite.gameObject.SetActive(true);

            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetTarget(numRainbowToClear);
            hud.SetRemaining(numMoves);
        }
        public override void OnMove()
        {
            if (uiManager.isHourglassMode) return;
            base.OnMove();
            _movesUsed++;
            int remainingMoves = Mathf.Max(0, numMoves - _movesUsed);
            hud.SetRemaining(remainingMoves);

            if(remainingMoves==0&&_rainbowFishCleared!=0) 
                GameLose();            
        }

        public override void OnPieceCleared(GamePiece piece)
        {
            base.OnPieceCleared(piece);

            if(piece.PieceType == PieceType.Rainbow)
            {
                _rainbowFishCleared++;
                numRainbowToClear = Mathf.Max(0, numRainbowToClear - 1);
                hud.UpdateSpriteTarget();

                if (numRainbowToClear == 0)
                {
                    currentScore += 1000 * (numMoves - _movesUsed);
                    hud.SetScore(currentScore);
                    GameWin();
                }
               
            }
        }
    }
}
