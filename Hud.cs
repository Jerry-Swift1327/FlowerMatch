using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3
{
    public class Hud : MonoBehaviour
    {
        public Level level;
        private void Start()
        {
            GameCanvasManager.Instance.SetTargetDisplayMode(level.Type);
            SetLevelType(level.Type);
        }

        // 设置关卡类型（更新剩余/目标描述）
        public void SetLevelType(LevelType Type)
        {
            switch (Type)
            {
                case LevelType.GrassMoves:
                    GameCanvasManager.Instance.remainingSubtext.text = "移动次数";
                    if(level is LevelGrassMoves levelGrass)
                        GameCanvasManager.Instance.SetSingleSpriteTarget(ObstacleType.Grass, levelGrass._numGrassLeft);
                    break;             

                case LevelType.RainbowMoves:
                    GameCanvasManager.Instance.remainingSubtext.text = "移动次数";
                    if (level is LevelRainbowMoves levelRainbowMoves)
                        GameCanvasManager.Instance.SetSingleSpriteTarget(PieceType.Rainbow, levelRainbowMoves.numRainbowToClear);
                    break;

                case LevelType.ColorMoves:
                    GameCanvasManager.Instance.remainingSubtext.text = "移动次数";
                    if (level is LevelColorMoves levelColorMoves)
                        GameCanvasManager.Instance.SetSingleSpriteTarget(levelColorMoves.targetColor, levelColorMoves.numSpritesToClear);
                    break;              

                case LevelType.DoubleColorMoves:
                    GameCanvasManager.Instance.remainingSubtext.text = "移动次数";
                    if(level is LevelDoubleColorMoves levelDoubleColorMoves)
                    {
                        GameCanvasManager.Instance.SetDoubleSpriteTarget(
                            levelDoubleColorMoves.targetColor1, levelDoubleColorMoves.numSpritesToClearColor1,
                            levelDoubleColorMoves.targetColor2, levelDoubleColorMoves.numSpritesToClearColor2);
                    }                  
                    break;

                case LevelType.BubbleMoves:
                    GameCanvasManager.Instance.remainingSubtext.text = "移动次数";
                    if (level is LevelBubbleMoves levelBubble)
                        GameCanvasManager.Instance.SetSingleSpriteTarget(ObstacleType.Bubble, levelBubble._numBubblesLeft);
                    break;

            }
        }

        private void UpdateStars(int score)
        {
            int starCount = 0;
            if (score >= level.score3Star) starCount = 3;
            else if (score >= level.score2Star) starCount = 2;
            else if (score >= level.score1Star) starCount = 1;

            GameCanvasManager.Instance.UpdateScoringStars(starCount);
        }

        public void UpdateSpriteTarget()
        {
            SetLevelType(level.Type);
        }


        public void SetRemaining(int remaining) => GameCanvasManager.Instance.UpdateRemaining(remaining.ToString()); // 步数

        //public void SetRemaining(string remaining) => GameCanvasManager.Instance.UpdateRemaining(remaining);  // 倒计时


        public void OnGameLose()
        {
            GameCanvasManager.Instance.ShowDefeatPanel();

            var defeatPanel = GameCanvasManager.Instance.GetPanel(3);
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                GameCanvasManager.Instance.UpdateMainButtonsState();
            }
        }

        void SaveStars(string levelName)
        {
            string numericPart = levelName.Replace("Level", "");
            if (!int.TryParse(numericPart, out int levelNumber)) return;

            string formattedLevelName = $"Level{levelNumber:D2}";
            int currentStars = PlayerPrefs.GetInt(formattedLevelName, 0);
            if (GameCanvasManager.Instance.currentStarCount > currentStars)
            {
                PlayerPrefs.SetInt(formattedLevelName, GameCanvasManager.Instance.currentStarCount);
                Debug.Log($"保存星星数：{formattedLevelName} = {GameCanvasManager.Instance.currentStarCount}");
            }
        }

        public void OnGameWin(int score)
        {
            var victoryPanel = GameCanvasManager.Instance.GetPanel(2);
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                GameCanvasManager.Instance.UpdateMainButtonsState();
                Debug.Log("胜利面板已激活");
            }

            foreach (var star in GameCanvasManager.Instance.victoryStars)
                star.gameObject.SetActive(false);

            for (int i = 0; i < GameCanvasManager.Instance.currentStarCount; i++)
            {
                if (i < GameCanvasManager.Instance.victoryStars.Length)
                    GameCanvasManager.Instance.victoryStars[i].gameObject.SetActive(true);
            }

            GameCanvasManager.Instance.ShowVictoryPanel(score);

            string currentLevelName = SceneManager.GetActiveScene().name;
            SaveStars(currentLevelName);
        }

        public void SetScore(int score)
        {
            int starCount = CalculateStarCount(score);
            GameCanvasManager.Instance.UpdateScoringStars(starCount);
            GameCanvasManager.Instance.currentStarCount = starCount;
            GameCanvasManager.Instance.SetScoreText(score);
        }

        private int CalculateStarCount(int score)
        {
            int starCount = 0;
            if (score >= level.score3Star) return 3;
            if (score >= level.score2Star) return 2;
            if (score >= level.score1Star) return 1;

            if (level.Didwin && starCount == 0)
                starCount = 1;

            return starCount;
        }

    }
}