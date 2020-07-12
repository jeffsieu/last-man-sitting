using Godot;

public class Enemy : HealthEntity
{
    public Vector3 Velocity;
    protected SpatialMaterial material;
    protected Player player;

    protected float stunnedDuration = 0;
    protected StateManager stateManager;

    public Enemy()
    {
        MaxHealth = 100;
    }

    public override void _Ready()
    {
        base._Ready();
        CollisionLayer = ColLayer.Enemies;
        SetCollisionMaskBit(ColLayer.Bit.Enemies, true);
        material = new SpatialMaterial()
        {
            AlbedoColor = Colors.White,
            FlagsTransparent = false
        };

        MeshInstance cylinder = GetNode<MeshInstance>("MeshInstance");
        cylinder.SetSurfaceMaterial(0, material);

        player = GetTree().Root.GetNode<Spatial>("Level").GetNode<Player>("Player");

        RegenerationRate = 0.0f;
    }

    public override void _Process(float delta)
    {
        Color baseColor = Colors.Red;
        material.AlbedoColor = baseColor;
        UpdateStatusBars(delta);
    }

    protected Vector3 GetNextPointToTarget()
    {
        return GlobalTransform.origin + GlobalTransform.origin.DirectionTo(player.GlobalTransform.origin);
    }

    protected virtual void IdleState(float delta, float elapsedDelta)
    {
    }

    protected virtual void StunnedState(float delta, float elapsedDelta)
    {
        // Remain on the spot until stun duration is over
        if (elapsedDelta >= stunnedDuration)
        {
            stateManager.GoTo(IdleState);
        }
    }

    public virtual void Stun(float duration)
    {
        stunnedDuration = duration;
        stateManager.GoTo(StunnedState);
    }

    protected override void Die()
    {
        base.Die();
        CollisionLayer = 0;
        material.FlagsTransparent = true;
        Tween tween = new Tween();
        AddChild(tween);
        tween.InterpolateProperty(material, "albedo_color:a", 0.5f, 0, 1.0f);
        tween.InterpolateCallback(this, 1.0f, "queue_free");
        tween.Start();
    }
}
