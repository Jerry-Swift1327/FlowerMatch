using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance;

    public AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
                
            musicSource.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Update()
    {
        if (Instance != this) Destroy(gameObject);
    }
}
