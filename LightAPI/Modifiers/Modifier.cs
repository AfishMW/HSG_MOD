using LightInDark.Game;
using LightInDark.Language;

namespace LightInDark.Modifiers
{
    /// <summary>
    /// 修饰器定义（静态数据）。与角色类似但更轻量，可叠加在玩家身上而不影响其职业。
    /// 复用点：<see cref="Key"/> 作为语言键前缀（默认 "<see cref="Key"/>.describe"）。
    /// </summary>
    public abstract class Modifier
    {
        public string Name { get; }
        public Color Color { get; }

        /// <summary>修饰器内部名（语言键前缀），默认取类型名。</summary>
        public virtual string Key => GetType().Name;

        /// <summary>修饰器描述，默认解析语言键 "<see cref="Key"/>.describe"。</summary>
        public virtual string Description => LightInDark.Language.Language.GetStringOrKey($"{Key}.describe", DescriptionFallback);

        /// <summary>构造时传入的描述回退文本。</summary>
        protected string DescriptionFallback { get; }

        /// <summary>实例化修饰器运行时对象。</summary>
        public abstract RuntimeModifier CreateInstance(Player player);

        protected Modifier(string name, Color color, string description = "")
        {
            Name = name;
            Color = color;
            DescriptionFallback = description;
        }
    }
}
