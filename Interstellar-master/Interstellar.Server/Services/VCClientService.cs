using Interstellar.Messages;
using Interstellar.Messages.Messages;
using Interstellar.Messages.Variation;
using Interstellar.Server.VoiceChat;
using SIPSorcery.Net;
using System.Collections.Concurrent;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Interstellar.Server.Services;

internal class VCClientService : WebSocketBehavior, IMessageProcessor
{
    private RTCPeerConnection connection;
    private Dictionary<int, MediaStreamTrack> streamTracks = new(32);
    private Dictionary<int, AudioStream> audioStreams = new(32);
    private VCClient? client = null;
    private bool IsJoined => client != null;
    private readonly ConcurrentQueue<IceCandMessage> pendingIceCandidates = new();

    public VCClientService()
    {
        connection = new(WebSocketHelpers.GetRTCConfiguration());
        connection.OnAudioFrameReceived += frame =>
        {
            var durationRtpUnits = RtpTimestampExtensions.ToRtpUnits(frame.DurationMilliSeconds, AudioHelpers.ClockRate);
            client?.BroadcastAudio(durationRtpUnits, frame.EncodedAudio);
        };

        connection.onicecandidate += (candidate) =>
        {
            Console.WriteLine("Client " + this.ID + " ICE candidate generated.");
            var msg = new IceCandMessage(candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex, candidate.usernameFragment);
            // まだ開いていない場合があるので開いているかを確認
            if (this.Context?.WebSocket?.ReadyState == WebSocketState.Open)
            {
                SendMessage(msg);
            }
            else
            {
                pendingIceCandidates.Enqueue(msg);
            }
        };
    }

    protected override void OnOpen()
    {
        Console.WriteLine("Client " + this.ID + " connected.");
        // WebSocketが開く前に送信しようとしたICE候補を送信
        while (pendingIceCandidates.TryDequeue(out var msg))
        {
            SendMessage(msg);
        }
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            try
            {
                MessagePacker.UnpackMessages(e.RawData, this);
            }catch(InvalidDataException ex)
            {
                Console.WriteLine("Error processing message from client " + this.ID + ": " + ex.Message);
            }
        }

    }

    protected override void OnClose(CloseEventArgs e)
    {
        Console.WriteLine("Client " + this.ID + " disconnected.");
        client?.Close();
        pendingIceCandidates.Clear();
    }

    int IMessageProcessor.Process(MessageTag tag, ReadOnlySpan<byte> bytes)
    {
        int read = -1;
        switch (tag)
        {
            case MessageTag.Join:
                Console.WriteLine("Client " + this.ID + " requested to join a room.");
                JoinRoom(JoinMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.SdpAnswer:
                Console.WriteLine("Client " + this.ID + " sent SDP answer.");
                AcceptSdpAnswer(SdpAnswerMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.AddIceCand:
                Console.WriteLine("Client " + this.ID + " sent ICE candidate.");
                AddIceCandidate(IceCandMessage.DeserializeWithoutTag(bytes, out read));
                break;
            case MessageTag.Profile:
                if (client == null)
                {
                    Console.WriteLine("Client " + this.ID + " sent profile without joining room.");
                    break;
                }
                var profile = ProfileMessage.DeserializeWithoutTag(bytes, out read);
                Console.WriteLine("Client " + this.ID + " sent profile (name: " + profile.PlayerName + "id: " + profile.PlayerId + ").");
                client.UpdateProfile(profile.PlayerName, profile.PlayerId);
                break;
            case MessageTag.Custom:
                CustomMessage.DeserializeForServerWithoutTag(bytes, out read);
                client?.BroadcastRawMessage(bytes);
                break;
            case MessageTag.RequestReload:
                read = 0;
                ResendConnectionInformation();
                break;
            case MessageTag.UpdateMuteStatus:
                var muteStatus = UpdateMuteStatusMessage.DeserializeWithoutTag(bytes, out read);
                client?.UpdateMuteStatus(muteStatus.Mute);
                break;
        }
        return read;
    }

    private void JoinRoom(JoinMessage message)
    {
        if (!IsJoined)
        {
            Console.WriteLine("Client " + this.ID + " joined room " + message.RoomCode + " in region " + message.Region);
            VCRoom room = RoomManager.GetRoom(message.Region, message.RoomCode);
            client = room.Join(this);

            //受け取りトラックを追加する。
            var format = AudioHelpers.GetOpusFormat(client.ClientId);
            var stream = new MediaStreamTrack(format, MediaStreamStatusEnum.RecvOnly);
            connection.addTrack(stream);

            SendMessages([new ShareIdMessage(client.ClientId), UpdateTracks(room.CurrentVoiceMask), ..client.ShareExistingProfiles()]);
        }
    }

    private void AcceptSdpAnswer(SdpAnswerMessage message)
    {
        connection.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = message.Sdp });
    }

    private void AddIceCandidate(IceCandMessage message)
    {
        connection.addIceCandidate(new RTCIceCandidateInit
        {
            candidate = message.Candidate,
            sdpMid = message.SdpMid,
            sdpMLineIndex = (ushort)message.SdpMLineIndex,
            usernameFragment = message.UsernameFragment
        });
    }

    private void ResendConnectionInformation()
    {
        if(client == null) return;
        SendMessages([UpdateTracks(client.Room.CurrentVoiceMask), ..client.ShareExistingProfiles()]);
    }

    private SdpOfferMessage UpdateTracks(long mask)
    {
        int myId = client!.ClientId;
        for(int i = 0; i < AudioHelpers.MaxTracks; i++)
        {
            if(i == myId) continue;
            bool shouldHave = (mask & (1L << i)) != 0;
            bool have = streamTracks.ContainsKey(i);
            if (shouldHave && !have)
            {
                var format = AudioHelpers.GetOpusFormat(i);
                var stream = new MediaStreamTrack(format, MediaStreamStatusEnum.SendOnly);
                streamTracks.Add(i, stream);
                connection.addTrack(stream);
            }
        }

        //AudioStreamを更新
        audioStreams.Clear();
        foreach (var audioStream in connection.AudioStreamList)
        {
            audioStreams[audioStream.GetSendingFormat().ID] = audioStream;
        }
        

        var offer = connection.createOffer(null);
        connection.setLocalDescription(offer).Wait();
        return new SdpOfferMessage(offer.sdp, mask);
    }

    /// <summary>
    /// クライアントに現在のトラック情報を送信します。SDPオファーメッセージを送信します。
    /// </summary>
    /// <param name="mask"></param>
    public void SendTracksMask(long mask)
    {
        if (IsJoined)
        {
            SendMessage(UpdateTracks(mask));
        }
    }

    public void SendClientLeft(int clientId)
    {
        if (IsJoined)
        {
            SendMessage(new NoticeDisconnectMessage(clientId));
        }
    }

    /// <summary>
    /// クライアントに音声フレームを送信します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="durationRtpUnits"></param>
    /// <param name="encodedAudio"></param>
    public void SendAudio(int id, uint durationRtpUnits, byte[] encodedAudio)
    {
        if (audioStreams.TryGetValue(id, out var stream))
        {
            stream.SendAudio(durationRtpUnits, encodedAudio);
        }
        else
        {
            if(!lastError.HasValue || System.DateTime.Now.Subtract(lastError.Value).Microseconds > 500)
            {
                lastError = System.DateTime.Now;
                SendMessage(UpdateTracks(client!.Room.CurrentVoiceMask));
                return;
            }
        }
    }
    System.DateTime? lastError = null;

    /// <summary>
    /// クライアントにメッセージを送信します。
    /// </summary>
    /// <param name="message"></param>
    public void SendMessage(IMessage message) => this.Send(MessagePacker.PackMessage(message).ToArray());
    
    /// <summary>
    /// クライアントにメッセージを与えられた順序で送信します。
    /// </summary>
    /// <param name="messages"></param>
    public void SendMessages(params IEnumerable<IMessage> messages) => this.Send(MessagePacker.PackMessages(messages).ToArray());

    public void SendRawMessage(byte[] message) => this.Send(message.ToArray());
}
