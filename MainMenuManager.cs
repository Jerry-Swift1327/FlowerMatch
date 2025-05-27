using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
    private bool isNetworkConnected = false;

    [Header("加载面板")]
    public GameObject loadingPanel;
    public Slider loadingBar;
    public Text loadingValueText;

    [Header("账号管理面板")]
    public GameObject accountManagementPanel;
    public Button createAccountButton;
    public Button loginAccountButton;

    [Header("创建账号面板")]
    public GameObject createAccountPanel;
    public InputField createAccountInput;
    public InputField createPasswordInput;
    public Text createErrorText;
    public Button createCompleteButton;
    public Button createCloseButton;

    [Header("登录账号面板")]
    public GameObject loginAccountPanel;
    public InputField loginAccountInput;
    public InputField loginPasswordInput;
    public Text loginErrorText;
    public Button loginCheckButton;
    public Button loginCloseButton;

    [Header("提示颜色")]
    public Color successColor = Color.green;
    public Color errorColor = Color.red;

    private float loadingProgress = 0.01f;
    private Coroutine errorDisplayCoroutine;
    private string actualLoginPassword = "";
    private string actualCreatePassword = "";

    private void Start()
    {
        Instance = this;
        SetPanelStates(true, false, false, false);
        BindEvents();
        StartCoroutine(LoadGame());
        PhotonManager.OnErrorOccurred += ShowError;
        PhotonManager.OnAccountCreated += HandleAccountCreated;
        PhotonManager.OnAccountCreationFailed += HandleAccountCreationFailed;
        PhotonManager.OnLoginSuccess += HandleLoginSuccess;
    }

    private void OnDestroy()
    {
        PhotonManager.OnErrorOccurred -= ShowError;
        PhotonManager.OnAccountCreated -= HandleAccountCreated;
        PhotonManager.OnAccountCreationFailed -= HandleAccountCreationFailed;
        PhotonManager.OnLoginSuccess -= HandleLoginSuccess;
    }

    private void BindEvents()
    {
        loginPasswordInput.onValueChanged.AddListener(OnLoginPasswordInputChanged);
        createPasswordInput.onValueChanged.AddListener(OnCreatePasswordInputChanged);

        createAccountButton.onClick.AddListener(() => ShowPanel(createAccountPanel));
        loginAccountButton.onClick.AddListener(() => ShowPanel(loginAccountPanel));
        createCompleteButton.onClick.AddListener(OnCreateAccountComplete);
        createCloseButton.onClick.AddListener(() => HidePanel(createAccountPanel));
        loginCheckButton.onClick.AddListener(OnLoginGameButtonClick);
        loginCloseButton.onClick.AddListener(() => HidePanel(loginAccountPanel));
    }

    //创建账号按钮点击
    private void OnCreateAccountComplete()
    {
        string account = createAccountInput.text;
        string password = actualCreatePassword;

        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            ShowError("账号或密码不能为空，请重试！");
            return;
        }

        PhotonManager.Instance.CreateAccount(account, password);
    }

    //登录按钮点击
    public void OnLoginGameButtonClick()
    {
        string account = loginAccountInput.text;
        string password = actualLoginPassword;

        if (string.IsNullOrEmpty(account)||string.IsNullOrEmpty(password))
        {
            ShowError("账号或密码不能为空，请重试！");
            return;
        }

        PhotonManager.Instance.Login(account, password);
    }

    //处理账号创建成功
    private void HandleAccountCreated()
    {
        ShowSuccessMessage("账号创建成功，即将进入游戏……", createErrorText);
        StartCoroutine(HidePanelAfterDelay(createAccountPanel, "LevelList"));
    }

    private void HandleLoginSuccess()
    {
        ShowSuccessMessage("账号登录成功，正在进入游戏……", loginErrorText);
        StartCoroutine(HidePanelAfterDelay(loginAccountPanel, "LevelList")); 
    }

    //处理账号创建失败
    private void HandleAccountCreationFailed(string error)
    {
        ShowError(error);
    }

    //显示成功提示
    private void ShowSuccessMessage(string message, Text targetText)
    {
        if (targetText != null)
        {
            targetText.color = successColor;
            targetText.text = message;
            targetText.gameObject.SetActive(true);
            errorDisplayCoroutine = StartCoroutine(HideSuccessAfterDelay(targetText));
        }
    }

    //延迟隐藏成功提示
    private IEnumerator HideSuccessAfterDelay(Text successText)
    {
        yield return new WaitForSeconds(1.5f);
        successText.gameObject.SetActive(false);
    }

    //延迟隐藏面板并跳转场景
    private IEnumerator HidePanelAfterDelay(GameObject panel, string sceneName)
    {
        yield return new WaitForSeconds(1.5f);
        HidePanel(panel);
        SceneManager.LoadScene(sceneName);
    }

    //显示错误提示
    public void ShowError(string message)
    {
        Text targetText = createAccountPanel.activeSelf ? createErrorText : loginErrorText;
        if (targetText != null)
        {
            targetText.color = errorColor;
            targetText.text = message;
            targetText.gameObject.SetActive(true);
            errorDisplayCoroutine = StartCoroutine(HideErrorAfterDelay(targetText));
        }
    }

    //延迟隐藏错误提示
    private IEnumerator HideErrorAfterDelay(Text errorText)
    {
        yield return new WaitForSeconds(2f);
        errorText.gameObject.SetActive(false);
    }

    //
    private void OnLoginPasswordInputChanged(string input)
    {
        if (input.Length > actualLoginPassword.Length)
            actualLoginPassword += input[input.Length - 1];
        else if (input.Length < actualLoginPassword.Length)
            actualLoginPassword = actualLoginPassword.Substring(0, input.Length);
        loginPasswordInput.text = new string('*', actualLoginPassword.Length);
    }

    private void OnCreatePasswordInputChanged(string input)
    {
        if (input.Length > actualCreatePassword.Length)
            actualCreatePassword += input[input.Length - 1];
        else if (input.Length < actualCreatePassword.Length)
            actualCreatePassword = actualCreatePassword.Substring(0, input.Length);
        createPasswordInput.text = new string('*', actualCreatePassword.Length);
    }

    private void SetPanelStates(bool loading, bool accountManagement, bool createAccount, bool login)
    {
        loadingPanel.SetActive(loading);
        accountManagementPanel.SetActive(accountManagement);
        createAccountPanel.SetActive(createAccount);
        loginAccountPanel.SetActive(login);
    }

    private void ShowPanel(GameObject panel) => panel.SetActive(true);
    private void HidePanel(GameObject panel) => panel.SetActive(false);

    //加载游戏
    private IEnumerator LoadGame()
    {
        PhotonManager.Instance.UpdateNetworkStatus("正在连接服务器……");
        yield return new WaitUntil(() => PhotonManager.Instance.IsConnected);
        PhotonManager.Instance.UpdateNetworkStatus("连接成功，正在加载……");
        while (loadingProgress < 1f)
        {
            loadingProgress += 0.01f;
            loadingBar.value = loadingProgress;
            loadingValueText.text = $"{Mathf.FloorToInt(loadingProgress * 100)}%";
            yield return new WaitForSeconds(0.035f);
        }
        SetPanelStates(false, true, false, false);
    }
}