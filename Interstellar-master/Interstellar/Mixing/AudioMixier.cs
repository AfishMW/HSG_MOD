using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Mixing;

internal interface IMixingContext
{
}
internal class AudioMixier
{
    internal ISampleProvider OutputProvider { get; }

    /// <summary>
    /// 音声データを追加します。
    /// </summary>
    /// <param name="clientId">音声の送り主</param>
    /// <param name="buffer">音声データを含む配列</param>
    /// <param name="length">音声データの長さ</param>
    internal void AddSamples(int clientId, float[] buffer, int length)
    {
    }
}
