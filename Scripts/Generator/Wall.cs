using Godot;
using System;

public class Wall : StaticBody
{
    public Wall(Vector2 position, Vector2 dimensions, Material material)
    {
        Mesh wallMesh = new CubeMesh
        {
            Size = Vector3.One
        };
        MeshInstance meshInstance = new MeshInstance
        {
            Mesh = wallMesh
        };
        meshInstance.SetSurfaceMaterial(0, material);

        CollisionShape collisionShape = new CollisionShape
        {
            Shape = new BoxShape
            {
                Extents = 0.5f * Vector3.One
            }
        };
        AddChild(meshInstance);
        AddChild(collisionShape);

        Scale = new Vector3(dimensions.x, 3, dimensions.y);
        Translation = new Vector3(position.x + dimensions.x / 2, 1, position.y + dimensions.y / 2);
    }
}