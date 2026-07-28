using Concentus;
using Interstellar.Messages;
using Interstellar.Messages.Messages;
using Interstellar.Messages.Variation;
using NAudio.Wave;
using SIPSorcery.Net;
using System;
using System.Text;
using WebSocketSharp;

namespace Interstellar.Network;

internal interface IConnectionContext
{
    /// <summary>
    /// 音声フレームを受け取ったときに呼び出されます。
    /// </summary>
    /// <param name="clientId">音声フレームの送り主。</param>
    /// <param name="samples">音声データを含む配列。このメソッドの呼び出し後、配列は再利用されます。別の用途に使わないでください。</param>
    /// <param name="length">音声データの長さ。</param>
    void OnAudioFrameReceived(int clientId, float[] samples, int length);

    /// <summary>
    /// クライアントが切断したときに呼び出されます。
    /// </summary>
    /// <param name="clientId"></param>
    void OnClientDisconnected(int clientId); 

    /// <summary>
    /// クライアントのプロフィールが更新されたときに呼び出されます。
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="playerName"></param>
    /// <param name="playerId"></param>
    void OnClientProfileUpdated(int clientId, string playerName, byte playerId);

    /// <summary>
    /// ミュート状態の更新を受け取ったときに呼び出されます。
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="isMute"></param>
    void OnReceiveMuteStatus(int clientId, bool isMute);

    /// <summary>
    /// カスタムメッセージを受け取ったときに呼び出されます。
    /// </summary>
    /// <param name="message"></param>
    void OnCustomMessageReceived(byte[] message);
}

/// <summary>
/// サーバーと接続し、音声や情報の送受信を担います。
/// </summary>
internal class RoomConnection : IMessageProcessor
{
    private readonly string roomCode;
    private readonly string region;
    private readonly WebSocket socket;
    private RTCPeerConnection? connection = null;
    private ProfileMessage? profileMessage = null;
    private int? myClientId = null;

    public int MyClientId => myClientId ?? -1;

    private AudioStream? localAudioStream;

    IConnectionContext context;
    public RoomConnection(IConnectionContext context, string roomCode, string region, string url)
    {
        this.context = context;
        this.roomCode = roomCode;
        this.region = region;

        this.socket = new WebSocket(url);
        if (url.StartsWith("wss:")) this.socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;
        this.socket.OnMessage += (sender, e) =>
        {
            if (e.IsBinary) MessagePacker.UnpackMessages(e.RawData, this);
        };
        Connect();
    }

    public void UpdateMuteStatus(bool mute)
    {
        var message = new UpdateMuteStatusMessage(mute);
        socket.SendMessage(message);
    }

    /// <summary>
    /// ゲーム内のプレイヤー情報を更新します。
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="playerId"></param>
    public void UpdateProfile(string playerName, byte playerId)
    {
        var message = new ProfileMessage(playerName, playerId);
        profileMessage = message;
        TrySendProfile();
    }

    private void TrySendProfile()
    {
        if (profileMessage != null && socket.IsAlive)
        {
            this.socket.SendMessage(profileMessage);
            profileMessage = null;
        }
    }

    private void Connect()
    {
        this.socket.OnOpen += (sender, e) =>
        {
            SetUpRTCConnection();
            this.socket.SendMessage(new JoinMessage(this.roomCode, this.region));
        };
        this.socket.Connect();
    }

    private void SetUpRTCConnection()
    {
        //オーディオフレームを受け渡す関数
        float[] buffer = new float[2048];
        Dictionary<int, IOpusDecoder> decoders = new(64);
        void DecodeAndAddSample(int id, byte[] encodedAudio)
        {
            try
            {
                if (!decoders.ContainsKey(id)) decoders[id] = AudioHelpers.GetOpusDecoder();

                var decoder = decoders[id];
                int length = decoder.Decode(encodedAudio, buffer, buffer.Length);
                context.OnAudioFrameReceived(id, buffer, length);
            }
            catch (Exception excep)
            {
                Console.WriteLine(excep.ToString());
            }
        }


        this.socket.SendMessages(new JoinMessage(roomCode, region));
        TrySendProfile();
        this.connection = new RTCPeerConnection(WebSocketHelpers.GetRTCConfiguration());
        this.connection.OnAudioFrameReceived += frame =>
        {
            DecodeAndAddSample(frame.AudioFormat.FormatID, frame.EncodedAudio);
        };
        this.connection.onicecandidate += (candidate) =>
        {
            this.socket.SendMessage(new IceCandMessage(candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex, candidate.usernameFragment));
        };
    }

    private IOpusEncoder encoder = AudioHelpers.GetOpusEncoder();
    byte[] encodedBuffer = new byte[8192];
    public void SendAudio(float[] sampleBuffer, int sampleLength, double bufferMilliseconds)
    {
        if(localAudioStream == null) return;

        var durationRtpUnits = bufferMilliseconds.ToRtpUnits(AudioHelpers.ClockRate);
        int encodedLength = encoder.Encode(sampleBuffer, sampleLength, encodedBuffer, encodedBuffer.Length);
        localAudioStream?.SendAudio(durationRtpUnits, new ArraySegment<byte>(encodedBuffer, 0, encodedLength));
    }


    int IMessageProcessor.Process(MessageTag tag, ReadOnlySpan<byte> bytes)
    {
        int read = -1;
        switch (tag)
        {
            case MessageTag.ShareId:
                OnReceiveMyClientId(ShareIdMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.SdpOffer:
                OnReceiveSdpOffer(SdpOfferMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.AddIceCand:
                OnReceiveIceCandMessage(IceCandMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.ShareProfile:
                var profile = ShareProfileMessage.DeserializeWithoutTag(bytes, out read);
                context.OnClientProfileUpdated(profile.AudioId, profile.PlayerName, profile.PlayerId);
                break;
            case MessageTag.NoticeDisconnect:
                var disconnect =NoticeDisconnectMessage.DeserializeWithoutTag(bytes, out read);
                context.OnClientDisconnected(disconnect.ClientId);
                break;
            case MessageTag.ShareMuteStatus:
                var muteStatus = ShareMuteStatusMessage.DeserializeWithoutTag(bytes, out read);
                context.OnReceiveMuteStatus(muteStatus.ClientId, muteStatus.IsMute);
                break;
            case MessageTag.Custom:
                context.OnCustomMessageReceived(bytes.ToArray());
                break;
        }
        return read;
    }

    private void OnReceiveMyClientId(ShareIdMessage message)
    {
        int id = message.Id;
        myClientId = id;
        var localTrack = new MediaStreamTrack(AudioHelpers.GetOpusFormat(id), MediaStreamStatusEnum.SendOnly);
        connection!.addTrack(localTrack);
        localAudioStream = connection.AudioStreamList.Find(a => a.GetSendingFormat().ID == id);
    }

    MediaStreamTrack[] tracks = new MediaStreamTrack[AudioHelpers.MaxTracks]; 
    private void OnReceiveSdpOffer(SdpOfferMessage message)
    {
        //トラックの更新(削除はしない)
        long mask = message.Mask;
        for (int i = 0; i < AudioHelpers.MaxTracks; i++)
        {
            if ((mask & (1L << i)) != 0)
            {
                if (tracks[i] != null) continue;

                var format = AudioHelpers.GetOpusFormat(i);
                var track = new MediaStreamTrack(format, MediaStreamStatusEnum.RecvOnly);
                connection!.addTrack(track);
                tracks[i] = track;
            }
        }

        //SDPの処理
        connection!.setRemoteDescription(new RTCSessionDescriptionInit { sdp = message.Sdp, type = RTCSdpType.offer });
        var answer = connection.createAnswer(null);
        connection.setLocalDescription(answer).Wait();
        socket.SendMessage(new SdpAnswerMessage(answer.sdp));
    }

    private void OnReceiveIceCandMessage(IceCandMessage message)
    {
        connection!.addIceCandidate(new RTCIceCandidateInit
        {
            candidate = message.Candidate,
            sdpMid = message.SdpMid,
            sdpMLineIndex = (ushort)message.SdpMLineIndex,
            usernameFragment = message.UsernameFragment
        });
    }

    internal void SendCustomMessage(byte[] message)
    {
        socket.SendMessage(new CustomMessage(message));
    }

    internal void SendZeroSizeMessage(MessageTag tag)
    {
        socket.SendMessage(tag);
    }
    internal void Disconnect()
    {
        connection?.Close("Client left the game.");
        socket.Close();
    }
}
