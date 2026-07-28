using Interstellar.Messages;
using Interstellar.Network;
using Interstellar.Routing;
using NAudio.Wave;

namespace Interstellar.VoiceChat;

public class VCRoomParameters
{
    public VCRoom.OnConnectClient? OnConnectClient;
    public VCRoom.OnUpdateProfile? OnUpdateProfile;
    public VCRoom.CustomMessageHandler? MessageHandler;
    public VCRoom.OnDisconnect? OnDisconnect;
    public VCRoom.OnUpdateMuteStatus? OnUpdateMuteStatus;
    public int BufferMaxLength = 4096;
    public int BufferLength = 2048;

    public VCRoomParameters SetBufferLength(int length, int additional = 2048)
    {
        BufferLength = length;
        BufferMaxLength = length + additional;
        return this;
    }
}

public class VCRoom : IConnectionContext, IHasAudioPropertyNode, IMicrophoneContext, ISpeakerContext
{
    private RoomConnection connection;
    private AudioManager audioManager;
    private Dictionary<int, AudioRoutingInstance> audioInstances = new();
    private readonly OnConnectClient? onConnectClient;
    private readonly OnUpdateProfile? onUpdateProfile;
    private readonly CustomMessageHandler? onCustomMessage;
    private readonly OnDisconnect? onDisconnect;
    private readonly OnUpdateMuteStatus? onUpdateMuteStatus;
    private bool loopBack = false;

    public delegate void OnConnectClient(int clientId, AudioRoutingInstance routing, bool isLocalClient);
    public delegate void OnUpdateProfile(int clientId, byte playerId, string playerName);
    public delegate void OnUpdateMuteStatus(int clientId, bool mute);
    public delegate void OnDisconnect(int clientId);
    public delegate void CustomMessageHandler(byte[] message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="audioRouter"></param>
    /// <param name="roomCode"></param>
    /// <param name="region"></param>
    /// <param name="url"></param>
    /// <param name="onConnectClient"></param>
    /// <param name="onUpdateProfile">プロフィールが更新されたときに呼び出されます。過去に共有されたProfileであっても、onConnectClientで接続を通知された後に呼び出されることが保証されています。</param>
    public VCRoom(AbstractAudioRouter audioRouter, string roomCode, string region, string url, VCRoomParameters? additionalParameters)
    {
        this.onConnectClient = additionalParameters?.OnConnectClient;
        this.onUpdateProfile = additionalParameters?.OnUpdateProfile;
        this.onCustomMessage = additionalParameters?.MessageHandler;
        this.onDisconnect = additionalParameters?.OnDisconnect;
        this.onUpdateMuteStatus = additionalParameters?.OnUpdateMuteStatus;

        this.connection = new RoomConnection(this, roomCode, region, url);
        this.audioManager = new AudioManager(audioRouter, additionalParameters?.BufferLength ?? 2048, additionalParameters?.BufferMaxLength ?? 4096);
    }

    public void SetLoopBack(bool enable) => this.loopBack = enable;

    /// <summary>
    /// 自身のプロフィールを更新します。
    /// ゲーム終了後、ロビーに戻ったときなどに呼び出してください。
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <param name="playerId">プレイヤーID</param>
    public void UpdateProfile(string playerName, byte playerId) => this.connection.UpdateProfile(playerName, playerId);

    /// <summary>
    /// 音声を送信します。
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="length"></param>
    void IMicrophoneContext.SendAudio(float[] samples, int samplesLength, double samplesMilliseconds, float coeff)
    {
        for(int i = 0; i < samplesLength; i++) samples[i] *= coeff;
        if (!Mute)
        {
            this.connection.SendAudio(samples, samplesLength, samplesMilliseconds);
        }
        OnAudioSent(samples, samplesLength);
    }

    ISampleProvider? ISpeakerContext.GetEndpoint() => audioManager.Endpoint;

    IMicrophone? microphone = null;
    public IMicrophone? Microphone
    {
        get => microphone;
        set
        {
            this.microphone?.Close();
            value?.Initialize(this);
            this.microphone = value;
        }
    }
    public void SetMicrophone(IMicrophone? microphone) => Microphone = microphone;

    ISpeaker? speaker = null;
    public ISpeaker? Speaker
    {
        get => speaker;
        set
        {
            this.speaker?.Close();
            value?.Initialize(this);
            this.speaker = value;
        }
    }
    public void SetSpeaker(ISpeaker? speaker) => Speaker = speaker;

    private AudioRoutingInstance GetOrCreateAudioInstance(int clientId, bool asLocalClient)
    {
        if (!audioInstances.TryGetValue(clientId, out var instance))
        {
            instance = audioManager.Generate(clientId);
            onConnectClient?.Invoke(clientId, instance, asLocalClient);
            audioInstances[clientId] = instance;
            if (pooledProfile.TryGetValue(clientId, out var profile))
            {
                onUpdateProfile?.Invoke(clientId, profile.id, profile.name);
                pooledProfile.Remove(clientId);
            }
        }
        return instance;
    }

    private bool TryGetAudioInstance(int clientId, out AudioRoutingInstance? instance) => audioInstances.TryGetValue(clientId, out instance);

    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId) => (audioManager as IHasAudioPropertyNode).GetProperty(propertyId);

    void IConnectionContext.OnAudioFrameReceived(int clientId, float[] samples, int length)
    {
        var instance = GetOrCreateAudioInstance(clientId, false);
        instance.AddSamples(samples, 0, length);
    }

    void IConnectionContext.OnClientDisconnected(int clientId)
    {
        if(TryGetAudioInstance(clientId, out var instance))
        {
            audioManager.Remove(clientId);
            audioInstances.Remove(clientId);
            onDisconnect?.Invoke(clientId);
        }
    }

    void IConnectionContext.OnCustomMessageReceived(byte[] message)
    {
        onCustomMessage?.Invoke(message);
    }

    public void SendCustomMessage(byte[] message)
    {
        connection.SendCustomMessage(message);
    }

    public void Rejoin()
    {
        connection.SendZeroSizeMessage(MessageTag.RequestReload);
    }

    Dictionary<int, (string name, byte id)> pooledProfile = [];
    void IConnectionContext.OnClientProfileUpdated(int clientId, string playerName, byte playerId)
    {
        if (TryGetAudioInstance(clientId, out _))
        {
            onUpdateProfile?.Invoke(clientId, playerId, playerName);
        }
        else
        {
            pooledProfile[clientId] = (playerName, playerId);
        }
    }

    void IConnectionContext.OnReceiveMuteStatus(int clientId, bool isMute)
    {
        onUpdateMuteStatus?.Invoke(clientId, isMute);
    }

    void OnAudioSent(float[] buffer, int count)
    {
        if (loopBack && connection.MyClientId != -1)
        {
            var instance = GetOrCreateAudioInstance(connection.MyClientId, true);
            instance.AddSamples(buffer, 0, count);
        }
    }

    private bool mute = false;
    public bool Mute => mute;
    public void SetMute(bool mute)
    {
        if (this.mute == mute) return;
        this.mute = mute;
        connection.UpdateMuteStatus(mute);
    }

    public void Disconnect()
    {
        connection.Disconnect();
        Microphone = null;
        Speaker = null;
    }

    public int SampleRate => AudioHelpers.ClockRate;
}
