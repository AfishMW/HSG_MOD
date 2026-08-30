using LightInDark.Core;
using System;

namespace Light.Configuration;

public class RoleOption<T>
{
    public string Key { get; } 
    public T DefaultValue { get; }
    public T Value { get; set; }
}
