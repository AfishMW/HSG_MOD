using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Game;

internal class LocalStatus
{
    public bool HasReportButton => GamePlayer.LocalPlayer?.AllAbilities.All(a => a.HasReportButton) ?? true;
    public bool CanReport => GamePlayer.LocalPlayer?.AllAbilities.All(a => a.CanReport) ?? true;
}
