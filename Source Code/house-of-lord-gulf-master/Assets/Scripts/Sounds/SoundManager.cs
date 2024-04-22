using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager manager;

    public float defaultVolume;

    [Space]
    //THIS CONTAINS SOUND FOR CANDY
    public SoundContainerForCandy SoundContainerForCandy;

    [Header("BG Sounds")]
    [SerializeField] AudioSource backgroundMusicAudioSource;

    [Header("UI Sounds")]
    [SerializeField] AudioSource buttonClickAudioSource;

    //THIS CONTAINS AUDIO SOURCE SO THAT WE DON:T HAVE TO CREATE THAT RUNTIME
    [Header("List Of AudioSources")]
    public List<AudioSource> CandyDestorySource;

    Dictionary<string, List<AudioClip>> audioGroups = new();

    private AudioSource AudioSourceTemp;

    int DestroySFXCount;

    private void Awake()
    {
        manager = this;
    }


    #region Play Sound Seperately

    /// <summary>
    /// Gets Audiosource form list 
    /// set's clip and play
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySoundSeperately(AudioClip clip)
    {
        AudioSourceTemp = GetEmptyAudioSource();
        if (AudioSourceTemp == null)
        {
            PlayWhenAudioSourceIsNull(clip, defaultVolume);
            return;
        }
        AudioSourceTemp.volume = defaultVolume;
        AudioSourceTemp.PlayOneShot(clip);
    }

    /// <summary>
    /// Audio For Candy Destruction
    /// </summary>
    public void PlaySoundForCandyDestructionSapratly()
    {
        AudioSourceTemp = GetEmptyAudioSource();
        if (AudioSourceTemp == null)
        {
            PlayWhenAudioSourceIsNull(SoundContainerForCandy.destroyCandySFXs[Random.Range(0, DestroySFXCount)], defaultVolume);
            return;
        }
        AudioSourceTemp.volume = defaultVolume;
        AudioSourceTemp.PlayOneShot(SoundContainerForCandy.destroyCandySFXs[Random.Range(0, DestroySFXCount)]);
    }

    /// <summary>
    /// Just Check if all audiosources are playing
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="volume"></param>
    void PlayWhenAudioSourceIsNull(AudioClip clip, float volume)
    {
#if !UNITY_SERVER
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);
        Destroy(audioSource, clip.length);
#endif
    }


    AudioSource GetEmptyAudioSource()
    {
#if !UNITY_SERVER
        foreach (var item in CandyDestorySource)
        {
            if (!item.isPlaying)
            {
                return item;
            }
        }
#endif

        return null;
    }

    /// <summary>
    /// Gets Audiosource
    /// Sets Volume
    /// Plays Clip
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="volume"></param>
    public void PlaySoundSeperately(AudioClip clip, float volume)
    {

        AudioSourceTemp = GetEmptyAudioSource();
        if (AudioSourceTemp == null)
        {
            PlayWhenAudioSourceIsNull(clip, volume);
            return;
        }
        AudioSourceTemp.volume = volume;
        AudioSourceTemp.PlayOneShot(clip);
    }

#endregion

    #region Play Sound Seperately In Group

    public void PlaySoundSeperatelyInGroup(AudioClip clip, string id)
    {
        if (audioGroups.ContainsKey(id))
            audioGroups[id].Add(clip);
        else
        {
            audioGroups.Add(id, new() { clip });
            StartCoroutine(Coroutine_PlayAudioGroup(id));
        }
    }

    IEnumerator Coroutine_PlayAudioGroup(string id)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = defaultVolume;

        while (audioGroups.ContainsKey(id) && audioGroups[id].Count > 0)
        {
            audioSource.PlayOneShot(audioGroups[id][0]);
            yield return new WaitForSeconds(audioGroups[id][0].length);
            audioGroups[id].RemoveAt(0);
        }

        audioGroups.Remove(id);
    }

    public void PlaySoundSeperatelyInGroup(AudioClip clip, float volume, string id)
    {
        if (audioGroups.ContainsKey(id))
            audioGroups[id].Add(clip);
        else
        {
            audioGroups.Add(id, new() { clip });
            StartCoroutine(Coroutine_PlayAudioGroup(id, volume));
        }
    }

    IEnumerator Coroutine_PlayAudioGroup(string id, float vol)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = vol;

        while (audioGroups.ContainsKey(id) && audioGroups[id].Count > 0)
        {
            audioSource.PlayOneShot(audioGroups[id][0]);
            yield return new WaitForSeconds(audioGroups[id][0].length);
            audioGroups[id].RemoveAt(0);
        }

        audioGroups.Remove(id);
    }

    #endregion

    #region Background Music

    public void PlayBackgroundMusic(AudioClip clip)
    {
        backgroundMusicAudioSource.volume = defaultVolume;
        backgroundMusicAudioSource.clip = clip;
        backgroundMusicAudioSource.loop = true;
        backgroundMusicAudioSource.Play();
    }

    public void StopBackgroundMusic()
    {
        backgroundMusicAudioSource.Stop();
    }

    public void ChangeBackgroundMusicVolume(float volume)
    {
        backgroundMusicAudioSource.volume = volume;
    }

    public void MuteBackgroundMusic(bool mute)
    {
        backgroundMusicAudioSource.volume = mute ? 0 : defaultVolume;
    }

    #endregion

    #region UI Button Click Sounds

    public void Play_ButtonClickSound()
    {
        buttonClickAudioSource.Play();
    }

    #endregion
}
