using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class Level : MonoBehaviour
    {
        public GameGrid gameGrid;
        public GameCanvasManager gameCanvasManager;
        public Hud hud;

        [Header("评分阈值")]
        public int score1Star;
        public int score2Star;
        public int score3Star;
        protected int currentScore;
        private int _scoreMultiplier = 1;

        protected bool isGameOver = false;

        public LevelType Type { get; set; }

        private bool _didWin;
        public bool Didwin => _didWin;

        private void Awake()
        {
            gameCanvasManager = FindObjectOfType<GameCanvasManager>();
            hud = FindObjectOfType<Hud>();
        }

        // 游戏胜利时调用
        protected void GameWin()
        {
            if (isGameOver) return;
            isGameOver = true;
            _didWin = true;
            gameGrid.GameOver();
            StartCoroutine(WaitForGridFillAndClear(() => hud.OnGameWin(currentScore)));
        }

        // 游戏失败时调用
        protected void GameLose()
        {
            if (isGameOver) return;
            isGameOver = true;
            _didWin = false;
            gameGrid.GameOver();
            StartCoroutine(WaitForGridFillAndClear(() => hud.OnGameLose()));
        }

        public virtual void OnMove() 
        {
            if (gameCanvasManager.isHourglassMode) return;
        }
       
        public virtual void OnPieceCleared(GamePiece piece)
        {
            if (!enabled) return;
            currentScore += piece.score*_scoreMultiplier;
            hud.SetScore(currentScore);
            _scoreMultiplier = 1;
        }

        public void MarkScoreMultiplier(int multiplier)=> _scoreMultiplier = multiplier;

        //游戏结束时等待网格填充完成并消除可消除的组合
        protected IEnumerator WaitForGridFillAndClear(System.Action onComplete)
        {
            bool hasMatches;
            int maxRecursionDepth = 20;
            int currentDepth = 0;

            do
            {
                yield return new WaitUntil(() => !gameGrid.IsFilling);

                if (gameGrid._gameOver || currentDepth >= maxRecursionDepth) break;

                hasMatches = gameGrid.ClearAllValidMatches();
                if (hasMatches)
                {
                    yield return gameGrid.StartCoroutine(gameGrid.Fill());
                    currentDepth++;
                }
                yield return null;
            } while (hasMatches);

            yield return new WaitUntil(() => !gameGrid.IsFilling);
            gameGrid.ClearAllValidMatches();

            this.enabled = false; // 禁用 Level 脚本，避免继续响应
            hud.SetScore(currentScore);
            yield return null;
            onComplete?.Invoke();
        }
    }
}