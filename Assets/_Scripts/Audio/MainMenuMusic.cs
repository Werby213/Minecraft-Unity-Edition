using UnityEngine;
using System.Collections.Generic;

public class MainMenuMusic : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] musicClips;
    private List<int> playedIndices = new List<int>();

    void Start()
    {
        if (musicClips.Length == 0)
        {
            Debug.LogWarning("No music clips assigned!");
            return;
        }

        PlayRandomMusic();
    }

    void PlayRandomMusic()
    {
        if (playedIndices.Count >= musicClips.Length)
        {
            playedIndices.Clear();
        }

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, musicClips.Length);
        } while (playedIndices.Contains(randomIndex));

        playedIndices.Add(randomIndex);
        AudioClip randomClip = musicClips[randomIndex];

        audioSource.clip = randomClip;
        audioSource.Play();
        Invoke(nameof(OnMusicFinished), randomClip.length);
    }

    void OnMusicFinished()
    {
        PlayRandomMusic();
    }
}
