using System.IO;
using System.Reflection;

/// <summary>
/// Manages the storage and loading of audio clips for playback.
/// </summary>
public class AudioClipStorage
{
    /// <summary>
    /// Dictionary containing all loaded audio clips, indexed by their names.
    /// </summary>
    public static Dictionary<string, AudioClipData> AudioClips { get; } = new Dictionary<string, AudioClipData>();

    /// <summary>
    /// Loads an audio clip from raw byte data and registers it under the given name.
    /// </summary>
    /// <param name="rawData">
    /// The raw byte array containing the audio data in Ogg Vorbis format.
    /// </param>
    /// <param name="name">
    /// The unique name to assign to the loaded audio clip.
    /// </param>
    /// <returns>
    /// <c>true</c> if the clip was successfully loaded and registered;  
    /// <c>false</c> if the raw data is invalid or a clip with the same name already exists.
    /// </returns>
    /// <remarks>
    /// - Logs errors if the raw data is null/empty or if a duplicate name is used.  
    /// - Uses <see cref="VorbisReader"/> to decode the Ogg Vorbis stream.  
    /// - The decoded clip is stored in the <c>AudioClips</c> dictionary as an <see cref="AudioClipData"/>.
    /// </remarks>
    public static bool LoadClip(byte[] rawData, string name)
    {
        // Ensure raw data is not null or empty.
        if (rawData == null || rawData.Length == 0)
        {
            ServerConsole.AddLog("[AudioPlayer] Failed loading clip because raw data is null or empty!");
            return false;
        }

        // Ensure no clip with the same name is already loaded.
        if (AudioClips.ContainsKey(name))
        {
            ServerConsole.AddLog($"[AudioPlayer] Failed loading clip because clip with {name} is already loaded!");
            return false;
        }

        float[] samples = null;
        int sampleRate = 0;
        int channels = 0;

        using (MemoryStream ms = new MemoryStream(rawData))
        {
            using (VorbisReader reader = new VorbisReader(ms))
            {
                sampleRate = reader.SampleRate;
                channels = reader.Channels;

                samples = new float[reader.TotalSamples * channels];
                reader.ReadSamples(samples);
            }
        }

        // Create a new AudioClipData instance with default values.
        AudioClips.Add(name, new AudioClipData(name, sampleRate, channels, samples));
        return true;
    }


    /// <summary>
    /// Destroys loaded clips.
    /// </summary>
    /// <param name="name">Then name of clip.</param>
    /// <returns>If clip was successfully destroyed.</returns>
    public static bool DestroyClip(string name)
    {
        if (!AudioClips.ContainsKey(name))
        {
            ServerConsole.AddLog($"[AudioPlayer] Clip with name {name} is not loaded!");
            return false;
        }

        return AudioClips.Remove(name);
    }
}