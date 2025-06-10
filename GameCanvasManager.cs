using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Match3
{
    public class GameCanvasManager : MonoBehaviour
    {
        public static GameCanvasManager Instance;

        [Header("��������")]
        public Level level;
        public Hud hud;

        [Header("������Դ")]
        public Sprite[] colorSprites;
        public Sprite[] obstacleSprites;
        public Sprite specialSprites;

        [Header("��Ϸ״̬")]
        public float remainingTime = 15f;
        public bool IsInputBlocked { get; private set; }
        [HideInInspector] public int currentStarCount;
        [HideInInspector] public bool isHourglassMode;

        [Header("��Ƶ����")]
        public AudioSource uiAudioSource;
        public AudioClip victorySound;
        public AudioClip defeatSound;
        public AudioClip smashSound;

        [Header("UI��ť")]
        public Button settingsButton;
        public Button pauseButton;
        public Button musicButton;
        public Button soundButton;
        public Button hourglassButton;

        [Header("��Ƶͼ��״̬")]
        public Image nullImageMusic;
        public Image nullImageSound;

        [Header("ȫ�������")]
        [Tooltip("˳��0-���� 1-�˳� 2-ʤ�� 3-ʧ�� 4-���� 5-��ͣ 6-����")]
        public GameObject[] allPanels;

        [Header("ʤ�����")]
        public Text victoryScoreText;
        public Image[] victoryStars;

        [Header("ʧ�����")]
        public Text loseText;

        [Header("ʣ��״̬��ʾ")]
        public Text remainingText;
        public Text remainingSubtext;

        [Header("Ŀ����ʾ")]
        public Image targetSprite;
        public Text targetNumber;
        public Image checkmark;
        public Image targetSprite_1;
        public Text targetNumber_1;
        public Image checkmark_1;

        [Header("�Ʒ����")]
        public Text levelNumberText;
        public Text scoringText;
        public Image[] scoringStars;

        [Header("��ʾ��Ϣ")]
        public Text notificationText;

        private void Awake()
        {
            Instance = this;
            InitializeUI();
        }
        public void InitializeUI()
        {
            foreach (var panel in allPanels) panel.SetActive(false);

            UpdateScoringStars(0);

            if (levelNumberText != null)
            {
                int currentLevel = GetCurrentLevel();
                levelNumberText.text = $"��{currentLevel}��";
            }

            settingsButton.onClick.AddListener(() => TogglePanel(0));
            musicButton.onClick.AddListener(OnMusicButtonClick);
            soundButton.onClick.AddListener(OnSoundButtonClick);
            hourglassButton.onClick.AddListener(OnHourglassButtonClick);
            hourglassButton.interactable = true;

            bool isSoundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            nullImageSound.gameObject.SetActive(!isSoundEnabled);
        }

        private void OnMusicButtonClick()
        {
            if (GameMusicManager.Instance != null)
            {
                if (GameMusicManager.Instance.musicSource.isPlaying)
                {
                    GameMusicManager.Instance.musicSource.Pause();
                    nullImageMusic.gameObject.SetActive(true);
                }
                else
                {
                    GameMusicManager.Instance.musicSource.Play();
                    nullImageMusic.gameObject.SetActive(false);
                }
            }
        }

        private void OnSoundButtonClick()
        {
            bool isSoundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            isSoundEnabled = !isSoundEnabled;

            PlayerPrefs.SetInt("SoundEnabled", isSoundEnabled ? 1 : 0);
            PlayerPrefs.Save();

            nullImageSound.gameObject.SetActive(!isSoundEnabled);
            SetAllSoundsMute(!isSoundEnabled);
        }

        private void OnHourglassButtonClick()
        {
            if (!hourglassButton.interactable) return;

            isHourglassMode = true;
            hourglassButton.interactable = false;
            notificationText.gameObject.SetActive(true);
            notificationText.text = $"����ɳ©ģʽ��ʱ�����ֹͣ���£�{remainingTime}���ָ�";
            StartCoroutine(HourglassCountdown());
        }

        private IEnumerator HourglassCountdown()
        {
            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                notificationText.text = $"����ɳ©ģʽ������ֹͣ���£�{Mathf.CeilToInt(remainingTime)}���ָ�";
                yield return null;
            }

            isHourglassMode = false;
            notificationText.gameObject.SetActive(false);
        }

        private void SetAllSoundsMute(bool mute)
        {
            if (uiAudioSource != null) uiAudioSource.mute = mute;

            foreach (GameGrid grid in FindObjectsOfType<GameGrid>())
                if (grid.audioSource != null)grid.audioSource.mute = mute;

            if (SoundManager.Instance != null && SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.mute = mute;
        }

        private void TogglePanel(int panelIndex)
        {
            GameObject targetPanel = GetPanel(panelIndex);
            if (targetPanel == null) return;

            switch (panelIndex)
            {
                case 0: // ������壨����0�������˳���廥��
                    targetPanel.SetActive(!targetPanel.activeSelf);
                    GetPanel(1)?.SetActive(false); // �ر��˳����
                    break;
                case 1: // �˳���壨����1���������л�
                    targetPanel.SetActive(!targetPanel.activeSelf);
                    break;
                case 4: // ������壨����4�������������ر������Ǳ�Ҫ���
                    CloseNonEssentialPanels();
                    targetPanel.SetActive(!targetPanel.activeSelf);
                    break;
                default: // ������壨ʤ����ʧ�ܵȣ����ر���������������ʾ
                    CloseAllPanels();
                    targetPanel.SetActive(true);
                    break;
            }

            UpdateMainButtonsState();
        }

        // �رշǱ�Ҫ��壨����ʤ��/ʧ����壩 ʤ��(2), ʧ��(3)
        private void CloseNonEssentialPanels()
        {
            for (int i = 0; i < allPanels.Length; i++)
            {
                if (i == 2 || i == 3) continue;
                var panel = GetPanel(i);
                if (panel != null && panel.activeSelf) panel.SetActive(false);
            }
        }

        private void CloseAllPanels()
        {
            for (int i = 0; i < allPanels.Length; i++)
            {
                if (i == 7 || i == 8) continue;
                allPanels[i].SetActive(false);
            }
        }

        // �ر�������壨��ָ�������⣩
        private void CloseAllPanelsExcept(int exceptIndex)
        {
            for (int i = 0; i < allPanels.Length; i++)
            {
                if (i == exceptIndex) continue;
                allPanels[i].SetActive(false);
            }
        }

        public GameObject GetPanel(int index)
        {
            if (index < 0 || index >= allPanels.Length) return null;
            return allPanels[index];
        }

        public void SetTargetDisplayMode(LevelType levelType)
        {
            targetSprite.gameObject.SetActive(false);
            targetSprite_1.gameObject.SetActive(false);

            switch (levelType)
            {
                case LevelType.DoubleColorMoves:
                    targetSprite.gameObject.SetActive(true);
                    targetSprite_1.gameObject.SetActive(true);
                    break;

                default:
                    targetSprite.gameObject.SetActive(true);
                    break;
            }
        }

        public void SetSingleSpriteTarget<T>(T targetType, int count)
        {
            Sprite targetSprite = GetSpriteByType(targetType);
            this.targetSprite.sprite = targetSprite;
            targetNumber.text = count > 0 ? count.ToString() : " ";
            checkmark.gameObject.SetActive(count == 0);
        }

        public void SetDoubleSpriteTarget<T1, T2>(T1 targetType_1, int count_1, T2 targetType_2, int count_2)
        {
            Sprite sprite_1 = GetSpriteByType(targetType_1);
            targetSprite.sprite = sprite_1;
            targetNumber.text = count_1 > 0 ? count_1.ToString() : " ";
            checkmark.gameObject.SetActive(count_1 == 0);

            Sprite sprite_2 = GetSpriteByType(targetType_2);
            targetSprite_1.sprite = sprite_2;
            targetNumber_1.text = count_2 > 0 ? count_2.ToString() : " ";
            checkmark_1.gameObject.SetActive(count_2 == 0);
        }

        private Sprite GetSpriteByType(object targetType)
        {
            if (targetType is ColorType colorType)
            {
                switch (colorType)
                {
                    case ColorType.Red: return colorSprites[0];
                    case ColorType.Orange: return colorSprites[1];
                    case ColorType.Yellow: return colorSprites[2];
                    case ColorType.Green: return colorSprites[3];
                    case ColorType.Blue: return colorSprites[4];
                    case ColorType.Pink: return colorSprites[5];
                    case ColorType.Purple: return colorSprites[6];
                    case ColorType.White: return colorSprites[7];
                    case ColorType.Black: return colorSprites[8];
                    default: return null;
                }
            }
            else if (targetType is PieceType pieceType)
            {
                switch (pieceType)
                {
                    case PieceType.Rainbow: return specialSprites;
                    default: return null;
                }
            }
            else if (targetType is ObstacleType obstacleType)
            {
                switch (obstacleType)
                {
                    case ObstacleType.Bubble: return obstacleSprites[0];
                    case ObstacleType.Grass: return obstacleSprites[1];
                    case ObstacleType.Bubble_1:return obstacleSprites[2];
                    case ObstacleType.Bubble_2:return obstacleSprites[3];
                    case ObstacleType.Bubble_3:return obstacleSprites[4];

                    default: return null;
                }
            }
            return null;
        }

        public void ShowVictoryPanel(int score)
        {
            CloseAllPanels();
            allPanels[2].SetActive(true);
            if (victoryScoreText != null)
            {
                victoryScoreText.text = score.ToString();
                victoryScoreText.gameObject.SetActive(true);
            }

            UpdateVictoryStars(currentStarCount);
            UpdateMainButtonsState();

            for (int i = 0; i < victoryStars.Length; i++)
            {
                victoryStars[i].gameObject.SetActive(i < currentStarCount);
            }

            if (uiAudioSource != null && victorySound != null)
            {
                uiAudioSource.Stop();
                uiAudioSource.PlayOneShot(victorySound);
            }
        }

        public void ShowDefeatPanel()
        {
            CloseAllPanels();
            allPanels[3].SetActive(true);
            UpdateMainButtonsState();
            if (uiAudioSource != null && defeatSound != null)
            {
                uiAudioSource.Stop();
                uiAudioSource.PlayOneShot(defeatSound);
            }
        }

        public void UpdateScoringStars(int starCount)
        {
            for (int i = 0; i < scoringStars.Length; i++)
            {
                scoringStars[i].gameObject.SetActive(i <= starCount);
            }
        }

        public void UpdateVictoryStars(int starCount)
        {
            for (int i = 0; i < victoryStars.Length; i++)
            {
                victoryStars[i].gameObject.SetActive(i + 1 <= starCount);
            }
        }

        public void ForceSyncStars()
        {
            UpdateVictoryStars(currentStarCount);
            UpdateScoringStars(currentStarCount);
        }

        public void UpdateMainButtonsState()
        {
            var victory = GetPanel(2);
            var defeat = GetPanel(3);
            if (victory == null || defeat == null) return;

            bool isBlock = victory.activeSelf || defeat.activeSelf;
            settingsButton.interactable = !isBlock;
            pauseButton.interactable = !isBlock;
            hourglassButton.interactable = !isBlock;
        }

        public void UpdateRemaining(string value) => remainingText.text = value;

        public void SetScoreText(int score) => scoringText.text = score.ToString();

        public void OnCloseRankingButton() //�ر����а�ť
        {
            var rankingPanel = GetPanel(4);
            if (rankingPanel != null) rankingPanel.SetActive(false);

            var victoryPanel = GetPanel(2);
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                UpdateMainButtonsState();
            }
        }

        public void OnContinueButton()  // �ɹ�����µļ�����ť
        {
            SceneManager.LoadScene("LevelList");
            if (LevelMenuManager.Instance != null)
            {
                LevelMenuManager.Instance.UpdateLevelButtonsDisplay();
                int currentLevel = GetCurrentLevel();
                if (currentLevel >= 1 && currentLevel < LevelMenuManager.Instance.totalLevels)
                    LevelMenuManager.Instance.RevealNextLevelButton(currentLevel);
            }
        }

        //�˳������˳���ť
        private void OnExitButton() => SceneManager.LoadScene("LevelList");

        //ʧ�����ķ��ذ�ť
        private void OnReturnButton() => SceneManager.LoadScene("LevelList");

        // ʧ���������水ť
        public void OnReplayButton() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // ȷ����;���水ť
        public void OnConfirmRestartButton()
        {
            allPanels[6].SetActive(false);
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnRankingButton() // ���а�ť
        {
            var victoryPanel = GetPanel(2);
            bool victoryPanelActive = victoryPanel != null && victoryPanel.activeSelf;

            CloseNonEssentialPanels();
            allPanels[4].SetActive(true);
            if (victoryPanelActive) victoryPanel.SetActive(true);
        }
        public void OnPauseButton() //��ͣ��ť
        {
            TogglePanel(5);
            Time.timeScale = 0;
            IsInputBlocked = true;
        }
        public void OnResumeButton() //�ָ���Ϸ��ť
        {
            allPanels[5].SetActive(false);
            Time.timeScale = 1;
            IsInputBlocked = false;
        }
        public void OnFinishButton() //���˳���尴ť
        {
            CloseAllPanelsExcept(1);
            allPanels[1].SetActive(true);
            Time.timeScale = 0;
            IsInputBlocked = true;
        }

        public void OnRestartButton() //���������
        {
            allPanels[6].SetActive(true);
            Time.timeScale = 0;
            IsInputBlocked = true;
        }

        public void OnCloseExitButton() //�ر��˳���尴ť
        {
            allPanels[1].SetActive(false);
            Time.timeScale = 1;
            IsInputBlocked = false;
        }

        public void OnCloseRestartButton() //�ر�������尴ť
        {
            allPanels[6].SetActive(false);
            Time.timeScale = 1;
            IsInputBlocked = false;
        }

        private int GetCurrentLevel()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.StartsWith("Level"))
            {
                string levelPart = sceneName.Substring(5);
                if (int.TryParse(levelPart, out int level))
                    return level;
            }
            return 1; // Ĭ�Ϸ��عؿ�1
        }
    }
}
