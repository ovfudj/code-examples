using Godot;
using System.Collections.Generic;
using Godotvoxelscavenger.Extensions;
using Godotvoxelscavenger.Core;
using Godotvoxelscavenger.Voxel;
using Godotvoxelscavenger.Voxel.Utilities;
using Godotvoxelscavenger.Voxel.World;

//Custom physics world for my godot voxel game

//Voxel portion performs dda raycast at each block point disection of the AABB in order to calculate whether or not it is over a voxel
//and move it only the amount of velocity it needs to collide
//After words an SAT based collision system is used to detect whether or not the body is in another entity, if it is, it will be pushed
//our by a minimum translation vector, so it is no longer colliding.
//Currently does not support collision from rotating oriented bounding boxes as 
//moving entities do not support rotation right now, Although the current system could ceratinly support it with some modifications.

public partial class VoxelPhysicsWorldNode : Node
{
    VoxelWorldData WorldData;
    public void AddBody(VoxelPhysicsBodyNode body)
    {
        Log.Message($"Added body {body}");
        bodies.Add(body);
    }
    public void RemoveBody(VoxelPhysicsBodyNode body)
    {
        bodies.Remove(body);
    }
    List<VoxelPhysicsBodyNode> bodies = new List<VoxelPhysicsBodyNode>();
    Vector3 Gravity = new Vector3(0,-9.8f,0);
    public override void _Ready()
    {
        WorldData = Find.ActiveVoxelWorld.WorldDataNode.WorldData;
    }
    public override void _Process(double delta)
    {

        bodies.Each<VoxelPhysicsBodyNode>(body => 
        {
            if(!body.Static)
            {
                Vector3 position = body.GlobalPosition;
                BoxShape3D BBox = body.VoxelBounds;

                Vector3 pos;
                Vector3 TranslationVector = Vector3.Zero;
                Vector3 minimumTranslation = Vector3.Inf;

                float rightMin = float.MaxValue;
                float leftMin = float.MaxValue;
                float topMin = float.MaxValue;
                float botMin = float.MaxValue;
                float forwardMin = float.MaxValue;
                float backMin = float.MaxValue;

                bool hitRight = false;
                bool hitLeft = false;
                bool hitBot = false;
                bool hitTop = false;
                bool hitForward = false;
                bool hitBack = false;

                float xTranslate = body.Velocity.x * (float)delta;
                float yTranslate = body.Velocity.y * (float)delta;
                float zTranslate = body.Velocity.z * (float)delta;

                float tolerance = 0.001f;

                //Sends out a ray from the outer broken up shell for each of the voxels in the direction that face normal
                //that the side of the bounding box would be facing
                //Then it offsets the object by the raycast instead of the velocity. The offset is decreased by the tolerance value to
                //prevent the bounding box from being inside the collided voxel.

                //Loop from 0 to the Size of the bounding box in increments of the voxel cell size
                bool maxX = false;
                for (float i = 0; !maxX; i = Mathf.Min(i + 1, BBox.Size.x))
                {
                    bool maxY = false;
                    for (float j = 0; !maxY; j = Mathf.Min(j + 1, BBox.Size.y))
                    {
                        bool maxZ = false;
                        for (float k = 0; !maxZ; k = Mathf.Min(k + 1, BBox.Size.z))
                        {
                            pos = new Vector3(i, j, k) - BBox.Size/2f;
                            Vector3 globalPos = body.GlobalPosition + pos;
                            //only test the shell by raycasting in the direction of the normal
                            if(body.Velocity.x > 0 && i == BBox.Size.x)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Right,xTranslate);
                                if(raycast.Hit && raycast.Length <= xTranslate)
                                {
                                    hitRight = true;
                                    float offset = Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < rightMin)
                                    {
                                        rightMin = offset;
                                    }
                                }
                            }
                            if(body.Velocity.y > 0 && j == BBox.Size.y)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Up,yTranslate);
                                if(raycast.Hit && raycast.Length <= yTranslate)
                                {
                                    hitTop = true;
                                    float offset =Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < topMin)
                                    {
                                        topMin = offset;
                                    }
                                }
                            }
                            if(body.Velocity.z > 0 && k == BBox.Size.z)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Back,zTranslate);
                                if(raycast.Hit && raycast.Length <= zTranslate)
                                {
                                    hitBack = true;
                                    float offset = Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < backMin)
                                    {
                                        backMin = offset;
                                    }
                                }
                            }
                            if(body.Velocity.x < 0 && i == 0)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Left,Mathf.Abs(xTranslate));
                                if(raycast.Hit && raycast.Length <= Mathf.Abs(xTranslate))
                                {
                                    hitLeft = true;
                                    float offset = Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < leftMin)
                                    {
                                        leftMin = offset;
                                    }
                                }
                            }
                            if(body.Velocity.y < 0 && j == 0)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Down,Mathf.Abs(yTranslate));
                                if(raycast.Hit && raycast.Length <= Mathf.Abs(yTranslate))
                                {
                                    hitBot = true;
                                    float offset = Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < botMin)
                                    {
                                        botMin = offset;
                                    }
                                }
                            }
                            if(body.Velocity.z < 0 && k == 0)
                            {
                                VoxelRaycast raycast = VoxelRaycastUtility.Raycast(globalPos,Vector3.Forward,Mathf.Abs(zTranslate));
                                if(raycast.Hit && raycast.Length <= Mathf.Abs(zTranslate))
                                {
                                    hitForward = true;
                                    float offset = Mathf.Max(0,raycast.Length - tolerance);
                                    if(offset < forwardMin)
                                    {
                                        forwardMin = offset;
                                    }
                                }
                            }
                            maxZ = k == BBox.Size.z;
                        }
                        maxY = j == BBox.Size.y;
                    }
                    maxX = i == BBox.Size.x;
                }

                if(hitRight)
                {
                    xTranslate = rightMin;
                }
                if(hitLeft)
                {
                    xTranslate = -leftMin;
                }
                if(hitTop)
                {
                    yTranslate = topMin;
                }
                if(hitBot)
                {
                    yTranslate = -botMin;
                }
                if(hitBack)
                {
                    zTranslate = backMin;
                }
                if(hitForward)
                {
                    zTranslate = -forwardMin;
                }

                body.GlobalPosition += new Vector3(xTranslate,yTranslate,zTranslate);
                
                if(body.UsesGravity) body.Velocity += Gravity * (float)delta;

                //Entity MTV push out
                bodies.Each<VoxelPhysicsBodyNode>(otherBody=>
                    {
                        if(!body.Equals(otherBody))
                        {
                            MinimumTranslationVector MTV = GetEntityMTV(body,otherBody);
                            body.GlobalPosition += MTV.Axis * MTV.Magnitude;
                        }
                    }
                );
            }
        }
        );
    }
    public MinimumTranslationVector GetEntityMTV(VoxelPhysicsBodyNode targetBody, VoxelPhysicsBodyNode otherBody)
    {
        //Checks each face normal assuming this is a concave cube and searches for a plane of intersection along each axis
        //If one is found the axis is returned as a Minimum Translation Vector or the minimum amount the object must move to no longer collide
        //we look for the LARGEST of the MTVs as this is the amount the collider needs to be offset by in order to no longer collide.
        //If you want to know what going on here refer to this article : 
        //https://dyn4j.org/2010/01/sat/#:~:text=The%20Separating%20Axis%20Theorem%2C%20SAT,a%20number%20of%20other%20applications.

        BoxShape3D targetBBox = targetBody.EntityBounds;
        BoxShape3D otherBBox = otherBody.EntityBounds;
        Vector3 position = targetBody.GlobalPosition - new Vector3(targetBBox.Size.x/2,targetBBox.Size.y/2,targetBBox.Size.z/2);
        Vector3 otherPosition = otherBody.GlobalPosition - new Vector3(otherBBox.Size.x/2,otherBBox.Size.y/2,otherBBox.Size.z/2);

        //The normal with the smallest magnitude
        Vector3 smallest = Vector3.Zero;

        //The magnitude of the overlap of the smallest normal
        float overlap = float.MaxValue;
        

        //Get all the values of the vertices in the bbox
        Vector3[] vertices = {
            new Vector3(0, 0, 0) + position,
            new Vector3(targetBBox.Size.x, targetBBox.Size.y, targetBBox.Size.z) + position,
            new Vector3(targetBBox.Size.x, 0, 0) + position,
            new Vector3(targetBBox.Size.x, targetBBox.Size.y, 0) + position,
            new Vector3(targetBBox.Size.x, 0, targetBBox.Size.z) + position,
            new Vector3(0, targetBBox.Size.y, 0) + position,
            new Vector3(0, targetBBox.Size.y, targetBBox.Size.z) + position,
            new Vector3(0, 0, targetBBox.Size.z) + position
        };

        Vector3[] otherVertices =
        {
            new Vector3(0, 0, 0) + otherPosition,
            new Vector3(otherBBox.Size.x, otherBBox.Size.y, otherBBox.Size.z) + otherPosition,
            new Vector3(otherBBox.Size.x, 0, 0) + otherPosition,
            new Vector3(otherBBox.Size.x, otherBBox.Size.y, 0) + otherPosition,
            new Vector3(otherBBox.Size.x, 0, otherBBox.Size.z) + otherPosition,
            new Vector3(0, otherBBox.Size.y, 0) + otherPosition,
            new Vector3(0, otherBBox.Size.y, otherBBox.Size.z) + otherPosition,
            new Vector3(0, 0, otherBBox.Size.z) + otherPosition
        };

        Quaternion rotation = targetBody.Quaternion;
        Vector3 center = targetBody.GlobalPosition;
        Vector3[] rotatedVertices =
        {
            RotatePoint(rotation,vertices[0],center),
            RotatePoint(rotation,vertices[1],center),
            RotatePoint(rotation,vertices[2],center),
            RotatePoint(rotation,vertices[3],center),
            RotatePoint(rotation,vertices[4],center),
            RotatePoint(rotation,vertices[5],center),
            RotatePoint(rotation,vertices[6],center),
            RotatePoint(rotation,vertices[7],center)
        };

        Quaternion otherRotation = otherBody.Quaternion;
        Vector3 otherCenter = otherBody.GlobalPosition;

        Vector3[] otherRotatedVertices =
        {
            RotatePoint(otherRotation,otherVertices[0],otherCenter),
            RotatePoint(otherRotation,otherVertices[1],otherCenter),
            RotatePoint(otherRotation,otherVertices[2],otherCenter),
            RotatePoint(otherRotation,otherVertices[3],otherCenter),
            RotatePoint(otherRotation,otherVertices[4],otherCenter),
            RotatePoint(otherRotation,otherVertices[5],otherCenter),
            RotatePoint(otherRotation,otherVertices[6],otherCenter),
            RotatePoint(otherRotation,otherVertices[7],otherCenter)
        };

        Vector3[] axes =
        {
            RotatePoint(rotation,Vector3.Up,Vector3.Zero),
            RotatePoint(rotation,Vector3.Right,Vector3.Zero),
            RotatePoint(rotation,Vector3.Back,Vector3.Zero),
            RotatePoint(otherRotation,Vector3.Up,Vector3.Zero),
            RotatePoint(otherRotation,Vector3.Right,Vector3.Zero),
            RotatePoint(otherRotation,Vector3.Back,Vector3.Zero),
        };

        for(int i = 0 ; i < axes.Length; i ++)
        {
            Vector3 axis = axes[i];

            float min = axis.Dot(rotatedVertices[0]);
            float max = min;

            foreach(Vector3 vertice in rotatedVertices)
            {
                float p = axis.Dot(vertice);
                if(p < min)
                {
                    min = p;
                }
                else if(p > max)
                {
                    max = p;
                }
            }

            Interval originInterval = new Interval(min,max);

            min = axis.Dot(otherRotatedVertices[0]);
            max = min;

            foreach(Vector3 otherVertice in otherRotatedVertices)
            {
                float p = axis.Dot(otherVertice);
                if(p < min)
                {
                    min = p;
                }
                else if(p > max)
                {
                    max = p;
                }
            }

            Interval otherInterval = new Interval(min, max);
            float o = originInterval.GetOverlap(otherInterval);
            if (o == 0)
            {
                //If there is no new overlap that means there is no collision so
                //the minimum translation value is 0
                return new MinimumTranslationVector(smallest, 0);
            }
            else if (Mathf.Abs(o) < Mathf.Abs(overlap))
            {
                overlap = o;
                smallest = axis;
            }
        }
        return new MinimumTranslationVector(smallest,overlap);
    }
    public Vector3 RotatePoint(Quaternion rotation, Vector3 point, Vector3 center)
    {
        return rotation * (point - center) + center;
    }
    private class Interval
    {
        float min, max;
        public Interval(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Intersect(Interval other)
        {
            if (other.min >= min && other.min <= max)
            {
                return true;
            }
            if (other.max >= min && other.max <= max)
            {
                return true;
            }
            if (other.min <= min && other.max >= max)
            {
                return true;
            }
            return false;
        }

        public float GetOverlap(Interval other)
        {
            if (other.min >= min && other.min <= max)
            {
                return -(max - other.min);
            }
            if (other.max >= min && other.max <= max)
            {
                return other.max - min;
            }
            if (other.min <= min && other.max >= max)
            {
                return Mathf.Abs(other.max - min);
            }
            return 0;
        }
    }
    public struct MinimumTranslationVector
    {
        public Vector3 Axis;
        public float Magnitude;

        public MinimumTranslationVector(Vector3 Axis,float Magnitude)
        {
            this.Axis = Axis;
            this.Magnitude = Magnitude;
        }
    }
    private OffsetData GetVoxelOffset(Vector3 start,float length, VoxelFaceDirection dir)
    {
        length = Mathf.Abs(length);
        float distance = 0;
        float step = 1;
        bool checkForward = false;
        Vector3 direction = Vector3.Zero;

        if(dir == VoxelFaceDirection.Right)
        {
            distance = (start.x - Mathf.Floor(start.x));
            direction = Vector3.Right;
        }
        if(dir == VoxelFaceDirection.Left)
        {
            checkForward = true;
            distance = Mathf.Floor(start.x) + 1 - start.x;
            direction = Vector3.Left;
        }
        if(dir == VoxelFaceDirection.Top)
        {
            distance = (start.y - Mathf.Floor(start.y));
            direction = Vector3.Up;
        }
        if(dir == VoxelFaceDirection.Back)
        {
            distance = (start.z - Mathf.Floor(start.z));
            direction = Vector3.Back;
        }
        while(distance < length)
        {
            ushort id = WorldData.GetVoxelAtWorldPosition((start + (distance + (checkForward ? 1:0)) * direction).ToVector3iFloored());
            if(id != VoxelDefsOf.Null.id && id != VoxelDefsOf.Air.id)
            {
                return new OffsetData{Hit = true, Offset = distance};
            }
            distance = Mathf.Min(distance+step,length);
        }
        return new OffsetData{Hit = false, Offset = 0};
    }
    private struct OffsetData
    {
        public bool Hit;
        public float Offset;
    }
}
