using Epic.OnlineServices.Mods;
using Nebula.Roles.Impostor;
using TMPro;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;

namespace Nebula.Extensions;

public static class KillAnimationExtension
{
    public record IPlayerlikePosition(IPlayerlike? Player, VVector2 AlterPosition)
    {
        public bool AmOwner => (Player?.AmOwner ?? false) && Player is GamePlayer;
        public VVector2 Position => (Player?.IsActive ?? false) ? Player.Position : AlterPosition;
    }
    static public (IEnumerator enumerator, Vector2? deadPos) CoPerformModKill(this KillAnimation killAnim, byte requestSender, int requestId, PlayerControl source, IPlayerlikePosition target, GamePlayer realTarget, KillCharacteristics killCharacteristics, bool blink, bool targetIsUsingUtility, VVector2 deadGoalPos, bool useViperDeadBody, PlayerControl killer, CommunicableTextTag deadState)
    {
        VVector2? finalDeadPos = null;
        FollowerCamera cam = Camera.main.GetComponent<FollowerCamera>();
        bool isParticipant = source.AmOwner || target.AmOwner;
        PlayerPhysics sourcePhys = source.MyPhysics;
        if (blink) KillAnimation.SetMovement(source, false);

        //targetに対するKillAnimation.SetMovement
        target.Player?.Logic.SetMovement(false);

        //既存の死体を探す
        var existedBodies = ModSingleton<DeadBodyManager>.Instance.AllDeadBodies.ToArray();

        DeadBody GenerateDisableDeadBody(int variation, VVector2 position)
        {
            finalDeadPos = position;
            DeadBody deadBody = GameObject.Instantiate<DeadBody>(GameManager.Instance.deadBodyPrefab[useViperDeadBody ? 1 : 0]);
            if (useViperDeadBody)
            {
                var modViperDeadBody = deadBody.gameObject.AddComponent<ModViperDeadBody>();
                if (killer.AmOwner) modViperDeadBody.PlayKillerSE();
            }
            deadBody.enabled = false;
            deadBody.ParentId = realTarget.PlayerId;
            foreach (var r in deadBody.bodyRenderers.GetFastEnumerator()) realTarget.VanillaPlayer.SetPlayerMaterialColors(r);
            deadBody.gameObject.ForEachAllChildren(c => c.layer = LayerExpansion.GetPlayersLayer());

            realTarget.VanillaPlayer.SetPlayerMaterialColors(deadBody.bloodSplatter);

            VVector2 vector = position + new VVector2(killAnim.BodyOffset);

            //至近距離に死体がある場合、死体の位置をずらす
            if (existedBodies.Any(b => b.Position.Distance(vector) < 0.05f))
            {
                void TryShift()
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            var dir = VVector2.Right.Rotate(15 + 45 * i + 90 * j);
                            var cand = vector + dir * 0.12f;

                            if (
                                !Helpers.AnyNonTriggersBetween(vector, cand, out _, Constants.ShipAndAllObjectsMask) &&
                                !existedBodies.Any(b => b.Position.Distance(cand) < 0.05f)
                                )
                            {
                                vector = cand;
                                break;
                            }
                        }
                    }
                }
                TryShift();
            }

            deadBody.transform.position = new(vector.x, vector.y, vector.y / 1000f);

            var wrapped = ModSingleton<DeadBodyManager>.Instance.RegisterDeadBody(deadBody, DeadBodyManager.GenerateId(requestSender, requestId, variation), realTarget);
            GameOperatorManager.Instance?.Run(new DeadBodyInstantiateEvent(realTarget, wrapped, killer?.GetModInfo(), deadState));

            return deadBody;
        }

        bool targetIsRealPlayer = target.Player is GamePlayer;
        bool realTargetWillDie = targetIsRealPlayer || killCharacteristics.HasFlag(KillCharacteristics.FlagKillRealPlayer);

        DeadBody? realDeadbody = null;
        if (killCharacteristics.HasFlag(KillCharacteristics.FlagLeftRealDeadBody) || (targetIsRealPlayer && killCharacteristics.HasFlag(KillCharacteristics.FlagLeftDeadBody)))
        {
            VVector2 deadBodyPlayerPos = realTarget.Position;
            if (targetIsUsingUtility) deadBodyPlayerPos = deadGoalPos;
            realDeadbody = GenerateDisableDeadBody(0, deadBodyPlayerPos);
        }

        DeadBody? targetDeadBody = null;
        if (!targetIsRealPlayer)
        {
            var pos = target.Player?.Position ?? target.AlterPosition;
            if (killCharacteristics.HasFlag(KillCharacteristics.FlagLeftDeadBody))
            {
                targetDeadBody = GenerateDisableDeadBody(1, pos);
            }
            else
            {
                ManagedEffects.RpcDisappearEffect.LocalInvoke((pos.AsVector3(-1f), LayerExpansion.GetPlayersLayer()));
            }
        }


        if (target.Player is IFakePlayer fp)
        {
            GameOperatorManager.Instance?.Run(new PlayerKillFakePlayerEvent(fp, source.GetModInfo()!), shouldNotCheckGameEnd: false);
            fp.Release();
        }


        if (isParticipant)
        {
            cam.Locked = true;
            ConsoleJoystick.SetMode_Task();
            if (AmongUsLLImpl.LocalPlayer.AmOwner) //恒真では？
            {
                AmongUsLLImpl.LocalPlayer.MyPhysics.inputHandler.enabled = true;
            }
        }

        if (realTargetWillDie)
        {
            realTarget.VanillaPlayer.Die(DeathReason.Kill, false);
            (realTarget as PlayerModInfo)?.UpdateModDead(true);
            PlayerExtension.ResetOnDying(realTarget.VanillaPlayer);
        }

        IEnumerator GetEnumerator()
        {
            if (blink)
            {
                if (!source.Data.IsDead)
                {
                    float timeout = 0.2f;
                    var anim = source.MyPhysics.Animations.CoPlayCustomAnimation(killAnim.BlurAnim);
                    while (timeout > 0f && anim.MoveNext())
                    {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }
                }
                source.NetTransform.SnapTo(target.Position);
                sourcePhys.Animations.PlayIdleAnimation();
                KillAnimation.SetMovement(source, true);
            }

            if (target.Player?.IsActive ?? false) target.Player?.Logic?.SetMovement(true);

            if (targetDeadBody != null) targetDeadBody.enabled = true;
            if (realDeadbody != null) realDeadbody.enabled = true;

            if (isParticipant)
            {
                cam.Locked = false;
            }
            yield break;
        }
        return (GetEnumerator(), finalDeadPos);
    }
}
