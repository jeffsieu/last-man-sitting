using System;
using Godot;
using Godot.Collections;

public class Player : HealthEntity
{
    [Export]
    protected float MaxSpeed = 25f;

    [Export]
    protected float TurningResistance = 0.2f;

    [Export]
    protected float MaxAccelerationFactor = 0.3f;

    [Export]
    protected float MaxDecelerationFactor = 0.1f;

    [Export]
    protected float WalkingSlowFactor = 0.7f;
    [Export]
    private float Gravity = 9.8f;
    protected float RotationSpeed = 30f;

    public Vector3 Velocity;
    public bool IsMovementLocked = false;

    private float WalkingSpeed
    {
        get
        {
            return WalkingSlowFactor * MaxSpeed;
        }
    }

    private float MaxAcceleration
    {
        get
        {
            return MaxAccelerationFactor * MaxSpeed;
        }
    }
    private float MaxDeceleration
    {
        get
        {
            return MaxDecelerationFactor * MaxSpeed;
        }
    }

    private bool IsSprinting
    {
        get
        {
            return Input.IsActionPressed("movement_sprint");
        }
    }

    private bool IsPrimaryAttackPressed
    {
        get
        {
            return Input.IsActionPressed("attack_primary");
        }
    }

    private bool IsSecondaryAttackPressed
    {
        get
        {
            return Input.IsActionPressed("attack_secondary");
        }
    }

    private Vector2 mousePosition;
    private Camera camera;
    private InputMode inputMode = InputMode.Keyboard;
    private Vector3 previousFaceDirection = Vector3.Forward;
    private AimableAttack weapon;
    private AimableAttack skill;

    public Player()
    {
        MaxHealth = 100;
    }


    public override void _Ready()
    {
        base._Ready();
        camera = GetParent().GetNode<Camera>("Camera");
        weapon = GetNode<AimableAttack>("Weapon");
        skill = GetNode<AimableAttack>("Skill");

        // Move weapon to the front of the player
        weapon.Translation = Vector3.Forward * Scale.z;
    }

    public override void _Input(InputEvent @event)
    {
        // Mouse in viewport coordinates
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            mousePosition = eventMouseMotion.Position;
            inputMode = InputMode.Keyboard;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        Velocity.y -= Gravity;

        if (!IsMovementLocked)
        {
            Vector2 weightedDirection = GetWeightedMovementDirection();
            Move(weightedDirection, delta);
        }
        float angle = GetFaceDirection().AngleTo(previousFaceDirection);
        angle = Math.Min(angle, RotationSpeed * delta);
        Vector3 newFaceDirection = previousFaceDirection.Rotated(GetFaceDirection().Cross(previousFaceDirection), -angle);
        LookAt(Translation + newFaceDirection, Vector3.Up);
        previousFaceDirection = newFaceDirection.Normalized();

        // So that the global rotation of the weapon will be zero
        weapon.WeightedAttackDirection = GetWeightedAttackDirection();
        weapon.IsAttackButtonPressed = IsPrimaryAttackPressed;

        skill.WeightedAttackDirection = GetWeightedAttackDirection();
        skill.IsAttackButtonPressed = IsSecondaryAttackPressed;

        bool showSkillAimIndicator = IsSecondaryAttackPressed;
        weapon.AimIndicator.Visible = !showSkillAimIndicator;
        skill.AimIndicator.Visible = showSkillAimIndicator;

        weapon.AimIndicator.Translation = Vector3.Down;
        skill.AimIndicator.Translation = Vector3.Down;
    }

    public void Move(Vector2 weightedDirection, float delta)
    {
        float targetSpeed = IsSprinting ? MaxSpeed : WalkingSpeed;
        Vector3 targetVelocity = targetSpeed * new Vector3(weightedDirection.x, 0, weightedDirection.y);
        Vector3 targetAcceleration = targetVelocity - Velocity;
        Vector3 actualAcceleration = targetAcceleration;

        // Player trying to accelerate in the same direction
        if (targetVelocity.Dot(targetAcceleration) > 0)
        {
            if (targetAcceleration.Length() > MaxAcceleration)
            {
                actualAcceleration = targetAcceleration.Normalized() * MaxAcceleration;
            }
        }
        // Player trying to slow down/switch direction
        else
        {
            if (targetAcceleration.Length() > MaxDeceleration)
            {
                actualAcceleration = targetAcceleration.Normalized() * MaxDeceleration;
            }
        }

        // 1.0 when player is continuing forward/trying to go backward,
        // 0.0 when player is trying to turn 90 degrees
        float directionSpeedFactor = Mathf.Abs(Mathf.Cos(targetVelocity.AngleTo(Velocity)));

        // Map speed factor to [1 - resistance, 1], so player is slowed by <resistance> when he is turning 90 degrees
        directionSpeedFactor = Mathf.Lerp(1 - TurningResistance, 1.0f, directionSpeedFactor);

        // Slow player when he is turning
        actualAcceleration *= directionSpeedFactor;

        Velocity += actualAcceleration;
        Velocity = MoveAndSlide(Velocity);
    }

    public Vector2 GetWeightedAttackDirection()
    {
        if (Input.GetConnectedJoypads().Count > 0)
        {
            float horizontal = Input.GetActionStrength("aim_right") - Input.GetActionStrength("aim_left");
            float vertical = Input.GetActionStrength("aim_down") - Input.GetActionStrength("aim_up");
            Vector2 joystick = new Vector2(horizontal, vertical).Clamped(1.0f);
            if (joystick.Length() > 0)
            {
                inputMode = InputMode.Controller;
                return joystick;
            }
        }

        if (inputMode == InputMode.Keyboard)
        {
            Vector3? cursorPosition = GetCursorPointOnPlayerPlane();
            if (cursorPosition.HasValue)
            {
                Vector3 displacement = cursorPosition.Value - Translation;
                return weapon.GetWeightedAttackDirectionFromMouseDisplacement(displacement);
            }
        }
        return Vector2.Zero;
    }

    private Vector2 GetWeightedMovementDirection()
    {
        float horizontal = Input.GetActionStrength("movement_right") - Input.GetActionStrength("movement_left");
        float vertical = Input.GetActionStrength("movement_down") - Input.GetActionStrength("movement_up");
        Vector2 direction = new Vector2(horizontal, vertical);

        return direction.Clamped(1.0f);
    }

    private Vector3 GetFaceDirection()
    {
        Vector3 direction = default;

        if (Input.GetConnectedJoypads().Count > 0)
        {
            float horizontal = Input.GetActionStrength("aim_right") - Input.GetActionStrength("aim_left");
            float vertical = Input.GetActionStrength("aim_down") - Input.GetActionStrength("aim_up");
            Vector2 joystick = new Vector2(horizontal, vertical).Clamped(1.0f);
            if (joystick.Length() > 0)
            {
                inputMode = InputMode.Controller;
            }
            direction += ((Vector3.Right * joystick.x) + (Vector3.Back * joystick.y)).Normalized();
        }

        if (inputMode == InputMode.Keyboard)
        {
            Vector3? cursorPosition = GetCursorPointOnPlayerPlane();
            if (cursorPosition.HasValue)
            {
                direction = cursorPosition.Value - Translation;
            }
        }

        return direction.Normalized();
    }

    /// <summary>
    /// Returns the point above the floor that the cursor is pointing at, which is at the same height as the origin of the player.
    /// Can be used to make the player face the cursor.
    /// </summary>
    /// <returns>The cursor point on the same plane as the player.</returns>
    private Vector3? GetCursorPointOnPlayerPlane()
    {
        Vector3 cameraRayOrigin = camera.ProjectRayOrigin(mousePosition);
        Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);
        float factor = (GlobalTransform.origin.y - cameraRayOrigin.y) / rayDirection.y;
        return cameraRayOrigin + rayDirection * factor;
    }

    protected override void Die()
    {
        QueueFree();
    }
}
