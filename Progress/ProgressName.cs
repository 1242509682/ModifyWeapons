﻿namespace ModifyWeapons.Progress;

[AttributeUsage(AttributeTargets.Field)]
public class ProgressName : Attribute
{
    public string[] Names { get; set; }

    public ProgressName(params string[] names)
    {
        Names = names;
    }
}
