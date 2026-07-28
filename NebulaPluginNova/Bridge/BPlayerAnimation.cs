using PowerTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Bridge;

internal class BPlayerAnimation
{
    private PlayerAnimations myAnimations;
    private PlayerBodyTypes currentBodyType;
    private PlayerAnimationGroup currentGroup;
    private SpriteAnim currentAnimator;
    private AnimationClip currentSpawnAnim;
    private int instanceId;

    public BPlayerAnimation(PlayerAnimations animations)
    {
        this.myAnimations = animations;
        this.currentGroup = animations.group;
        this.currentBodyType = currentGroup.BodyType;
        this.currentSpawnAnim = currentGroup.SpawnAnim;
        this.currentAnimator = currentGroup.SpriteAnimator;
        this.instanceId = animations.GetInstanceIdFast();
    }

    internal void UpdateAnimationGroup(PlayerAnimationGroup group)
    {
        this.currentGroup = group;
        this.currentBodyType = currentGroup.BodyType;
        this.currentSpawnAnim = currentGroup.SpawnAnim;
    }

    public PlayerAnimationGroup Group => currentGroup;
    public AnimationClip SpawnAnim => currentSpawnAnim;
    public PlayerBodyTypes BodyType => currentBodyType;
    public SpriteAnim Animator => currentAnimator;
    public PlayerAnimations Animations => myAnimations;
    public int AnimationsInstanceId => instanceId;
}
