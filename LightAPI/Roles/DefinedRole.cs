using LightInDark.Configuration;
using LightInDark.Game;
using LightInDark.Language;
using UnityEngine;

namespace LightInDark.Roles
{
    /// <summary>
    /// 角色定义（静态数据）。注册时请务必重写：
    ///  - <see cref="CodeName"/>（必须，唯一内部名，兼作语言/配置键前缀）
    ///  - <see cref="IntroBlurbKey"/>（必须，开场白语言键，其译文不可为空）
    ///
    /// 其余可直接用默认：描述默认读 "<see cref="CodeName"/>.describe"，技能介绍默认读
    /// "<see cref="CodeName"/>.skill"，职业名默认读 "<see cref="CodeName"/>.name"。
    /// 所有展示文本一律走语言键，不在代码里硬编码。
    /// </summary>
    public abstract class DefinedRole : IRoleDocument
    {
        /// <summary>显示名（默认按语言键 <see cref="NameKey"/> 解析，缺省回退 CodeName）。</summary>
        public string Name => LightInDark.Language.Language.GetStringOrKey(NameKey, CodeName);

        public Color Color { get; }
        public RoleCategory Category { get; }

        /// <summary>注册序号（RPC 用）</summary>
        public int Id { get; internal set; }

        /// <summary>唯一内部名（语言/配置键前缀）。必须重写。</summary>
        public abstract string CodeName { get; }

        /// <summary>职业名语言键。可重写，默认 "&lt;CodeName&gt;.name"。</summary>
        public virtual string NameKey => $"{CodeName}.name";

        /// <summary>职业描述语言键。可重写，默认 "&lt;CodeName&gt;.describe"。</summary>
        public virtual string DescriptionKey => $"{CodeName}.describe";

        /// <summary>职业描述（按语言键解析，缺省回退 CodeName）。</summary>
        public string Description => LightInDark.Language.Language.GetStringOrKey(DescriptionKey, CodeName);

        /// <summary>开场白语言键。必须重写，且其译文不可为 null/空（注册时校验）。</summary>
        public abstract string IntroBlurbKey { get; }

        /// <summary>开场白（按语言键解析）。</summary>
        public string IntroBlurb => LightInDark.Language.Language.GetStringOrKey(IntroBlurbKey, "");

        /// <summary>技能介绍语言键。可重写，默认 "&lt;CodeName&gt;.skill"。</summary>
        public virtual string SkillDescriptionKey => $"{CodeName}.skill";

        /// <summary>技能介绍（按语言键解析）。</summary>
        public string SkillDescription => LightInDark.Language.Language.GetStringOrKey(SkillDescriptionKey, "");

        /// <summary>分配参数（默认不参与分配）</summary>
        public virtual AllocationParameters Allocation => default;

        /// <summary>默认参数（实例化时使用）</summary>
        public virtual int[] DefaultArguments => System.Array.Empty<int>();

        /// <summary>该角色是否可在本局生成（分配机调用）</summary>
        public virtual bool CanSpawnIn() => true;

        /// <summary>职业立绘（帮助详情左上角），为 null 时不显示</summary>
        public virtual Sprite IconImage => null;

        protected DefinedRole(Color color, RoleCategory category)
        {
            Color = color;
            Category = category;
        }

        public abstract RuntimeRole CreateInstance(Player player, int[] arguments);
    }
}
