using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelLoadingManager : MonoBehaviour
{
    public static LevelLoadingManager Instance;

    [Header("������")]
    public Slider loadingBar;
    public Text loadingValueText;

    [Header("��������")]
    public Animator loadingAnimator;
    public AnimationClip fadeOutAnimation;

    private string targetLevelScene;
    private bool isAnimationTriggered = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        targetLevelScene = PlayerPrefs.GetString("TargetLevelScene", "Level01");
        StartCoroutine(LoadLevelProcess());
    }

    private IEnumerator LoadLevelProcess()
    {
        loadingBar.value = 0;
        loadingValueText.text = "0%";
        
        yield return StartCoroutine(SimulateLoading()); //ģ����ع���

        TriggerFadeOutAnimation(); //������ɺ󴥷�����

        yield return StartCoroutine(WaitForAnimation()); //�ȴ������������

        SceneManager.LoadScene(targetLevelScene); //����������ɺ���ת�ؿ�
        Destroy(gameObject);
    }
    private IEnumerator SimulateLoading()
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.25f; //�������ص��ٶ�
            loadingBar.value = progress;
            loadingValueText.text = $"{Mathf.FloorToInt(progress * 100)}%";
            yield return null;
        }
        loadingBar.value = 1f; //ȷ����ȷ���ص�100%
        loadingValueText.text = "100%";
    }

    private void TriggerFadeOutAnimation()
    {
        if (isAnimationTriggered || loadingAnimator == null || fadeOutAnimation == null) return;

        loadingAnimator.SetTrigger("FadeOut");
        isAnimationTriggered = true;
    }

    private IEnumerator WaitForAnimation()
    {
        if (loadingAnimator == null || fadeOutAnimation == null) yield break;

        while (!loadingAnimator.GetCurrentAnimatorStateInfo(0).IsName("LoadingLevel"))
        {
            yield return null;
        }

        while (loadingAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }
}
