using LightInDark.Game;

namespace LightInDark.Roles
{
    public abstract class DefinedRole
    {
        public string Name { get; }
        public Color Color { get; }

        protected DefinedRole(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public abstract RuntimeRole CreateInstance(Player player);
    }
}