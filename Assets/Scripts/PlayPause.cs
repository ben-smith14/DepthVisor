using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayPause : MonoBehaviour
{
    // All icons by Smashicons on flaticon.com
    [SerializeField] Sprite PlaySprite;
    [SerializeField] Sprite PauseSprite;

    private bool IsPlaying = false;

    public void PlayPauseRecording()
    {
        Debug.Log("Button clicked");

        if (IsPlaying)
        {
            IsPlaying = false;
            gameObject.GetComponent<Image>().sprite = PlaySprite;
        } else
        {
            IsPlaying = true;
            gameObject.GetComponent<Image>().sprite = PauseSprite;
        }
    }
}
