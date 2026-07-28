using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Node;

/// <summary>
/// 複数ノードから読み取られるノードで使用する音声データのバッファ。
/// </summary>
internal class AudioBuffer : ISampleProvider
{
    float[]? buffer;
    float[]? temp;
    int length = 0;
    int groupId = -1;
    public int GroupId => groupId;
    ISampleProvider source;

    public AudioBuffer(ISampleProvider source, int groupId)
    {
        this.source = source;
        this.groupId = groupId;
    }

    public void Clear()
    {
        buffer = null;
    }

    

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count) { 
        if(this.buffer == null)
        {
            if(temp != null && temp.Length >= count) this.buffer = temp;
            else temp = this.buffer = new float[count];
            
            int actualCount = source.Read(this.buffer, 0, count);
            if (actualCount < count) Array.Clear(this.buffer, actualCount, count - actualCount);
            length = count;
        }
        if (count != length) throw new InvalidOperationException("The count must be consistent across all calls in the sequence.");

        Buffer.BlockCopy(this.buffer, 0, buffer, offset * 4, count * 4);
        return count;
    }
}
