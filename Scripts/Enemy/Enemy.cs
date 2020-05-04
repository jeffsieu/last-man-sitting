using Godot;
using System;
using System.Collections.Generic;

public class Enemy : HealthEntity
{
    public Vector3 Velocity;
    private readonly float gravity = 4.85f;
    private SpatialMaterial material;

    public Enemy()
    {
        MaxHealth = 100;
    }

    public override void _Ready()
    {
        base._Ready();
        material = new SpatialMaterial()
        {
            AlbedoColor = Colors.White,
            FlagsTransparent = true
        };

        CSGCylinder cylinder = GetNode<CSGCylinder>("CSGCylinder");
        cylinder.Material = material;
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        Velocity.x *= 0.9f;
        Velocity.y -= gravity;
        Velocity.z *= 0.9f;
        Velocity = MoveAndSlide(Velocity);
        Color baseColor = Colors.Red.LinearInterpolate(Colors.Blue, GetPercentLeft<MarkStatus>());
        Color healthColor = baseColor.LinearInterpolate(Colors.White, 1 - Health / 100);
        material.AlbedoColor = healthColor;
    }

    protected override void Die()
    {
        Tween tween = new Tween();
        AddChild(tween);
        Color initialColor = material.AlbedoColor;
        initialColor.a = 0.5f;
        Color finalColor = material.AlbedoColor;
        finalColor.a = 0;
        tween.InterpolateProperty(material, "albedo_color", initialColor, finalColor, 1.0f);
        tween.InterpolateCallback(this, 1.0f, "queue_free");
        tween.Start();
    }
}
