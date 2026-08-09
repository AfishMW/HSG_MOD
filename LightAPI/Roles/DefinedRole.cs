using System;
using LightInDark.Configuration;
using LightInDark.Game;
using UnityEngine;

namespace LightInDark.Roles
{
    /// <summary>
    /// 角色定义（静态数据）
    /// </summary>
    public abstract class DefinedRole : IRoleDocument
    {
        public string Name { get; }
        public Color Color { get; }
        public RoleCategory Category { get; }
        public string Description { get; }

        /// <summary>注册序号（RPC 用）</summary>
        public int Id { get; internal set; }

        /// <summary>分配参数（默认不参与分配）</summary>
        public virtual AllocationParameters Allocation => default;

        /// <summary>默认参数（实例化时使用）</summary>
        public virtual int[] DefaultArguments => Array.Empty<int>();

        /// <summary>该角色是否可在本局生成（分配机调用）</summary>
        public virtual bool CanSpawnIn() => true;

        /// <summary>职业立绘（帮助详情左上角），为 null 时不显示</summary>
        public virtual Sprite IconImage => null;

        /// <summary>开场白（分配职业时显示在职业名底部），为空时不显示</summary>
        public virtual string IntroBlurb => "";

        /// <summary>技能介绍（帮助详情立绘下方），多行用 \n 分隔</summary>
        public virtual string SkillDescription => "";

        protected DefinedRole(string name, Color color, RoleCategory category, string description = "")
        {
            Name = name;
            Color = color;
            Category = category;
            Description = description;
        }

        public abstract RuntimeRole CreateInstance(Player player, int[] arguments);
    }
}
