using Godot;
using System;
using Godotvoxelscavenger.Core;
using Godotvoxelscavenger.Extensions;
using Godotvoxelscavenger.Voxel.World;
using Godotvoxelscavenger.Voxel;

//This script was made for my godot voxel game in order to raycast with little processing power using some properties
//of voxels in relation to the cartesian grid using the DDA raycasting algorithm.

namespace Godotvoxelscavenger.Voxel.Utilities
{
    public static class VoxelRaycastUtility
    {
        public static VoxelRaycast Raycast(Vector3 start, Vector3 forward, float length) 
        {
            //Step is the direction each direction changes by for each single step in that voxel direction
            //where LengthOfX(1) = Sqrt(1^2 + dy/dx^2 + dz/dx^2)
            Vector3 unitStepSize = new Vector3(
                Mathf.Sqrt(1 + Mathf.Pow(forward.y/forward.x,2) + Mathf.Pow(forward.z/forward.x,2)),
                Mathf.Sqrt(1 + Mathf.Pow(forward.x/forward.y,2) + Mathf.Pow(forward.z/forward.y,2)),
                Mathf.Sqrt(1 + Mathf.Pow(forward.x/forward.z,2) + Mathf.Pow(forward.y/forward.z,2))
            );

            //In these cases the value would be divide by zero which isnt a number so the step size should be set to manual large values.
            if(forward.x == 0)
            {
                unitStepSize.x = float.MaxValue;
            }
            if(forward.y == 0)
            {
                unitStepSize.y = float.MaxValue;
            }
            if(forward.z == 0)
            {
                unitStepSize.z = float.MaxValue;
            }

            Vector3i step        = new Vector3i();
            Vector3i mapCheck    = start.ToVector3iFloored();

            //Cooresponds how far down the hypotenuse each direction is
            Vector3 coordinateRayLengths = new Vector3();
            Vector3i VoxelAdjacentPos = new Vector3i();
            Vector3 HitPos = new Vector3();
            VoxelFaceDirection faceDirection = VoxelFaceDirection.None;
            
            //Check if the block is in the current position
            VoxelDef def = Find.ActiveVoxelMap.Get(Find.ActiveVoxelWorld.WorldDataNode.WorldData.GetVoxelAtWorldPosition(new Vector3i(mapCheck.x,mapCheck.y,mapCheck.z)));
            if( def.id != VoxelDefsOf.Air.id && def.id != VoxelDefsOf.Null.id)
            {
                HitPos = mapCheck;
                return new VoxelRaycast(true,0,HitPos,mapCheck,def,faceDirection,mapCheck);
            }

            if(length == 0)
            {
                return  new VoxelRaycast(false,length,HitPos,mapCheck,VoxelDefsOf.Air,VoxelFaceDirection.Front,mapCheck);
            }
            //Initialize the starting ray length to the closest edge and the step direction for each coordinate direction
            if(forward.x < 0)
            {
                step.x = - 1;
                coordinateRayLengths.x = (start.x - mapCheck.x) * unitStepSize.x;
            }
            else
            {
                step.x = 1;
                coordinateRayLengths.x = (mapCheck.x + 1 - start.x) * unitStepSize.x; 
            }

            if(forward.y < 0)
            {
                step.y = - 1;
                coordinateRayLengths.y = (start.y - mapCheck.y) * unitStepSize.y;
            }
            else
            {
                step.y = 1;
                coordinateRayLengths.y = (mapCheck.y + 1 - start.y) * unitStepSize.y; 
            }

            if(forward.z < 0)
            {
                step.z = - 1;
                coordinateRayLengths.z = (start.z - mapCheck.z) * unitStepSize.z;
            }
            else
            {
                step.z = 1;
                coordinateRayLengths.z = (mapCheck.z + 1 - start.z) * unitStepSize.z; 
            }

            float distance = 0f;
            //After initialization walk the directions up the hypotenuse until a voxel is hit
            while(distance < length)
            {
                if(coordinateRayLengths.x < coordinateRayLengths.y && coordinateRayLengths.x < coordinateRayLengths.z)
                {
                    mapCheck.x += step.x;
                    distance = coordinateRayLengths.x;
                    coordinateRayLengths.x += unitStepSize.x;
                    coordinateRayLengths.x = Mathf.Min(coordinateRayLengths.x,length);
                    if(step.x > 0) 
                    {
                        faceDirection = VoxelFaceDirection.Left;
                    }
                    else
                    {
                        faceDirection = VoxelFaceDirection.Right;
                    }
                }
                else if(coordinateRayLengths.y < coordinateRayLengths.z && coordinateRayLengths.y < coordinateRayLengths.x)
                {
                    mapCheck.y += step.y;
                    distance = coordinateRayLengths.y;
                    coordinateRayLengths.y += unitStepSize.y;
                    coordinateRayLengths.y = Mathf.Min(coordinateRayLengths.y,length);
                    if(step.y > 0)
                    {
                        faceDirection = VoxelFaceDirection.Bottom;
                    }
                    else
                    {
                        faceDirection = VoxelFaceDirection.Top;
                    }
                }
                else
                {
                    mapCheck.z += step.z;
                    distance = coordinateRayLengths.z;
                    coordinateRayLengths.z += unitStepSize.z;
                    coordinateRayLengths.z = Mathf.Min(coordinateRayLengths.z,length);
                    if(step.z > 0)
                    {
                        faceDirection = VoxelFaceDirection.Front;
                    }
                    else
                    {
                        faceDirection = VoxelFaceDirection.Back;
                    }
                }
                def = Find.ActiveVoxelMap.Get(Find.ActiveVoxelWorld.WorldDataNode.WorldData.GetVoxelAtWorldPosition(new Vector3i(mapCheck.x,mapCheck.y,mapCheck.z)));
                if( def.id != VoxelDefsOf.Air.id && def.id != VoxelDefsOf.Null.id)
                {
                    distance = Mathf.Min(distance,length);
                    //Offset by the face normals for negative directions in order to "step into" the block that should be selected rather than the point of collision
                    switch(faceDirection)
                    {
                        case VoxelFaceDirection.Front:
                        VoxelAdjacentPos = mapCheck + Vector3i.Forward;
                        break;
                        case VoxelFaceDirection.Left:
                        VoxelAdjacentPos = mapCheck + Vector3i.Left;
                        break;
                        case VoxelFaceDirection.Bottom:
                        VoxelAdjacentPos = mapCheck + Vector3i.Down;
                        break;
                        case VoxelFaceDirection.Top:
                        VoxelAdjacentPos = mapCheck + Vector3i.Up;
                        break;
                        case VoxelFaceDirection.Right:
                        VoxelAdjacentPos = mapCheck + Vector3i.Right;
                        break;
                        case VoxelFaceDirection.Back:
                        VoxelAdjacentPos = mapCheck + Vector3i.Back;
                        break;
                    }
                    HitPos = start + forward * distance;
                    return new VoxelRaycast(true,distance,HitPos,mapCheck,def,faceDirection,VoxelAdjacentPos);
                }
            }
            //If there is no collision just return the position of the length from the start
            return new VoxelRaycast(false,length,HitPos,mapCheck,VoxelDefsOf.Air,VoxelFaceDirection.Front,mapCheck);
        }
    }

    public struct VoxelRaycast
    {
        public bool Hit;
        public float Length;
        public Vector3 HitPosition;
        public Vector3i VoxelPosition;
        public Vector3i VoxelAdjacentPosition;
        public VoxelDef Voxel;
        public VoxelFaceDirection FaceDirection;
        public VoxelRaycast(bool Hit,float Length, Vector3 HitPosition, Vector3i VoxelPosition, VoxelDef Voxel, VoxelFaceDirection FaceDirection, Vector3i VoxelAdjacentPosition)
        {
            this.Length = Length;
            this.Hit = Hit;
            this.HitPosition = HitPosition;
            this.VoxelPosition = VoxelPosition;
            this.Voxel = Voxel;
            this.FaceDirection = FaceDirection;
            this.VoxelAdjacentPosition = VoxelAdjacentPosition;
        }
    }
}
