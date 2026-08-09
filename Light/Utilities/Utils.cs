using LightInDark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities;

public static class Utils
{
    public static bool TryChangeMainMenuBackground(byte index)
    {
        try
        {
            // TODO 改背景板
            return true;
        }
        catch(Exception ex)
        {
            LightLogger.LogError($"更改背景板时发生错误：{ex.Message}\n错误堆栈->\n{ex}");
            return false;
        }
    }
}