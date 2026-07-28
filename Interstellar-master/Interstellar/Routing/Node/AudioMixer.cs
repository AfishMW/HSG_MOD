using Interstellar.Messages;
using Interstellar.NAudio.Provider;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Node;

/// <summary>
/// 複数ノードを入力に持つノードで使用する音声データのミキサー。
/// </summary>
internal class AudioMixer : ISampleProvider
{
    private record Input(ISampleProvider Provider, int GroupId);
    private List<Input> inputs = new();
    private WaveFormat waveFormat;
    private float[] temp = null!;
    public AudioMixer(int channels)
    {
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioHelpers.ClockRate, channels);
    }

    WaveFormat ISampleProvider.WaveFormat => waveFormat;

    int ISampleProvider.Read(float[] buffer, int offset, int count)
    {
        if (temp == null || temp.Length < count) temp = new float[count];
        bool isFirst = true;
        if(inputs.Count == 0)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }

        foreach (var input in inputs)
        {
            int read = input.Provider.Read(temp, 0, count);
            if (isFirst)
            {
                for (int i = 0; i < read; i++) buffer[offset + i] = temp[i];
            }
            else
            {
                for (int i = 0; i < read; i++) buffer[offset + i] += temp[i];
            }
            isFirst = false;
        }
        return count;
    }

    public void AddInput(ISampleProvider input, int groupId)
    {
        if (input.WaveFormat.Channels == 1 && waveFormat.Channels == 2)
        {
            inputs.Add(new(new MonoToStereoSampleProvider(input), groupId));
        }
        else
        {
            inputs.Add(new(input, groupId));
        }
    }

    public void RemoveInput(int groupId)
    {
        inputs.RemoveAll(i => i.GroupId == groupId);
    }
}
