using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Match3
{
    public class LevelMenuManager : MonoBehaviour
    {
        [Header("关卡总数")]
        public int totalLevels;

        [Header("音频控制")]
        public Toggle musicToggle;
        public Toggle soundToggle;

        [Header("功能面板")]
        public GameObject moreModesPanel;
        public GameObject accountOperationPanel;
        public GameObject profileToSelectPanel;
        public GameObject friendsListPanel;


        [Header("功能按钮")]
        public Button moreModesButton;
        public Button accountOperationButton;
        public Button profileFrameButton;
        public Button friendsButton;

        [Header("关卡面板")]
        public GameObject levelListPanel;
        public GameObject startLevelPanel;
        public Text levelText;
        private int currentSelectedLevel = 1;

        [Header("关闭按钮")]
        [Tooltip("顺序：0-模式 1-账号 2-好友")]
        public Button[] closeButtons;

        [Header("切换头像")]
        public Image currentProfileIcon;
       
        public static LevelMenuManager Instance;

        private void Awake()
        {
            Instance = this;
            
        }

        private void Start()
        {          
            if (musicToggle != null) 
                musicToggle.onValueChanged.AddListener(OnMusicToggleValueChanged);           

            SetAllPanelsInactive();
            InitializeAllButtons();  
            InitializeLevelSystem();
            InitializeProfileSelection();
            InitializeSoundToggle();
        }

        private void OnMusicToggleValueChanged(bool isOn)
        {
            if (GameMusicManager.Instance != null)
            {
                if (isOn) GameMusicManager.Instance.musicSource.Play();
                else GameMusicManager.Instance.musicSource.Pause();
            }
        }

        private void InitializeSoundToggle()
        {
            bool isSoundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            soundToggle.isOn = isSoundEnabled;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            SetAllSoundsMute(!isSoundEnabled);
        }
     
        private void OnSoundToggleChanged(bool isOn)
        {
            PlayerPrefs.SetInt("SoundEnabled", isOn ? 1 : 0);
            PlayerPrefs.Save();
            SetAllSoundsMute(!isOn);
        }

        private void SetAllSoundsMute(bool mute)
        {
            if (GameCanvasManager.Instance != null && GameCanvasManager.Instance.uiAudioSource != null)
            {
                GameCanvasManager.Instance.uiAudioSource.mute = mute;
            }

            GameGrid[] gameGrids = FindObjectsOfType<GameGrid>();
            foreach(GameGrid grid in gameGrids)
            {
                if (grid.audioSource != null) grid.audioSource.mute = mute;
            }

            if (SoundManager.Instance != null && SoundManager.Instance.audioSource != null)
            {
                SoundManager.Instance.audioSource.mute = mute;
            }
            
        }

        private void InitializeAllButtons()
        {
            moreModesButton.onClick.AddListener(() => TogglePanel(moreModesPanel));
            accountOperationButton.onClick.AddListener(() => TogglePanel(accountOperationPanel));
            profileFrameButton.onClick.AddListener(() =>
            {
                if (profileToSelectPanel.activeSelf)
                    ClosePanel(profileToSelectPanel);
                else TogglePanel(profileToSelectPanel);
            });
            friendsButton.onClick.AddListener(() => TogglePanel(friendsListPanel));

            if (closeButtons.Length == 3)
            {
                closeButtons[0].onClick.AddListener(() => ClosePanel(moreModesPanel));
                closeButtons[1].onClick.AddListener(() => ClosePanel(accountOperationPanel));
                closeButtons[2].onClick.AddListener(() => ClosePanel(friendsListPanel));
            }

            moreModesPanel.transform.Find("3DModeButton").GetComponent<Button>().onClick.AddListener(() => LoadScene("3DMode"));
            moreModesPanel.transform.Find("ARModeButton").GetComponent<Button>().onClick.AddListener(() => LoadScene("ARMode"));
        }

        private void TogglePanel(GameObject targetPanel)
        {
            SetAllPanelsInactive();
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                Time.timeScale = 0;
            }
            else Time.timeScale = 0;
        }

        private void ClosePanel(GameObject panel)
        {
            panel.SetActive(false);
            Time.timeScale = 1;
        }

        private void SetAllPanelsInactive()
        {
            moreModesPanel.SetActive(false);
            accountOperationPanel.SetActive(false);
            profileToSelectPanel.SetActive(false);
            friendsListPanel.SetActive(false);
        }

        private void LoadScene(string sceneName)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(sceneName);
        }

       private  void InitializeProfileSelection()
        {
            foreach (Transform child in profileToSelectPanel.transform)
            {
                if (child.name.StartsWith("PickProfileButton"))
                {
                    Button pickButton = child.GetComponent<Button>();
                    Image targetIcon = child.Find("ProfileIcon").GetComponent<Image>();

                    pickButton.onClick.AddListener(() =>
                    {
                        currentProfileIcon.sprite = targetIcon.sprite;
                        profileToSelectPanel.SetActive(false);
                        Time.timeScale = 1;
                    });
                }
            }
        }

        public void UpdateLevelButtonsDisplay()
        {
            for (int i = 1; i <= totalLevels; i++)
            {
                string levelKey = $"Level{i:D2}";
                int stars = PlayerPrefs.GetInt(levelKey, 0);
                Transform levelButton = levelListPanel.transform.Find($"LevelButton{i}");

                if (levelButton != null)
                {
                    bool isUnlocked = (i == 1) || (PlayerPrefs.GetInt($"Level{(i - 1):D2}", 0) > 0); // 关卡解锁条件

                    levelButton.gameObject.SetActive(isUnlocked);
                    levelButton.Find("number").gameObject.SetActive(isUnlocked);

                    levelButton.Find("star1")?.gameObject.SetActive(isUnlocked && stars >= 1);
                    levelButton.Find("star2")?.gameObject.SetActive(isUnlocked && stars >= 2);
                    levelButton.Find("star3")?.gameObject.SetActive(isUnlocked && stars >= 3);

                    levelButton.GetComponent<Button>().interactable = isUnlocked;
                }
            }
        }

        private void InitializeLevelSystem()
        {
            for (int i = 1; i <= totalLevels; i++)
            {
                Button levelButton = levelListPanel.transform.Find($"LevelButton{i}")?.GetComponent<Button>();
                if (levelButton != null)
                {
                    int levelNumber = i;
                    levelButton.onClick.AddListener(() => ShowStartLevelPanel(levelNumber));
                }
            }

            Button startButton = startLevelPanel.transform.Find("StartLevelButton")?.GetComponent<Button>();
            if (startButton != null) startButton.onClick.AddListener(LoadSelectedLevel);

            Button cancelButton = startLevelPanel.transform.Find("CancelStartPanelButton")?.GetComponent<Button>();
            if (cancelButton != null) cancelButton.onClick.AddListener(() => ClosePanel(startLevelPanel));

            UpdateLevelButtonsDisplay();
        }      

        private void ShowStartLevelPanel(int levelNumber)
        {
            currentSelectedLevel = levelNumber;
            UpdateLevelTextDisplay();
            TogglePanel(startLevelPanel);
        }

        private void UpdateLevelTextDisplay()
        {
            if (levelText != null) levelText.text = $"第{currentSelectedLevel}关卡";
        }

        private void LoadSelectedLevel()
        {                   
            string sceneName = GenerateSceneName();
            if (CheckSceneExists(sceneName))
            {
                Time.timeScale = 1;
                PlayerPrefs.SetString("TargetLevelScene", sceneName);
                SceneManager.LoadScene("LevelLoading");
                ClosePanel(startLevelPanel);
            }
            UpdateLevelButtonsDisplay();
        }

        private string GenerateSceneName()
        {
            return $"Level{currentSelectedLevel:D2}";
        }

        private bool CheckSceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string scene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (scene == sceneName) return true;
            }
            return false;
        }

        public void RevealNextLevelButton(int currentLevel)
        {
            int nextLevel = currentLevel + 1;
            string nextLevelButtonName = $"LevelButton{nextLevel}";
            Transform nextLevelButton = levelListPanel.transform.Find(nextLevelButtonName);

            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);
                nextLevelButton.Find("number").gameObject.SetActive(true);
                nextLevelButton.GetComponent<Button>().interactable = true;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "LevelList")
            {
                UpdateLevelButtonsDisplay();
            }
        }
    }
}

