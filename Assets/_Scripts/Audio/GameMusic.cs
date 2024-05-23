using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class GameMusic : MonoBehaviour
{
    public AudioClip[] musicClips;
    public AudioMixerGroup audioMixerGroup;
    public float pauseBetweenTracks = 240.0f; // ����� ����� ������� � ��������

    private List<int> playedIndices = new List<int>();

    void Start()
    {
        if (musicClips.Length == 0)
        {
            Debug.LogWarning("No music clips assigned!");
            return;
        }

        StartCoroutine(PlayRandomMusic());
    }

    IEnumerator PlayRandomMusic()
    {
        while (true)
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

            // ������� ��������� ������� ������ ��� ��������������� ����������
            GameObject tempAudioSource = new GameObject("TempAudioSource");
            AudioSource audioSource = tempAudioSource.AddComponent<AudioSource>();
            audioSource.clip = randomClip;
            audioSource.outputAudioMixerGroup = audioMixerGroup;
            audioSource.Play();

            Destroy(tempAudioSource, randomClip.length + pauseBetweenTracks); // ������� ������ ����� ��������� ������������ ����� � �����

            yield return new WaitForSeconds(randomClip.length + pauseBetweenTracks);
        }
    }
}
