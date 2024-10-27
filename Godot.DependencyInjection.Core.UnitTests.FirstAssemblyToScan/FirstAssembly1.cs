﻿using Godot.DependencyInjection.Attributes;

namespace Godot.DependencyInjection.Core.UnitTests.FirstAssemblyToScan;

public class FirstAssembly1
{
    [Inject]
    public Guid GuidPublic { get; set; }
    [InjectMembers]
    public FirstAssembly2 FirstAssembly2Public { get; set; } = null!;

    [Inject]
    protected Guid GuidProtected { get; set; }
    [InjectMembers]
    protected FirstAssembly2 FirstAssembly2Protected { get; set; } = null!;
    [InjectMembers]

    [Inject]
    private Guid GuidPrivate { get; set; }
    [InjectMembers]
    private FirstAssembly2 FirstAssembly2Private { get; set; } = null!;

    [Inject]
    public Guid guidPublic;

    [Inject]
    protected Guid guidProtected;

    [Inject]
    private Guid guidPrivate;


    [Inject]
    public void InjectPublic(Guid guid)
    {
    }
    [Inject]
    protected void InjectProtected(Guid guid)
    {
    }
    [Inject]
    private void InjectPrivate(Guid guid)
    {
    }
}