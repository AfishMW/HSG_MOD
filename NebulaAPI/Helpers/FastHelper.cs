using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Compat;

namespace Virial.Helpers;

public static class FastHelper
{
    public static ModGameObject ModGameObject(this UnityEngine.Component component, bool keep = true) => new(component, keep);
    public static ModGameObject ModGameObject(this UnityEngine.GameObject obj, bool keep = true) => new(obj, keep);
    public static ModGameObject ModGameObject(this UnityEngine.Transform transform) => new(transform);
}
