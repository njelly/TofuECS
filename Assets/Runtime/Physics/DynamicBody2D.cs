﻿using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Physics
{
    public unsafe struct DynamicBody2D
    {
        public FixVector2 Velocity;
        public Fix64 AngularVelocity;
        public Fix64 Mass;
        public Collider Collider;
        public bool IsAsleep;
        internal FixVector2* Forces;
        internal int ForcesNextIndex;
        internal int ForcesLength;

        public void AddForce(FixVector2 force)
        {
            if (ForcesNextIndex >= ForcesLength)
            {
                var prevLength = Marshal.SizeOf(typeof(FixVector2)) * ForcesLength;
                ForcesLength++;
                var newArray = (FixVector2*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FixVector2)) * ForcesLength);
                Buffer.MemoryCopy(Forces, newArray, prevLength, prevLength);
                Dispose();
                Forces = newArray;
            }

            Forces[ForcesNextIndex++] = force;
        }

        public void AddImpulse(FixVector2 impulse) => Velocity += impulse;

        internal void Init()
        {
            ForcesLength = 1;
            Forces = (FixVector2*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FixVector2)) * ForcesLength);
            ForcesNextIndex = 0;
        }

        internal void ClearForces()
        {
            ForcesNextIndex = 0;
        }

        internal FixVector2 SumForces()
        {
            var toReturn = FixVector2. Zero;
            for (var i = 0; i < ForcesNextIndex; i++)
                toReturn += Forces[i];

            return toReturn;
        }

        internal void Dispose()
        {
            if(Forces != null)
                Marshal.FreeHGlobal((IntPtr)Forces);
        }
    }

    public enum ShapeType
    {
        None,
        AABB,
        Circle,
    }

    public struct Collider
    {
        public ShapeType ShapeType;
        public Fix64 CircleRadius;
        public FixVector2 BoxExtents;
        public FixAABB BoundingBox;
        public bool IsTrigger;
    }
}