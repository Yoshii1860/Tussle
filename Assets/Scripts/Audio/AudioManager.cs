using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class SoundEffect
{
    public string name;
    public AudioClip clip;
    [Range(0.5f, 2f)] public float pitchMin = 1f;
    [Range(0.5f, 2f)] public float pitchMax = 1f;
    [Range(0f, 1f)] public float volumeMin = 1f;
    [Range(0f, 1f)] public float volumeMax = 1f;
}

[System.Serializable]
public class AreaMusic
{
    public string areaName;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip menuMusic;
    public List<AreaMusic> areaMusic;

    [Header("SFX")]
    public AudioSource sfxSource;
    public List<SoundEffect> soundEffects;

    private Dictionary<string, AudioClip> sfxDict = new();

    private const string MeinMenuSceneName = "MainMenu";
    private const string GameSceneName = "Game";

    private Coroutine fadeCoroutine;

    private void Awake()
    {

#if UNITY_SERVER
        Destroy(gameObject);
        return;
#endif


        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var sfx in soundEffects)
            if (!sfxDict.ContainsKey(sfx.name))
                sfxDict.Add(sfx.name, sfx.clip);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll(typeof(Button)))
        {
            var button = obj as Button;
            if (button != null)
                button.onClick.AddListener(PlayClickSFX);
        }

        if (scene.name == MeinMenuSceneName)
        {
            PlayMenuMusic();
        }
        else if (scene.name == GameSceneName)
        {
            PlayRandomAreaMusic();
        }
    }

    // --- MUSIC ---
    public void PlayMenuMusic()
    {
        if (menuMusic != null)
            PlayMusic(menuMusic);
    }

    public void PlayAreaMusic(string area)
    {
        Debug.Log($"Playing area music for: {area}");
        var found = areaMusic.Find(a => a.areaName == area);
        if (found != null && found.clip != null)
        {
            Debug.Log($"Found music for area '{area}': {found.clip.name}");
            if (musicSource.isPlaying && musicSource.clip == found.clip)
                return; // Already playing this music
            if (musicSource.isPlaying)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }

                Debug.Log($"Fading out current music: {musicSource.clip.name}");
                fadeCoroutine = StartCoroutine(FadeOutMusic(found.clip, 1f));
            }
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlayRandomAreaMusic()
    {
        if (areaMusic.Count == 0) return;
        int randomIndex = Random.Range(0, areaMusic.Count);
        AudioClip randomClip = areaMusic[randomIndex].clip;
        if (randomClip != null)
        {
            if (musicSource.isPlaying && musicSource.clip == randomClip)
                return;
            if (musicSource.isPlaying)
            { 
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }
                Debug.Log($"Fading out current music: {musicSource.clip.name}");
                fadeCoroutine = StartCoroutine(FadeOutMusic(randomClip, 1f));
            }
            else
            {
                musicSource.clip = randomClip;
                musicSource.loop = true;
                musicSource.Play();
            }
        }
    }
                

    // --- SFX ---
    public void PlaySFX(string name)
    {
        var sfx = soundEffects.Find(s => s.name == name);
        if (sfx != null && sfx.clip != null)
        {
            float pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            float volume = Random.Range(sfx.volumeMin, sfx.volumeMax);
            sfxSource.pitch = pitch;
            sfxSource.volume = volume;
            sfxSource.PlayOneShot(sfx.clip, volume);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        var sfx = soundEffects.Find(s => s.clip == clip);
        if (sfx != null)
        {
            float pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            float volume = Random.Range(sfx.volumeMin, sfx.volumeMax);
            sfxSource.pitch = pitch;
            sfxSource.volume = volume;
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayRandomSFX(string name)
    {
        var matches = soundEffects.FindAll(s => s.name.StartsWith(name));
        if (matches.Count > 0)
        {
            var sfx = matches[Random.Range(0, matches.Count)];
            float pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            float volume = Random.Range(sfx.volumeMin, sfx.volumeMax);
            sfxSource.pitch = pitch;
            sfxSource.volume = volume;
            sfxSource.PlayOneShot(sfx.clip, volume);
        }
       
    }

    public void PlaySFXAtPosition(string name, Vector3 position, float volume = 1f)
    {
        if (sfxDict.TryGetValue(name, out var clip) && clip != null)
            AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // --- UI ---

    public void PlayClickSFX()
    {
        PlaySFX("Click");
    }

    public void PlayCoinSFX()
    {
        PlaySFX("Coin");
    }

    public void PlayPotionSFX()
    {
        PlaySFX("Potion");
    }

    public void PlayDoorSFX()
    {
        PlaySFX("Door");
    }

    // --- Utils ---

    private IEnumerator FadeOutMusic(AudioClip clip, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = 0f; // Reset volume after fade out
        StartCoroutine(FadeInMusic(clip, duration, startVolume));
    }

    private IEnumerator FadeInMusic(AudioClip clip, float duration, float volume)
    {
        musicSource.clip = clip;
        musicSource.Play();
        musicSource.volume = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, volume, elapsed / duration);
            yield return null;
        }
        musicSource.volume = volume; // Ensure volume is set to full after fade in
    }
}