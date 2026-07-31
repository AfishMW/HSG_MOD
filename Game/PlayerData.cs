using HarmonyLib;
using InnerNet;
using LightInDark.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInDark.Game;

public class PlayerData
{
    [OnlyHost]
    void GetAllData(GameStartEvent ev)
    {

    }
}
