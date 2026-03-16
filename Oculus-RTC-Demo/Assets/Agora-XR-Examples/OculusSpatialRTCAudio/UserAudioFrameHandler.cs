using System;
using UnityEngine;
using Agora.Rtc;
using RingBuffer;

/// <summary>
///  This class is a MonoBehaviour that handles the audio frame for a specific user.
/// </summary>
public class UserAudioFrameHandler : MonoBehaviour {

	AudioSource audioSource;
    uint UID { get; set; }

    private int CHANNEL = 1;
    private int PULL_FREQ_PER_SEC = 100;
    private int CLIP_SAMPLES = 480;
    private int SAMPLE_RATE = 48000; // = CLIP_SAMPLES x PULL_FREQ_PER_SEC

    private int count;

    private RingBuffer<float> audioBuffer;
    private AudioClip _audioClip = null;

    private bool _startSignal;


    // Use this for initialization (runs after Init)
    void Start () {
		audioSource = GetComponent<AudioSource>();	
        if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
	    }
    }

    private void OnDisable()
    {
        ResetHandler();
    }

    public void Init(uint uid, AudioFrame audioFrame)
    {
        // Debug.Log("INIT:" + uid + " audioFrame:" + audioFrame);
        UID = uid;
        CLIP_SAMPLES = audioFrame.WordCount();
        SAMPLE_RATE = audioFrame.samplesPerSec;
        CHANNEL = audioFrame.channels;
        SetupAudio(audioSource, "clip_for_" + UID);
    }

    void SetupAudio(AudioSource aud, string clipName)
    {
        if (_audioClip != null) return; 
        var bufferLength = SAMPLE_RATE / PULL_FREQ_PER_SEC * CHANNEL * 1000; // 10-sec-length buffer
        audioBuffer = new RingBuffer<float>(bufferLength);
        // Debug.Log($"{UID} Created clip for SAMPLE_RATE:" + SAMPLE_RATE + " CLIP_SAMPLES:" + CLIP_SAMPLES + " channel:" + CHANNEL + " => bufferLength = " + bufferLength);
        _audioClip = AudioClip.Create(clipName,
            CLIP_SAMPLES,
            CHANNEL, SAMPLE_RATE, true,
            OnAudioRead);
        aud.clip = _audioClip;
        aud.loop = true;
        aud.Play();
    }

    void ResetHandler()
    {
        _startSignal = false;
        if (audioBuffer != null)
        {
            audioBuffer.Clear();
        }
        count = 0;
    }

    private void OnApplicationPause(bool pause)
    {
        Debug.LogWarning("Pausing application");
        if (pause)
        {
            ResetHandler();
        }
        else
        {
            Debug.Log("Application resumed.");
        }
    }

    internal void HandleAudioFrame(uint uid, AudioFrame audioFrame)
    {
        // Debug.Log("HandleAudioFrame:" + uid + " audioFrame:" + audioFrame);
        if (UID != uid || audioBuffer == null) return;

        var floatArray = ConvertByteToFloat16(audioFrame.RawBuffer);
        lock (audioBuffer)
        {
            audioBuffer.Put(floatArray);
            count++;
        }

        if (count == 100)
        {
            _startSignal = true;
        }
    }

    /// <summary>
    /// This method is called by the Unity engine to fill the audio buffer.
    /// </summary>
    /// <param name="data"></param>
    private void OnAudioRead(float[] data)
    {
        if (!_startSignal) return;
        lock (audioBuffer) {
            for (var i = 0; i < data.Length; i++)
            {
                if (audioBuffer.Count > 0)
                {
                    data[i] = audioBuffer.Get();
                }
                else
                {
                    data[i] = 0;
                }
            }
        }

    }

    private static float[] ConvertByteToFloat16(byte[] byteArray)
    {
        var floatArray = new float[byteArray.Length / 2];
        for (var i = 0; i < floatArray.Length; i++)
        {
            floatArray[i] = BitConverter.ToInt16(byteArray, i * 2) / 32768f; // -Int16.MinValue
        }

        return floatArray;
    }
}

static class AudioFrameExtension
{
    public static int WordCount(this AudioFrame audioFrame)
    {
        return audioFrame.samplesPerChannel * audioFrame.channels;
    }

    public static String ToString(this AudioFrame audioFrame)
    {
        return $"AudioFrame: type={audioFrame.type} samplesPerChannel={audioFrame.samplesPerChannel} bytesPerSample={audioFrame.bytesPerSample} channels={audioFrame.channels} samplesPerSec={audioFrame.samplesPerSec} WordCount={audioFrame.WordCount()}";
    }
}