using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Helpers;

namespace Virial.Game.Object;

public class Door : IGameObject, IEquatable<Door>
{

    internal OpenableDoor VanillaObject { get; private set; }
    private Virial.Compat.ModGameObject transform;
    internal int DoorId => VanillaObject.Id;
    public Virial.Compat.Vector2 Position => transform.Position;
    public bool Equals(Door? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return DoorId == other.DoorId;
    }

    internal Door(OpenableDoor door)
    {
        this.VanillaObject = door;
        this.transform = new(door, true);
    }
}
