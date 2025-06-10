using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioSource audioSource;
    public AudioClip clickSound;

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
        Button[] allButtons = FindObjectsOfType<Button>(true);

        foreach (Button button in allButtons)
        {
            button.onClick.AddListener(() =>
            {
                PlayClickSound();
            });
        }
    }

    public void PlayClickSound()
    {
        if(audioSource!=null&&clickSound!=null)
            audioSource.PlayOneShot(clickSound);
    }
}
