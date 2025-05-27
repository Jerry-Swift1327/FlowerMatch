using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Text;
using System.Collections;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;
    public Text networkStatusText;
    public bool IsConnected { get; private set; }
    private bool isSceneTransitioning;
    private float reconnectTimer;
    private const float reconnectInterval = 5f;

    // 事件：账号创建成功或失败、错误提示
    public static Action OnAccountCreated;
    public static Action OnLoginSuccess;
    public static Action<string> OnAccountCreationFailed;
    public static Action<string> OnErrorOccurred;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    //注册账号
    public void CreateAccount(string account, string password)
    {
        if (isSceneTransitioning) return;

        //验证格式
        if (!ValidateCredentials(account, password))
        {
            OnErrorOccurred?.Invoke("账号或密码格式不符合规则（账号3-10位字母数字下划线，密码8-15位字母数字）");
            return;
        }

        //检查账号是否已存在（本地存储）
        if (PlayerPrefs.HasKey(account))
        {
            OnAccountCreationFailed?.Invoke("账号已存在,请尝试其他账号！");
            return;
        }

        //计算密码哈希并本地存储
        string passwordHash = HashPassword(password);
        PlayerPrefs.SetString(account, passwordHash);
        PlayerPrefs.Save();

        //触发创建成功事件
        OnAccountCreated?.Invoke();
        StartCoroutine(DelaySceneJump());
    }

    //登录验证
    public void Login(string account, string password)
    {
        //检查账号是否存在
        if (!PlayerPrefs.HasKey(account))
        {
            OnErrorOccurred?.Invoke("账号不存在，请先创建账号！");
            return;
        }

        //验证密码哈希
        string storedHash = PlayerPrefs.GetString(account);
        string inputHash = HashPassword(password);

        if (storedHash == inputHash)
        {
            OnLoginSuccess?.Invoke();
            StartCoroutine(DelaySceneJump("LevelList")); // 登录成功，跳转关卡列表
        }
        else
        {
            OnErrorOccurred?.Invoke("账号或密码密码错误，请重试！");
        }
    }

    //延迟跳转场景
    private IEnumerator DelaySceneJump(string sceneName = "LevelList")
    {
        yield return new WaitForSeconds(1f);
        isSceneTransitioning = true;
        SceneManager.LoadScene(sceneName);
    }

    //密码哈希
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "YOUR_SALT_VALUE"));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    //验证账号密码格式
    private bool ValidateCredentials(string account, string password)
    {
        return Regex.IsMatch(account, @"^[a-zA-Z0-9_]{3,10}$") &&
               Regex.IsMatch(password, @"^[a-zA-Z0-9]{8,15}$");
    }

    //Photon网络回调
    public override void OnConnectedToMaster()
    {
        IsConnected = true;
        UpdateNetworkStatus("网络连接成功");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        IsConnected = false;
        UpdateNetworkStatus($"网络断开（原因：{cause}）");
        reconnectTimer = 0;
    }

    private void Update()
    {
        if (!IsConnected)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= reconnectInterval)
            {
                PhotonNetwork.ConnectUsingSettings();
                reconnectTimer = 0;
            }
        }
    }

    public void UpdateNetworkStatus(string message)
    {
        if (networkStatusText != null) networkStatusText.text = message;
    }
}