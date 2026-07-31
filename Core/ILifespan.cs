public interface ILifespan
{
    bool IsDeadObject { get; }
    bool IsAliveObject => !IsDeadObject;
}

public interface IReleasable
{
    void Release();
}

public class SimpleLifespan : ILifespan, IReleasable
{
    private bool dead = false;
    public bool IsDeadObject => dead;
    public void Release() => dead = true;
}

public class DependentLifespan : ILifespan
{
    private ILifespan? parent;
    public bool IsDeadObject => parent?.IsDeadObject ?? false;
    public bool Bind(ILifespan parent) { this.parent = parent; return true; }
}

public class FlexibleLifespan : DependentLifespan, IReleasable
{
    private bool released = false;
    public bool IsDeadObject => released || base.IsDeadObject;
    public void Release() => released = true;
}
public interface IGameComponent { }

public interface IGameOperator : IGameComponent,IReleasable,ILifespan
{
    void OnReleased() { }
}
public static class GameEntityExtension
{
    public static T Register<T>(this T gameEntity, ILifespan lifespan) where T : IGameOperator
    {
        return gameEntity;
    }
}