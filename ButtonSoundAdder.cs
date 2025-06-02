using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonSoundAdder : MonoBehaviour
{
    private void Start()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);

        foreach(Button button in allButtons)
        {
            button.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlayClickSound();
            }); 
        }
    }
}
