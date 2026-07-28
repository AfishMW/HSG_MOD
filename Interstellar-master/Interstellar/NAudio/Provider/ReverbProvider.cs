using NAudio.Wave;

namespace Interstellar.NAudio.Provider;

/// <summary>
/// リバーブ効果を追加するSampleProvider。
/// </summary>
internal class ReverbSampleProvider : ISampleProvider
{
    private readonly ISampleProvider sourceProvider;
    private readonly float[] delayBuffer;
    private int delayPosition;
    private float decay;
    private float wetMix; // リバーブ音のミックスレベル
    private float dryMix; // 原音のミックスレベル

    public float Decay { get => decay; set => decay = Math.Clamp(value, 0.0f, 1.0f); }
    public float WetDryMix { get => wetMix; set { wetMix = Math.Clamp(value, 0.0f, 1.0f); dryMix = 1.0f - wetMix; } }

    public WaveFormat WaveFormat => sourceProvider.WaveFormat;

    /// <summary>
    /// リバーブ効果を追加するSampleProvider
    /// </summary>
    /// <param name="sourceProvider">入力音声</param>
    /// <param name="delayMilliseconds">遅延（ミリ秒）</param>
    /// <param name="decay">減衰率 (0.0 - 1.0)</param>
    /// <param name="wetDryMix">エフェクト音と原音のミックスバランス (0.0=原音のみ, 1.0=エフェクト音のみ)</param>
    public ReverbSampleProvider(ISampleProvider sourceProvider, int delayMilliseconds, float decay, float wetDryMix)
    {
        this.sourceProvider = sourceProvider;
        int delaySamples = (int)(WaveFormat.SampleRate * (delayMilliseconds / 1000.0f)) * WaveFormat.Channels;
        delayBuffer = new float[delaySamples];
        this.decay = decay;
        this.wetMix = wetDryMix;
        this.dryMix = 1.0f - wetDryMix;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = sourceProvider.Read(buffer, offset, count);
        
        for (int i = 0; i < samplesRead; i++)
        {
            float currentSample = buffer[offset + i];            
            float delayedSample = delayBuffer[delayPosition];

            delayBuffer[delayPosition] = currentSample + (delayedSample * decay);
            buffer[offset + i] = (currentSample * dryMix) + (delayedSample * wetMix);

            delayPosition = (delayPosition + 1) % delayBuffer.Length;
        }
        return samplesRead;
    }
}