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
            UIManager.Instance.SetTargetDisplayMode(level.Type);
            SetLevelType(level.Type);
        }

        // 设置关卡类型（更新剩余/目标描述）
        public void SetLevelType(LevelType Type)
        {
            switch (Type)
            {
                case LevelType.Moves:
                    UIManager.Instance.remainingSubtext.text = "移动次数";
                    UIManager.Instance.targetSubtext.text = "目标分数";
                    break;

                case LevelType.Obstacle:
                    UIManager.Instance.remainingSubtext.text = "移动次数";
                    if(level is LevelGrassObstacles levelObstacles)
                        UIManager.Instance.SetSingleSpriteTarget(ObstacleType.Grass, levelObstacles._numObstaclesLeft);
                    break;

                case LevelType.Timer:
                    UIManager.Instance.remainingSubtext.text = "剩余时间";
                    UIManager.Instance.targetSubtext.text = "目标分数";
                    break;

                case LevelType.RainbowMoves:
                    UIManager.Instance.remainingSubtext.text = "移动次数";
                    if (level is LevelRainbowMoves levelRainbowMoves)
                        UIManager.Instance.SetSingleSpriteTarget(PieceType.Rainbow, levelRainbowMoves.numRainbowToClear);
                    break;

                case LevelType.ColorMoves:
                    UIManager.Instance.remainingSubtext.text = "移动次数";
                    if (level is LevelColorMoves levelColorMoves)
                        UIManager.Instance.SetSingleSpriteTarget(levelColorMoves.targetColor, levelColorMoves.numSpritesToClear);
                    break;

                case LevelType.ColorTimer:
                    UIManager.Instance.remainingSubtext.text = "剩余时间";
                    if (level is LevelColorTimer levelColorTimer)
                        UIManager.Instance.SetSingleSpriteTarget(levelColorTimer.targetColor, levelColorTimer.numSpritesToClear);
                    break;

                case LevelType.DoubleColorMoves:
                    UIManager.Instance.remainingSubtext.text = "移动次数";
                    if(level is LevelDoubleColorMoves levelDoubleColorMoves)
                    {
                        UIManager.Instance.SetDoubleSpriteTarget(
                            levelDoubleColorMoves.targetColor1, levelDoubleColorMoves.numSpritesToClearColor1,
                            levelDoubleColorMoves.targetColor2, levelDoubleColorMoves.numSpritesToClearColor2);
                    }                  
                    break;
            }
        }

        private void UpdateStars(int score)
        {
            int starCount = 0;
            if (score >= level.score3Star) starCount = 3;
            else if (score >= level.score2Star) starCount = 2;
            else if (score >= level.score1Star) starCount = 1;

            UIManager.Instance.UpdateScoringStars(starCount);
        }

        public void UpdateSpriteTarget()=> SetLevelType(level.Type);
        
        public void SetRemaining(int remaining) => UIManager.Instance.UpdateRemaining(remaining.ToString());

        public void SetRemaining(string remaining) => UIManager.Instance.UpdateRemaining(remaining);

        public void SetTarget(int target) =>
            UIManager.Instance.UpdateTarget(target.ToString());

        public void OnGameLose()
        {
            UIManager.Instance.ShowDefeatPanel();

            var defeatPanel = UIManager.Instance.GetPanel(3);
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                UIManager.Instance.UpdateMainButtonsState();
            }
        }

        void SaveStars(string levelName)
        {
            string numericPart = levelName.Replace("Level", "");
            if (!int.TryParse(numericPart, out int levelNumber)) return;

            string formattedLevelName = $"Level{levelNumber:D2}";
            int currentStars = PlayerPrefs.GetInt(formattedLevelName, 0);
            if (UIManager.Instance.currentStarCount > currentStars)
            {
                PlayerPrefs.SetInt(formattedLevelName, UIManager.Instance.currentStarCount);
                Debug.Log($"保存星星数：{formattedLevelName} = {UIManager.Instance.currentStarCount}");
            }
        }

        public void OnGameWin(int score)
        {
            var victoryPanel = UIManager.Instance.GetPanel(2);
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                UIManager.Instance.UpdateMainButtonsState();
                Debug.Log("胜利面板已激活");
            }

            foreach (var star in UIManager.Instance.victoryStars)
                star.gameObject.SetActive(false);

            for (int i = 0; i < UIManager.Instance.currentStarCount; i++)
            {
                if (i < UIManager.Instance.victoryStars.Length)
                    UIManager.Instance.victoryStars[i].gameObject.SetActive(true);
            }

            UIManager.Instance.ShowVictoryPanel(score);

            string currentLevelName = SceneManager.GetActiveScene().name;
            SaveStars(currentLevelName);
        }

        public void SetScore(int score)
        {
            int starCount = CalculateStarCount(score);
            UIManager.Instance.UpdateScoringStars(starCount);
            UIManager.Instance.currentStarCount = starCount;
            UIManager.Instance.SetScoreText(score);
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