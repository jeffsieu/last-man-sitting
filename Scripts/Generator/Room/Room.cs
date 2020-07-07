using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Room : LevelRegion
{
    private MapLoader mapLoader;
    private Player player;
    private readonly CSGPolygon floorMesh;

    public HashSet<Door> ConnectedDoors { get; private set; }
    public HashSet<TransparentWall> ConnectedWalls { get; private set; }
    public Vector2[] Tiles { get; private set; }
    public bool IsEmptyRoom = false;

    private readonly RandomNumberGenerator rng;
    private readonly int unitSize;
    public bool IsActive { get; private set; } = false;

    public Room(RegionShape regionShape, Vector2[] tiles, int unitSize, Material material)
    {
        RotationDegrees = new Vector3(90, 0, 0);
        Scale = unitSize * Vector3.One;
        Translation = new Vector3(0, -unitSize, 0);

        ConnectedDoors = new HashSet<Door>();
        ConnectedWalls = new HashSet<TransparentWall>();
        Tiles = tiles;
        rng = new RandomNumberGenerator();
        rng.Randomize();

        this.unitSize = unitSize;

        floorMesh = new CSGPolygon
        {
            Polygon = regionShape.MainPolygon
        };
        foreach (Vector2[] holePolygon in regionShape.HolePolygons)
        {
            CSGPolygon holeMesh = new CSGPolygon
            {
                Polygon = holePolygon,
                Operation = CSGShape.OperationEnum.Subtraction,
                Depth = 1.5f
            };
        }
        floorMesh.UseCollision = true;
        floorMesh.CollisionLayer = ColLayer.Environment;
        floorMesh.CollisionMask = ColLayer.Environment;

        SetFloorShaderParams(regionShape.MainPolygon, (ShaderMaterial)material);
        AddUserSignal("activated");
    }

    public override void _Ready()
    {
        base._Ready();
        mapLoader = GetParent<MapLoader>();
        if (!Engine.EditorHint)
            player = GetTree().Root.GetNode<Level>("Level").GetNode<Player>("Player");
        AddChild(floorMesh);
    }

    public override void _Process(float delta)
    {
        // Todo: Remove after EnemySpawner is implemented
        if (Input.IsKeyPressed((int)KeyList.H))
        {
            OpenAllConnectedDoors();
        }
        if (Input.IsKeyPressed((int)KeyList.J))
        {
            CloseAllConnectedDoors();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Contains(player))
            HideAllConnectedWalls();
        else
            UnhideAllConnectedWalls();
    }

    private void SetFloorShaderParams(Vector2[] polygon, ShaderMaterial material)
    {
        float minX = polygon[0].x, maxX = polygon[0].x;
        float minY = polygon[0].y, maxY = polygon[0].y;
        foreach (Vector2 point in polygon)
        {
            minX = Mathf.Min(point.x, minX);
            maxX = Mathf.Max(point.x, maxX);
            minY = Mathf.Min(point.y, minY);
            maxY = Mathf.Max(point.y, maxY);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // make a duplicate of the shader so that can customize the `size` uniform
        // might want to see if this costs anything? I don't think so though
        // 5 lines of shader code shouldn't cost anything
        ShaderMaterial dupMaterial = (ShaderMaterial)material.Duplicate();
        dupMaterial.SetShaderParam("size", new Vector2(width, height));
        dupMaterial.SetShaderParam("start_pos", new Vector2(maxX, maxY));
        floorMesh.Material = dupMaterial;
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            EmitSignal("activated", this);
        }
    }

    public void ConnectDoor(Door door)
    {
        ConnectedDoors.Add(door);
    }

    public void ConnectWall(TransparentWall wall)
    {
        ConnectedWalls.Add(wall);
    }

    public void OpenAllConnectedDoors()
    {
        foreach (Door door in ConnectedDoors) door.Open();
    }

    public void CloseAllConnectedDoors()
    {
        foreach (Door door in ConnectedDoors) door.Close();
    }

    public void HideAllConnectedWalls()
    {
        foreach (TransparentWall wall in ConnectedWalls) wall.HideWall();
    }

    public void UnhideAllConnectedWalls()
    {
        foreach (TransparentWall wall in ConnectedWalls) wall.UnhideWall();
    }

    public HashSet<Room> GetConnectedRooms()
    {
        HashSet<Room> rooms = new HashSet<Room>();
        foreach (Door door in ConnectedDoors)
        {
            foreach (Room room in door.ConnectedRooms)
                if (room != this)
                    rooms.Add(room);
        }
        return rooms;
    }

    public Vector3 GetRandomTileCenter()
    {
        Vector2 tile = Tiles[rng.RandiRange(0, Tiles.Length - 1)];
        return new Vector3((tile.x + 0.5f) * unitSize, 0, (tile.y + 0.5f) * unitSize) + mapLoader.GlobalTransform.origin;
    }

    public bool Contains(Spatial spatial)
    {
        if (spatial == null)
            return false;
        Vector3 localTranslation = spatial.GlobalTransform.origin - mapLoader.GlobalTransform.origin;
        int localX = (int)localTranslation.x / unitSize;
        int localY = (int)localTranslation.z / unitSize;
        return Tiles.Contains<Vector2>(new Vector2(localX, localY));
    }
}
