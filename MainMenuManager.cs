using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("�������")]
    public GameObject loadingPanel;
    public Slider loadingBar;
    public Text loadingValueText;
    private float loadingProgress = 0f;

    [Header("��ʼ��Ϸ��ť")] public Button startGameButton;

    [Header("�ر���尴ť")] public Button[] closePanelButton;

    [Header("�浵���")]
    public GameObject archivePanel;
    public Button[] archiveEnterGameButton;
    public Text[] archiveEnterGameText;
    private int clickedArchiveSlotIndex = -1;

    [Header("����Ϸ���")]
    public GameObject newGamePanel;
    public Text gameNameErrorText;
    public InputField gameNameInput;
    public Text gameNameCountText;
    public Button confirmationButton;
    private const int defaultCharCount = 20;

    [Header("ͷ��ѡ��")]
    public Text profileErrorText;
    public GameObject profileToSelectGroup;
    private int selectedProfileIndex = -1;
    private Button currentSelectedProfileButton;

    private List<Button> profileButtons = new List<Button>();
    private Image[] archiveSlotProfileIcons;

    private void Start()
    {
        Instance = this;
        archivePanel.SetActive(false);
        newGamePanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);

        InitializeProfileSelection();
        UpdateGameNameCountText(gameNameInput.text);
        StartCoroutine(LoadGame());

        confirmationButton.onClick.AddListener(OnConfirmationButtonClick);
        gameNameInput.onValueChanged.AddListener(UpdateGameNameCountText);

        gameNameErrorText.text = $"��Ϸ�ǳƲ���Ϊ�ա����ո񡢳��ȳ���{defaultCharCount}���ַ���";
        profileErrorText.text = "��ѡ���·������һ�仨��Ϊ��Ϸͷ��";

        for (int i = 0; i < archiveEnterGameButton.Length; i++)
        {
            int index = i;
            archiveEnterGameText[i].text = PlayerPrefs.GetInt($"ArchiveSlot{index}_Used", 0) == 1 ? "������Ϸ" : "�µ���Ϸ";
            archiveEnterGameButton[i].onClick.AddListener(() => OnArchiveEnterGameButtonClick(index));
        }

        foreach (Button closeButton in closePanelButton)
            closeButton.onClick.AddListener(OnClosePanelButtonClick);

        CacheArchiveSlotProfileIcons();
        InitializeArchiveSlotProfiles();
    }

    //������Ϸ
    private IEnumerator LoadGame()
    {
        while (loadingProgress < 1f)
        {
            loadingProgress += 0.01f;
            loadingBar.value = loadingProgress;
            loadingValueText.text = $"{Mathf.FloorToInt(loadingProgress * 100)}%";
            yield return new WaitForSeconds(0.035f);
        }
        loadingPanel.SetActive(false);
        startGameButton.gameObject.SetActive(true);
        startGameButton.onClick.AddListener(OnStartGameButtonClick);
    }

    private void OnStartGameButtonClick() => archivePanel.SetActive(true);

    private void CacheArchiveSlotProfileIcons()
    {
        Transform archiveGroups = archivePanel.transform.Find("ArchiveGroups");
        archiveSlotProfileIcons = new Image[archiveEnterGameButton.Length];

        for (int i = 0; i < archiveEnterGameButton.Length; i++)
        {
            Transform slot = archiveGroups.Find($"ArchiveSlot_{(i == 0 ? "" : i.ToString())}".TrimEnd('_'));
            var icon = slot?.Find("GameProfile")?.GetComponent<Image>();
            archiveSlotProfileIcons[i] = icon;
        }
    }

    private void InitializeArchiveSlotProfiles()
    {
        List<Sprite> profileSprites = new List<Sprite>();
        foreach (Transform child in profileToSelectGroup.transform)
        {
            Image icon = child.Find("ProfileIcon")?.GetComponent<Image>();
            if (icon != null) profileSprites.Add(icon.sprite);
        }

        for (int i = 0; i < archiveSlotProfileIcons.Length; i++)
        {
            Image profileIcon = archiveSlotProfileIcons[i];
            if (profileIcon == null) continue;

            bool isUsed = PlayerPrefs.GetInt($"ArchiveSlot{i}_Used", 0) == 1;

            if (!isUsed)
            {
                profileIcon.gameObject.SetActive(false);
            }
            else
            {
                profileIcon.gameObject.SetActive(true);
                int profileIndex = PlayerPrefs.GetInt($"CurrentProfileInLevel_{i}", -1);
                if (profileIndex >= 0 && profileIndex < profileSprites.Count)
                    profileIcon.sprite = profileSprites[profileIndex];
            }
        }
    }


    private void OnArchiveEnterGameButtonClick(int index)
    {
        clickedArchiveSlotIndex = index;
        int isUsed = PlayerPrefs.GetInt($"ArchiveSlot{index}_Used", 0);
        if (isUsed == 1)
        {
            PlayerPrefs.SetInt("LastUsedArchiveSlot", index);
            PlayerPrefs.Save();
            SceneManager.LoadScene("LevelList");
        }
        else
        {
            archivePanel.SetActive(false);
            newGamePanel.SetActive(true);
        }
    }

    private void OnClosePanelButtonClick()
    {
        if (archivePanel.activeSelf) archivePanel.SetActive(false);
        else if (newGamePanel.activeSelf)
        {
            ResetNewGamePanel();
            newGamePanel.SetActive(false);
            archivePanel.SetActive(true);
        }
    }

    public void ResetNewGamePanel()
    {
        gameNameInput.text = "";
        UpdateGameNameCountText("");       
        selectedProfileIndex = -1;

        if (currentSelectedProfileButton != null)
        {
            Transform prevCheckmark = currentSelectedProfileButton.transform.Find("Checkmark"); //
            if (prevCheckmark != null) prevCheckmark.gameObject.SetActive(false);
            currentSelectedProfileButton = null;
        }
    }

    private void UpdateGameNameCountText(string input)
    {
        int charCount = CountValidCharacters(input);
        gameNameCountText.text = $"{charCount} / {defaultCharCount}";
    }

    private void InitializeProfileSelection() 
    {
        currentSelectedProfileButton = null;
        profileButtons.Clear();

        foreach (Transform child in profileToSelectGroup.transform)
        {
            Button profileButton = child.GetComponent<Button>();
            if (profileButton == null) continue;

            Transform checkmark = child.Find("Checkmark"); //
            if (checkmark != null) checkmark.gameObject.SetActive(false);

            profileButton.onClick.AddListener(() => HandleProfileButtonClick(profileButton, checkmark, child.name));
            profileButtons.Add(profileButton);
        }
    }

    private void HandleProfileButtonClick(Button selectedButton, Transform checkmark, string buttonName) //
    {
        string indexString = buttonName.Replace("ProfileButton", "").Trim();
        if (!int.TryParse(indexString, out int index)) return;
        index -= 1;
        if (index < 0) return;

        if (currentSelectedProfileButton == selectedButton)
        {
            checkmark.gameObject.SetActive(false);
            currentSelectedProfileButton = null;
            selectedProfileIndex = -1;
            return;
        }

        if (currentSelectedProfileButton != null)
        {
            Transform prevCheckmark = currentSelectedProfileButton.transform.Find("Checkmark");
            prevCheckmark?.gameObject.SetActive(false);
        }
        checkmark.gameObject.SetActive(true);
        currentSelectedProfileButton = selectedButton;
        selectedProfileIndex = index;
    }

    private bool IsChineseCharacter(char ch) => ch >= 0x4e00 && ch <= 0x9fff; 

    private int CountValidCharacters(string input)
    {
        int count = 0;
        foreach (char ch in input)
        {
            if (char.IsLetterOrDigit(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch))
            {
                if (IsChineseCharacter(ch)) count += 2; // ��Ϊ������ռ2����λ
                else count += 1; // ��Ϊ������ռ1����λ
            }
        }
        return count;
    }

    private bool IsValidGameName(string gameName)
    {
        if (string.IsNullOrEmpty(gameName) || gameName.Contains(" ")) return false;
        int charCount = CountValidCharacters(gameName);
        return charCount <= defaultCharCount && charCount > 0;
    }

    private void OnConfirmationButtonClick()
    {      
        string gameName = gameNameInput.text;
        bool isGameNameValid = IsValidGameName(gameName);
        if (!isGameNameValid)
        {
            int charCount = CountValidCharacters(gameName);
            if (string.IsNullOrEmpty(gameName) || gameName.Contains(" "))
                gameNameErrorText.text = "��Ϸ�ǳƲ���Ϊ�ջ��߰����ո�";
            else if (charCount > 15)
                gameNameErrorText.text = $"��Ϸ�ǳƳ��Ȳ��ܳ���{defaultCharCount}���ַ�����ǰ��{charCount}Ϊ���ַ�����";
            else gameNameErrorText.text = "��Ϸ�ǳ�ֻ�ܰ������֡����ֺͱ����ţ�";
            gameNameInput.text = "";
        }

        bool isProfileSelected = currentSelectedProfileButton != null;
        if (!isProfileSelected) profileErrorText.text = "ͷ����Ϊ�գ���ѡ��ͷ��";

        if (isGameNameValid && isProfileSelected)
        {
            PlayerPrefs.SetString($"GameName_{clickedArchiveSlotIndex}", gameName);
            PlayerPrefs.SetInt($"SelectedProfileIndex_{clickedArchiveSlotIndex}", selectedProfileIndex);
            PlayerPrefs.SetInt($"ArchiveSlot{clickedArchiveSlotIndex}_Used", 1);
            PlayerPrefs.SetInt("LastUsedArchiveSlot", clickedArchiveSlotIndex);
            PlayerPrefs.Save();

            if (archiveEnterGameText.Length > clickedArchiveSlotIndex)
                archiveEnterGameText[clickedArchiveSlotIndex].text = "������Ϸ";

            SceneManager.LoadScene("LevelList");
        }
    }
}