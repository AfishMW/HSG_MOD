using LightInDark.Configuration;
using LightInDark.Game;

namespace LightInDark.Roles
{
    /// <summary>
    /// 角色定义（静态数据）
    /// </summary>
    public abstract class DefinedRole
    {
        public string Name { get; }
        public Color Color { get; }
        public RoleCategory Category { get; }
        public string Description { get; }

        protected DefinedRole(string name, Color color, RoleCategory category, string description = "")
        {
            Name = name;
            Color = color;
            Category = category;
            Description = description;
        }

        public abstract RuntimeRole CreateInstance(Player player);
    }
}
