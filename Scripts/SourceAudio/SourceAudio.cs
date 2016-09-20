using UnityEngine;

public class SourceAudio
{
    public static AudioClip LoadRawMP3(string clipName, byte[] rawData)
    {
        System.IO.MemoryStream mp3Stream = new System.IO.MemoryStream(rawData);
        NAudio.Wave.Mp3FileReader mp3File = new NAudio.Wave.Mp3FileReader(mp3Stream);
        NAudio.Wave.SampleProviders.Pcm32BitToSampleProvider sampler = new NAudio.Wave.SampleProviders.Pcm32BitToSampleProvider(mp3File);

        float[] floatBuffer = new float[mp3File.Length / 4];
        sampler.Read(floatBuffer, 0, floatBuffer.Length);

        mp3File.ReadNextFrame();
        //Debug.Log("AverageBytesPerSecond: " + (mp3File.WaveFormat.AverageBytesPerSecond / 4) + ", SampleRate: " + mp3File.WaveFormat.SampleRate + ", BitsPerSample: " + mp3File.WaveFormat.BitsPerSample + ", Channels: " + mp3File.WaveFormat.Channels);
        AudioClip loaded = AudioClip.Create(clipName, floatBuffer.Length, 1, mp3File.WaveFormat.AverageBytesPerSecond / 4, false);
        loaded.SetData(floatBuffer, 0);

        mp3Stream.Close();
        mp3File.Close();
        
        return loaded;
    }

    public static AudioClip LoadRawWAV(string clipName, byte[] rawData)
    {
        System.IO.MemoryStream wavStream = new System.IO.MemoryStream(rawData);
        NAudio.Wave.WaveFileReader wavFile = new NAudio.Wave.WaveFileReader(wavStream);
        NAudio.Wave.SampleProviders.Pcm32BitToSampleProvider sampler = new NAudio.Wave.SampleProviders.Pcm32BitToSampleProvider(wavFile);

        float[] floatBuffer = new float[wavFile.Length / 4];
        sampler.Read(floatBuffer, 0, floatBuffer.Length);

        //Debug.Log("AverageBytesPerSecond: " + (wavFile.WaveFormat.AverageBytesPerSecond / 4) + ", SampleRate: " + wavFile.WaveFormat.SampleRate + ", BitsPerSample: " + wavFile.WaveFormat.BitsPerSample + ", Channels: " + wavFile.WaveFormat.Channels);
        AudioClip loaded = AudioClip.Create(clipName, floatBuffer.Length, 1, wavFile.WaveFormat.AverageBytesPerSecond / 4, false);
        loaded.SetData(floatBuffer, 0);

        wavStream.Close();
        wavFile.Close();

        return loaded;
    }
}