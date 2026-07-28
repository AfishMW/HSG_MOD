using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Server.VoiceChat;

static internal class RoomManager
{
    static private Dictionary<string, VCRoom> rooms = [];

    static public VCRoom GetRoom(string region, string roomId)
    {
        string key = region + "." + roomId;
        if (rooms.TryGetValue(key, out var room))
        {
            return room;
        }
        else
        {
            room = new VCRoom(key);
            rooms[key] = room;
            return room;
        }
    }

    static public void RemoveRoom(string key) => rooms.Remove(key);
}
