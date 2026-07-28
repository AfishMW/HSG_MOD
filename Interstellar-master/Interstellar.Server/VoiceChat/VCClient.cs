using Interstellar.Messages;
using Interstellar.Messages.Variation;
using Interstellar.Server.Services;
using System.Diagnostics.CodeAnalysis;

namespace Interstellar.Server.VoiceChat;

internal class VCClient
{
    internal record Profile(string PlayerName, byte PlayerId);

    VCClientService service;
    VCRoom myRoom;
    Profile? profile = null;

    public bool IsClosed => service.ConnectionState == WebSocketSharp.WebSocketState.Closed;

    public bool IsMute { get; private set; } = false;

    public VCRoom Room => myRoom;

    public byte ClientId { get; }

    public VCClient(VCClientService service, byte clientId, VCRoom room)
    {
        this.service = service;
        this.ClientId = clientId;
        this.myRoom = room;
    }

    public void UpdateMuteStatus(bool isMute)
    {
        if(this.IsMute == isMute) return;
        this.IsMute = isMute;
        myRoom.Broadcast(ClientId, new ShareMuteStatusMessage(ClientId, isMute));
    }

    /// <summary>
    /// 自分以外の誰かが入退室したときに呼び出されます。
    /// </summary>
    /// <param name="currentMask"></param>
    public void OnJoinOrLeaveAnyone(long currentMask) {
        this.service.SendTracksMask(currentMask);
    }

    public void NoticeLeaveClient(byte clientId)
    {
        this.service.SendClientLeft(clientId);
    }

    /// <summary>
    /// 部屋に自身の音声をブロードキャストします。
    /// </summary>
    /// <param name="durationRtpUnits"></param>
    /// <param name="encodedAudio"></param>
    public void BroadcastAudio(uint durationRtpUnits, byte[] encodedAudio)
    {
        myRoom.Broadcast(ClientId, durationRtpUnits, encodedAudio);
    }

    public void BroadcastRawMessage(ReadOnlySpan<byte> message)
    {
        myRoom.BroadcastRawMessage(ClientId, message.ToArray());
    }

    /// <summary>
    /// このクライアントに音声を送信します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="durationRtpUnits"></param>
    /// <param name="encodedAudio"></param>
    public void SendAudio(int id, uint durationRtpUnits, byte[] encodedAudio)
    {
        this.service.SendAudio(id, durationRtpUnits, encodedAudio);
    }

    public void Send(byte[] rawMessage)
    {
        this.service.SendRawMessage(rawMessage);
    }

    public void Send(IMessage message) => this.service.SendMessage(message);

    public void UpdateProfile(string playerName, byte playerId)
    {
        this.profile = new Profile(playerName, playerId);
        myRoom.Broadcast(ClientId, new ShareProfileMessage(ClientId, playerName, playerId));
    }

    public bool TryGetProfile([MaybeNullWhen(false)]out string playerName, out byte playerId)
    {
        if(this.profile != null)
        {
            playerName = this.profile.PlayerName;
            playerId = this.profile.PlayerId;
            return true;
        }
        playerName = null;
        playerId = 0;
        return false;
    }

    /// <summary>
    /// クライアントとの通信を切断します。
    /// </summary>
    public void Close()
    {
        myRoom.Leave(this);
    }

    internal IEnumerable<ShareProfileMessage> ShareExistingProfiles()
    {
        foreach (var c in myRoom.Clients)
        {
            if (c.ClientId != this.ClientId && c.TryGetProfile(out var name, out var pid))
            {
                yield return new ShareProfileMessage(c.ClientId, name, pid);
            }
        }
    }
}
