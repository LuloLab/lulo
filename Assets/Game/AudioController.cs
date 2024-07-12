using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioClip levelComplete, clickButton, quadQuad, quadGround, quadWall, quadHome, quadIron;
    public bool soundOn;
    private AudioSource audioSource;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        soundOn = PlayerPrefs.GetInt("soundOn", 1) == 1;
        audioSource.mute = !soundOn;
    }
    public void ToggleSound()
    {
        soundOn = !soundOn;
        PlayerPrefs.SetInt("soundOn", soundOn ? 1 : 0);
        PlayerPrefs.Save();
        audioSource.mute = !soundOn;
    }

    public void LevelComplete()
    {
        audioSource.PlayOneShot(levelComplete);
    }
    public void ClickButton()
    {
        audioSource.PlayOneShot(clickButton);
    }
    public void QuadQuad()
    {
        audioSource.PlayOneShot(quadQuad);
    }
    public void QuadIron()
    {
        audioSource.PlayOneShot(quadIron);
    }
    public void QuadWall()
    {
        audioSource.PlayOneShot(quadWall);
    }
    public void QuadHome()
    {
        audioSource.PlayOneShot(quadHome);
    }
    public void QuadGround(float scale)
    {
        audioSource.PlayOneShot(quadGround, scale);
    }
}
