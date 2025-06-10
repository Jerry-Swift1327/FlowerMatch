using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Match3
{
    public class LevelMenuManager : MonoBehaviour
    {
        public static LevelMenuManager Instance;

        [Header("音频控制")]
        public Toggle musicToggle;
        public Toggle soundToggle;

        [Header("切换头像")]
        public Button profileButton;
        public GameObject profileToSelectPanel;

        [Header("关卡面板")]
        public GameObject levelListPanel;
        public GameObject startLevelPanel;
        public Text levelText;       

        [Header("显示玩家信息")]
        public Text gameNameText;
        public Image currentProfileIcon;

        [HideInInspector] public int totalLevels;
        private int currentSelectedLevel = 1;

        private void Awake()
        {
            Instance = this;           
        }

        private void Start()
        {          
            if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggleValueChanged);           

            SetAllPanelsInactive();
            InitializeAllButtons();  
            InitializeLevelSystem();
            InitializeProfileSelection();
            InitializeSoundToggle();
            InitializeProfileDisplay();
            LoadSavedGameName();
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
                GameCanvasManager.Instance.uiAudioSource.mute = mute;

            foreach(GameGrid grid in FindObjectsOfType<GameGrid>())
                if (grid.audioSource != null) grid.audioSource.mute = mute;

            if (SoundManager.Instance != null && SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.mute = mute;            
        }

        private void InitializeAllButtons()
        {
            profileButton.onClick.AddListener(() =>
            {
                if (profileToSelectPanel.activeSelf)
                    ClosePanel(profileToSelectPanel);
                else TogglePanel(profileToSelectPanel);
            });
        }

        private void InitializeProfileDisplay()
        {
            int slotIndex = PlayerPrefs.GetInt("LastUsedArchiveSlot", 0);
            int selectedIndex = PlayerPrefs.GetInt($"SelectedProfileIndex_{slotIndex}", 0);

            List<Sprite> profileSprites = new List<Sprite>();

            foreach(Transform child in profileToSelectPanel.transform)
            {
                if(child.name.StartsWith("ProfileButton"))
                {
                    Image targetIcon = child.Find("ProfileIcon").GetComponent<Image>();
                    profileSprites.Add(targetIcon.sprite);
                }
            }

            if (selectedIndex >= 0 && selectedIndex < profileSprites.Count && currentProfileIcon != null)
                currentProfileIcon.sprite = profileSprites[selectedIndex];

            SaveCurrentProfileIcon();
        }

        private void LoadSavedGameName()
        {
            int slotIndex = PlayerPrefs.GetInt("LastUsedArchiveSlot", 0);
            string savedName = PlayerPrefs.GetString($"GameName_{slotIndex}", "玩家");
            if (gameNameText != null) gameNameText.text = savedName;
        }

        private void TogglePanel(GameObject targetPanel)
        {
            SetAllPanelsInactive();
            if (targetPanel != null) targetPanel.SetActive(true);
            Time.timeScale = 0;
        }

        private void ClosePanel(GameObject panel)
        {
            panel.SetActive(false);
            Time.timeScale = 1;
        }

        private void SetAllPanelsInactive()=> profileToSelectPanel.SetActive(false);

        private void LoadScene(string sceneName)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(sceneName);
        }

       private void InitializeProfileSelection()
        {
            foreach (Transform child in profileToSelectPanel.transform)
            {
                if (child.name.StartsWith("ProfileButton"))
                {
                    Button pickButton = child.GetComponent<Button>();
                    Image targetIcon = child.Find("ProfileIcon").GetComponent<Image>();

                    pickButton.onClick.AddListener(() =>
                    {
                        currentProfileIcon.sprite = targetIcon.sprite;
                        profileToSelectPanel.SetActive(false);
                        Time.timeScale = 1;

                        SaveCurrentProfileIcon();
                    });
                }
            }
        }

        public void SaveCurrentProfileIcon()
        {
            int slotIndex = PlayerPrefs.GetInt("LastUsedArchiveSlot", 0);
            foreach (Transform child in profileToSelectPanel.transform)
            {
                if (child.name.StartsWith("ProfileButton"))
                {
                    Image icon = child.Find("ProfileIcon")?.GetComponent<Image>();
                    if (icon != null && icon.sprite == currentProfileIcon.sprite)
                    {
                        string name = child.name.Replace("ProfileButton", "");
                        if (int.TryParse(name, out int index))
                        {
                            index -= 1;
                            PlayerPrefs.SetInt($"CurrentProfileInLevel_{slotIndex}", index);
                            PlayerPrefs.Save();
                            break;
                        }
                    }
                }
            }
        }

        public void UpdateLevelButtonsDisplay()
        {
            int slotIndex = PlayerPrefs.GetInt("LastUsedArchiveSlot", -1);
            if (slotIndex < 0) return;

            for (int i = 1; i <= totalLevels; i++)
            {
                string levelKey = $"Archive{slotIndex}_Level{i:D2}";
                int stars = PlayerPrefs.GetInt(levelKey, -1);
                Transform levelButton = levelListPanel.transform.Find($"LevelButton{i}");

                if (levelButton != null)
                {
                    bool isUnlocked = false;

                    if (i == 1)
                    {
                        isUnlocked = true;
                        if (!PlayerPrefs.HasKey(levelKey))
                        {
                            PlayerPrefs.SetInt(levelKey, 0);
                            PlayerPrefs.Save();
                        }
                    }
                    else
                    {
                        string prevKey = $"Archive{slotIndex}_Level{(i - 1):D2}";
                        int prevStars = PlayerPrefs.GetInt(prevKey, -1);
                        isUnlocked = prevStars > 0;
                    }

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

        private string GenerateSceneName() => $"Level{currentSelectedLevel:D2}";

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
            int slotIndex = PlayerPrefs.GetInt("LastUsedArchiveSlot", -1);
            if (slotIndex < 0) return;

            int nextLevel = currentLevel + 1;
            string key = $"Archive{slotIndex}_Level{nextLevel:D2}";

            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 0);
                PlayerPrefs.Save();
            }

            string nextLevelButtonName = $"LevelButton{nextLevel}";
            Transform nextLevelButton = levelListPanel.transform.Find(nextLevelButtonName);

            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);
                nextLevelButton.Find("number").gameObject.SetActive(true);
                nextLevelButton.GetComponent<Button>().interactable = true;
            }
        }

        private void OnEnable()=> SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable()=> SceneManager.sceneLoaded -= OnSceneLoaded;     

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "LevelList") UpdateLevelButtonsDisplay();
        }
    }
}

