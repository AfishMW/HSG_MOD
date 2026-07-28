using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages;

public enum MessageTag
{
    Join = 0,
    Profile = 1,
    SdpOffer = 2,
    SdpAnswer = 3,
    AddIceCand = 4,
    ShareId = 5,
    ShareProfile = 6,
    NoticeDisconnect = 7,
    Custom = 8,
    RequestReload = 9,
    UpdateMuteStatus = 10,
    ShareMuteStatus = 11,
}
