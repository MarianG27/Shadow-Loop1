using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DoorScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public CanvasGroup fadeGroup;
    public GameObject endMenu;
    public Button continueButton;
    public Button resetButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;

    void Awake()
    {
        panel.SetActive(false);
        endMenu.SetActive(false);
    }

    public IEnumerator PlayStartSequence()
    {
        panel.SetActive(true);
        yield return FadeIn();

        PlayDoorSounds();

        yield return new WaitForSeconds(0.5f);
        yield return FadeOut();

        panel.SetActive(false);
    }

    public IEnumerator PlayExitSequence()
    {
        panel.SetActive(true);
        yield return FadeIn();

        PlayDoorSounds();
    }

    public IEnumerator ShowRoundEndMenu(GameManager gm)
    {
        panel.SetActive(true);
        yield return FadeIn();

        endMenu.SetActive(true);
        continueButton.onClick.RemoveAllListeners();
        resetButton.onClick.RemoveAllListeners();

        continueButton.onClick.AddListener(() => gm.ContinueRound());
        resetButton.onClick.AddListener(() => gm.RestartGame());

        yield return null;
    }

    public IEnumerator PlayContinueSequence()
    {
        endMenu.SetActive(false);
        PlayDoorSounds();
        yield return FadeOut();
        panel.SetActive(false);
    }

    private void PlayDoorSounds()
    {
        if (audioSource && doorOpenSound && doorCloseSound)
        {
            audioSource.PlayOneShot(doorOpenSound);
            audioSource.PlayOneShot(doorCloseSound);
        }
    }

    private IEnumerator FadeIn()
    {
        fadeGroup.alpha = 0f;
        float t = 0f;
        while (t < fadeDuration)
        {
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        fadeGroup.alpha = 1f;
        float t = 0f;
        while (t < fadeDuration)
        {
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }
}
