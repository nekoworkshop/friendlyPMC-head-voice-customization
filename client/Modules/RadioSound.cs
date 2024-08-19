using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class RadioSound : MonoBehaviour
{
    private AudioSource audioSourceRadio;
    private AudioSource audioSourceLocation;
    private AudioClip radioSounnd;

    private AudioClip locationPing;

    public void Enable()
    {
        audioSourceRadio = gameObject.AddComponent<AudioSource>();
        audioSourceLocation = gameObject.AddComponent<AudioSource>();

        string dllPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        string soundFilePath = Path.Combine(dllPath, "friendlyPMC", "RadioChat.wav");
        StartCoroutine(LoadRadioAudioClip(soundFilePath));

        soundFilePath = Path.Combine(dllPath, "friendlyPMC", "locationPing.wav");
        StartCoroutine(LoadLocationAudioClip(soundFilePath));
    }

    private IEnumerator LoadRadioAudioClip(string filePath)
    {
        if (File.Exists(filePath))
        {
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, AudioType.WAV))
            {
                yield return req.SendWebRequest();

                if (req.isNetworkError || req.isHttpError)
                {
                    Debug.LogError($"Failed to load audio file: {req.error}"); ;
                    yield break;
                }

                radioSounnd = DownloadHandlerAudioClip.GetContent(req);
                yield break;
            }
        }
        else
        {
            Debug.LogError($"Audio file not found: {filePath}");
        }
    }

    private IEnumerator LoadLocationAudioClip(string filePath)
    {
        if (File.Exists(filePath))
        {
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, AudioType.WAV))
            {
                yield return req.SendWebRequest();

                if (req.isNetworkError || req.isHttpError)
                {
                    Debug.LogError($"Failed to load audio file: {req.error}"); ;
                    yield break;
                }

                locationPing = DownloadHandlerAudioClip.GetContent(req);
                yield break;
            }
        }
        else
        {
            Debug.LogError($"Audio file not found: {filePath}");
        }
    }

    public void PlayRadioSound()
    {
        if (radioSounnd != null)
        {
            audioSourceRadio.panStereo = 0.0f;
            audioSourceRadio.volume = 0.5f * (friendlyPMC.friendlyPMC.statusSound.Value / 100);
            audioSourceRadio.PlayOneShot(radioSounnd);
        }
    }

    public void PlayLocationSound(float stereoPan)
    {
        if (locationPing != null) {
            audioSourceLocation.panStereo = stereoPan;
            audioSourceLocation.volume =  0.17f * (friendlyPMC.friendlyPMC.statusSound.Value / 100);
            audioSourceLocation.PlayOneShot(locationPing);
        }
    }
}
