using Interstellar.Messages;
using Interstellar.Messages.Variation;
using Interstellar.Server.Services;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Server.VoiceChat;

internal class VCRoom
{
    string myKey;
    Dictionary<byte, VCClient> fastClients = new();

    public VCRoom(string key)
    {
        myKey = key;
    }

    private void CheckAlive()
    {
        foreach (var closed in fastClients.Where(c => c.Value.IsClosed).ToArray())
        {
            try
            {
                Leave(closed.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while removing closed client: " + ex);
            }
        }
    }

    private byte AvailableId()
    {
        byte id = 0;
        while(fastClients.ContainsKey(id)) id++;
        return id;
    }

    public VCClient Join(VCClientService service)
    {
        CheckAlive();

        var client = new VCClient(service, AvailableId(), this);
        fastClients.Add(client.ClientId, client);
        
        //入室を通知する。
        long currentMask = CurrentVoiceMask;

        foreach (var c in fastClients.Values)
        {
            if(c.ClientId != client.ClientId) c.OnJoinOrLeaveAnyone(currentMask);
        }

        return client;
    }

    public void Leave(VCClient client)
    {
        if (fastClients.Remove(client.ClientId))
        {
            //退室を通知する。
            long currentMask = CurrentVoiceMask;
            foreach (var c in fastClients.Values)
            {
                if (c.IsClosed) continue;
                c.OnJoinOrLeaveAnyone(currentMask);
                c.NoticeLeaveClient(client.ClientId);
            }
        }

        if(fastClients.Count == 0) RoomManager.RemoveRoom(myKey);
    }

    public long CurrentVoiceMask { get
        {
            long mask = 0;
            foreach(var client in fastClients.Values)
            {
                mask |= (1L << client.ClientId);
            }
            return mask;
        } 
    }

    public void Broadcast(byte id, uint durationRtpUnits, byte[] encodedAudio)
    {
        foreach(var client in fastClients.Values)
        {
            if(client.ClientId != id) client.SendAudio(id, durationRtpUnits, encodedAudio);
        }
    }

    public void BroadcastRawMessage(byte id, byte[] rawMessage)
    {
        foreach (var client in fastClients.Values)
        {
            if (client.ClientId != id) client.Send(rawMessage);
        }
    }

    public void Broadcast(byte sender, IMessage message)
    {
        foreach (var client in fastClients.Values)
        {
            if (client.ClientId != sender) client.Send(message);
        }
    }

    public IEnumerable<VCClient> Clients => fastClients.Values;
}
