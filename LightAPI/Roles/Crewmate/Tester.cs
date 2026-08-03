// 文件: Roles/ExampleRole.cs
using LightInDark.Abilities;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.Patches;
using LightInDark.UI;
using UnityEngine;

namespace LightInDark.Roles
{
    public class ExampleRole : DefinedRole
    {
        public ExampleRole() : base("示例角色", Color.Cyan) { }

        public override RuntimeRole CreateInstance(Player player)
        {
            return new ExampleRuntime(this, player);
        }
    }

    public class ExampleRuntime : RuntimeRole
    {
        public ExampleRuntime(DefinedRole definition, Player player) : base(definition, player) { }

        protected override void OnActivated()
        {
            LightLogger.Log($"[ExampleRuntime] 角色激活，玩家: {MyPlayer.Name}");
            // 添加一个示例能力
            AddAbility(new ExampleAbility(MyPlayer));
        }
    }
}
public class ExampleAbility : AbstractPlayerAbility
{

    public ExampleAbility(Player player) : base(player)
    {
        LightLogger.Log($"[ExampleAbility] 能力创建，玩家: {player.Name}");

        // 加载一个测试图标（如果没有，就用内置图标）
        Sprite icon = HudManager.Instance.KillButton.graphic.sprite;

        var button = AbilityButtonFactory.CreateNormal(this, MyPlayer)
            .SetLabel("隐身")
            .SetImage(icon)
            .BindKey(KeyCode.F)
            .SetCooldown(5f)
            .Build();

        button.OnClick = () =>
        {
            LightLogger.Log($"[Ability] 隐身能力被触发");

            MyPlayer.Control.CoSetName("Button 能力触发！我们是冠军！！！！！");
            PatchManager.SendLocalMessage("We are champion!");
            MyPlayer.Control.RpcStartMeeting(null);
            button.StartCoolDown();
        };

        LightLogger.Log("[ExampleAbility] 按钮创建完成");
    }

    public override void Release()
    {
        LightLogger.Log("[ExampleAbility] 释放能力");
        // 按钮会自动释放，因为我们绑定了寿命
        base.Release();
    }
}