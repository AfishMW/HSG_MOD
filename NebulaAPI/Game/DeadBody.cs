using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Helpers;

namespace Virial.Game;

public class DeadBody
{
    internal int Id { get; private set; }
    internal global::DeadBody VanillaDeadBody { get; private set; }
    private Virial.Compat.ModGameObject transform;
    internal Virial.Compat.ModGameObject ModObject => transform;

    public Virial.Compat.Vector2 TruePosition => VanillaDeadBody.AsBoolFast() ? VanillaDeadBody.TruePosition : new(0f, 0f);
    public Virial.Compat.Vector2 Position => VanillaDeadBody.AsBoolFast() ? transform.Position : new(0f, 0f);
    public bool IsActive => VanillaDeadBody.AsBoolFast();
    public Player Player { get; private init; }
    public Player? CurrentHolder { get; internal set; }
    public bool IsFullyDissolved => (VanillaDeadBody.ParentId & 0x80) != 0;

    internal DeadBody(global::DeadBody vanillaDeadBody, int id, Player player)
    {
        this.Id = id;
        this.VanillaDeadBody = vanillaDeadBody;
        this.transform = new(vanillaDeadBody, true);
        this.Player = player;
    }
}
