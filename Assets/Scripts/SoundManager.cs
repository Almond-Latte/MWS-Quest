using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

public class SoundManager : MonoBehaviour
{
    [Header("ダイアログの有無")]
    public bool hasDialogue;
    [Header("SE Name: ダイアログ送り")]
    [Tooltip("拡張子を除いたものを指定")]
    public string dialogueForwardSound;
    [Header("SE Name: 決定音")]
    [Tooltip("拡張子を除いたものを指定")]
    public string decisionSound;
    [Header("SE Name: キャンセル音")]
    [Tooltip("拡張子を除いたものを指定")]
    public string cancelSound;
    [Header("SE Name: 歩行音")]
    [Tooltip("拡張子を除いたものを指定")]
    public string footStep;
    [Header("BGM Name:メインテーマ")]
    [Tooltip("拡張子を除いたものを指定")]
    public string bgm;
    [Header("BGM音量")]
    public float bgmVolume = 0.05f;
    [Header("SE音量")]
    public float seVolume = 0.2f;
    
    private AudioSource _soundEffectSource;
    private AudioSource _footStepSource;
    private AudioSource _bgmSource;
    private DialogueRunner _dialogueRunner;
    
    // SEのキャッシュ
    private readonly Dictionary<string, AudioClip> _seCache = new Dictionary<string, AudioClip>();
    // BGMのキャッシュ
    private readonly Dictionary<string, AudioClip> _bgmCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (audioSources.Length != 3)
        {
            Debug.LogError("AudioSourceの数が3つではありません");
            return;
        }
        _soundEffectSource = audioSources[0];
        _footStepSource = audioSources[1];
        _bgmSource = audioSources[2];
    }
    
    private void Start()
    {
        SetSEVolume(seVolume);
        SetBGMVolume(bgmVolume);
        
        if (hasDialogue)
        {
            _dialogueRunner = FindObjectOfType<DialogueRunner>();
            _dialogueRunner.onDialogueStart.AddListener(() => { PlaySE(decisionSound); });
        }
    }

    public void PlaySE(string seName)
    {
        if (!_seCache.ContainsKey(seName))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Sounds/SE/{seName}");
            if (!clip) // clipがnullの場合
            {
                Debug.LogError($"SE not found: {seName}");
                return;
            }
            _seCache.Add(seName, clip);
        }
        // SE再生
        _soundEffectSource.PlayOneShot(_seCache[seName]);
    }
    
    public void PlayFootStep()
    {
        if (_footStepSource.isPlaying)
        {
            return;
        }
        if (!_seCache.ContainsKey(footStep))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Sounds/SE/{footStep}");
            if (!clip) // clipがnullの場合
            {
                Debug.LogError($"SE not found: {footStep}");
                return;
            }
            _seCache.Add(footStep, clip);
        }
        // 再生
        _footStepSource.clip = _seCache[footStep];
        _footStepSource.loop = true;
        _footStepSource.Play();
        
    }

    public IEnumerator StopFootStep()
    {
        if (!_footStepSource.isPlaying)
        {
            yield break;
        }
        // フェードアウト
        yield return StartCoroutine(FadeOutFootStep(0.2f));
        // ボリュームを元に戻しておく
        _footStepSource.volume = seVolume;
    }


    private IEnumerator FadeInFootStep(float fadeInTime)
    {
        float volume = _footStepSource.volume;
        _footStepSource.volume = 0;
        _footStepSource.Play();
        float elapsedTime = 0f;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            _footStepSource.volume = Mathf.Lerp(0, volume, elapsedTime / fadeInTime);
            yield return null;
        }
    }
    private IEnumerator FadeOutFootStep(float fadeOutTime)
    {
        float elapsedTime = 0f;
        float volume = _footStepSource.volume;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            _footStepSource.volume = Mathf.Lerp(volume, 0, elapsedTime / fadeOutTime);
            yield return null;
        }
        _footStepSource.Stop();
    }
    
    public void PlayBGM(string bgmName, float fadeInTime=3.0f, bool resume=false)
    {
        // 一時停止解除
        // すでに再生中のBGMと同じ場合は一時停止を解除して音量をフェードイン
        if (resume && _bgmSource.clip && _bgmSource.clip.name == bgmName)
        {
            _bgmSource.UnPause();
            StartCoroutine(FadeInBGM(fadeInTime));
            return;
        }
        
        if (!_bgmCache.ContainsKey(bgmName))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Sounds/BGM/{bgmName}");
            if (!clip)
            {
                Debug.LogError($"BGM not found: {bgmName}");
                return;
            }
            _bgmCache.Add(bgmName, clip);
        }
        
        // BGM再生
        _bgmSource.clip = _bgmCache[bgmName];
        _bgmSource.loop = true;
        _bgmSource.Play();
        StartCoroutine(FadeInBGM(fadeInTime));
    }
    
    private IEnumerator FadeInBGM(float fadeInTime = 0.0f)
    {
        float elapsedTime = 0f;
        float volume = _bgmSource.volume;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0, volume, elapsedTime / fadeInTime);
            yield return null;
        }
        _bgmSource.volume = bgmVolume;
    }
    
    public IEnumerator StopBGM(float fadeOutTime = 0.0f, bool pause=false)
    {
        yield return StartCoroutine(FadeOutBGM(fadeOutTime, pause));
        // ボリュームを元に戻しておく
        _bgmSource.volume = bgmVolume;
    }
    
    private IEnumerator FadeOutBGM(float fadeOutTime, bool pause)
    {
        float elapsedTime = 0f;
        float volume = _bgmSource.volume;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(volume, 0, elapsedTime / fadeOutTime);
            yield return null;
        }
        if (pause)
        {
            _bgmSource.Pause();
        }
        else
        {
            _bgmSource.Stop();
        }
    }
    
    /// <summary>
    /// BGMのボリュームを設定
    /// </summary>
    /// <param name="volume"></param>
    public void SetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
        bgmVolume = volume;
    }
    
    /// <summary>
    /// SEのボリュームを設定
    /// </summary>
    /// <param name="volume"></param>
    public void SetSEVolume(float volume)
    {
        _soundEffectSource.volume = volume;
        _footStepSource.volume = volume;
        seVolume = volume;
    }
    
    
}
