using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the playback of audio clips from embedded PCM data, including looping, volume control, and mixing.
/// </summary>
public class AudioClipPlayback : IDisposable
{
    public const int PacketSize = 480;        // Number of samples per audio packet
    public const int SamplingRate = 48000;    // Playback sample rate
    public const int Channels = 1;            // Number of channels (mono)

    public int Id { get; }
    public string Clip { get; }
    public bool Loop { get; set; }
    public bool DestroyOnEnd { get; }
    public bool IsPaused { get; set; }
    public float Volume { get; set; } = 1f;

    public int ReadPosition { get; set; }       // Current playback position
    public float[] NextSample { get; private set; }

    /// <summary>
    /// Initializes a new instance of AudioClipPlayback.
    /// </summary>
    public AudioClipPlayback(int id, string clip, float volume = 1f, bool loop = false, bool destroyOnEnd = true)
    {
        Id = id;
        Clip = clip;
        Volume = volume;
        Loop = loop;
        DestroyOnEnd = destroyOnEnd;
    }

    /// <summary>
    /// Gets the PCM samples of the audio clip.
    /// </summary>
    public float[] Samples
    {
        get
        {
            if (string.IsNullOrEmpty(Clip))
                return Array.Empty<float>();

            if (!AudioClipStorage.AudioClips.TryGetValue(Clip, out AudioClipData data))
                return Array.Empty<float>();

            return data.Samples;
        }
    }

    /// <summary>
    /// Gets the total duration of the audio clip.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples.Length / (SamplingRate * Channels));

    /// <summary>
    /// Gets the current playback position as a time value.
    /// </summary>
    public TimeSpan CurrentTime => TimeSpan.FromSeconds((double)ReadPosition / (SamplingRate * Channels));

    /// <summary>
    /// Gets the current playback progress as a percentage.
    /// </summary>
    public float Progress => Mathf.Clamp01((float)ReadPosition / Samples.Length);

    /// <summary>
    /// Prepares the next chunk of PCM samples for playback.
    /// </summary>
    /// <returns>True if a chunk was prepared, false if playback ended.</returns>
    public bool PrepareSample()
    {
        bool destroy = false;
        NextSample = ReadPcmChunk(ref destroy);
        return !destroy;
    }

    /// <summary>
    /// Reads a chunk of PCM data from the clip.
    /// </summary>
    private float[] ReadPcmChunk(ref bool destroy)
    {
        if (IsPaused || Samples.Length == 0)
            return null;

        if (ReadPosition >= Samples.Length)
        {
            if (Loop)
                ReadPosition = 0;
            else
            {
                destroy = true;
                return null;
            }
        }

        int samplesToSend = Math.Min(PacketSize, Samples.Length - ReadPosition);
        float[] chunk = new float[samplesToSend];
        Array.Copy(Samples, ReadPosition, chunk, 0, samplesToSend);
        ReadPosition += samplesToSend;

        return PadPCMFloat(chunk, PacketSize);
    }

    /// <summary>
    /// Pads a PCM buffer to a fixed size.
    /// </summary>
    private static float[] PadPCMFloat(float[] pcmBuffer, int targetLength)
    {
        if (pcmBuffer.Length >= targetLength)
            return pcmBuffer;

        float[] padded = new float[targetLength];
        Array.Copy(pcmBuffer, padded, pcmBuffer.Length);
        return padded;
    }

    private static float[] _mixedData = new float[PacketSize];

    /// <summary>
    /// Mixes multiple playbacks into a single PCM buffer.
    /// </summary>
    public static float[] MixPlaybacks(AudioClipPlayback[] playbacks, ref List<int> clipsToDestroy)
    {
        bool allEmpty = true;

        foreach (var playback in playbacks)
        {
            if (!playback.PrepareSample())
                clipsToDestroy.Add(playback.Id);
        }

        for (int i = 0; i < PacketSize; i++)
        {
            float mixedSample = 0;
            foreach (var playback in playbacks)
            {
                if (playback.NextSample == null) continue;
                mixedSample += playback.NextSample[i] * playback.Volume;
                allEmpty = false;
            }

            _mixedData[i] = Mathf.Clamp(mixedSample, -1f, 1f);
        }

        return allEmpty ? null : _mixedData;
    }

    public void Dispose()
    {
        // Nothing to dispose for embedded PCM
    }
}
