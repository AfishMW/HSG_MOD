using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar;

public static class AudioDevices
{
    public static IEnumerable<string> MicrophoneDevices()
    {
        var count = WaveInEvent.DeviceCount;
        for (int i = 0; i < count; i++)
        {
            yield return WaveInEvent.GetCapabilities(i).ProductName;
        }
    }

    public static IEnumerable<string> SpeakerDevices()
    {
        foreach (var device in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            yield return device.FriendlyName;
        }
    }

    public static string DefaultSpeaker => new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications).FriendlyName;
}