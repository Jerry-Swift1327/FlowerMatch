using UnityEngine;

namespace Match3
{
    public class LevelTimer : Level
    {

        public int timeInSeconds;
        public int targetScore;
        [HideInInspector]
        public float _timer;

        private void Start ()
        {
            Type = LevelType.Timer;          
            hud.SetLevelType(Type);
            hud.SetScore(currentScore);
            hud.SetTarget(targetScore);
            hud.SetRemaining($"{timeInSeconds / 60}:{timeInSeconds % 60:00}");
        }

        private void Update()
        {
            if (uiManager.isHourglassMode) return;
            _timer += Time.deltaTime;
            hud.SetRemaining(
                $"{(int) Mathf.Max((timeInSeconds - _timer) / 60, 0)}:{(int) Mathf.Max((timeInSeconds - _timer) % 60, 0):00}");

            if (timeInSeconds - _timer <= 0)
            {
                if (currentScore >= targetScore) GameWin();             
                else GameLose();             
            }
        }      
	
    }
}
