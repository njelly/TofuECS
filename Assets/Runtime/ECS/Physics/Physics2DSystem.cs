using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BulletSharp.SoftBody;
using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS.Physics
{
    internal unsafe class Physics2DSystem : ISystem
    {
        private readonly struct CollisionPair
        {
            public readonly int EntityA;
            public readonly int EntityB;

            public CollisionPair(int entityA, int entityB)
            {
                EntityA = entityA;
                EntityB = entityB;
            }

            public override bool Equals(object obj)
            {
                if (obj == null) 
                    return false;
                
                var otherPair = (CollisionPair)obj;
                return otherPair.EntityA == EntityA && otherPair.EntityB == EntityB;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (EntityA * 397) ^ EntityB;
                }
            }
        }

        private readonly struct BodyInfo
        {
            public readonly int Entity;
            public readonly DynamicBody2D Body;
            public readonly Transform2D Transform;

            public BodyInfo(int entity, DynamicBody2D body, Transform2D transform)
            {
                Entity = entity;
                Body = body;
                Transform = transform;
            }
        }

        private CollisionPair* _potentialCollisions;
        private CollisionPair* _confirmedCollisions;
        private int _potentialCollisionsLength;
        private int _confirmedCollisionsLength;
        
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            Broadphase(f);
            NarrowPhase(f);

            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2dIterator.NextUnsafe(out var entityId, out var dynamicBody2d))
            {
                if(f.TryGetComponentUnsafe<Transform2D>(entityId, out var transform2d))
                    transform2d->PrevPosition = transform2d->Position;

                if (dynamicBody2d->ForcesNextIndex <= 0)
                    continue;

                // integrate the forces
                dynamicBody2d->Velocity += dynamicBody2d->SumForces() / new Fix64(dynamicBody2d->ForcesNextIndex) * f.DeltaTime;
                dynamicBody2d->ClearForces();

                // move the transform
                if (transform2d != null)
                    transform2d->Position += dynamicBody2d->Velocity * f.DeltaTime;
            }
        }

        private void Broadphase(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            var bodies = stackalloc BodyInfo[dynamicBody2dIterator.Count];
            var bodiesIndex = 0;
            var collisionPairs = stackalloc CollisionPair[dynamicBody2dIterator.Count * dynamicBody2dIterator.Count - dynamicBody2dIterator.Count];
            var collisionPairsIndex = 0;

            while (dynamicBody2dIterator.Next(out var entityId, out var dynamicBody2D))
            {
                if (f.TryGetComponent<Transform2D>(entityId, out var transform2D))
                    bodies[bodiesIndex++] = new BodyInfo(entityId, dynamicBody2D, transform2D);
            }

            // sort based on x position, so we don't double check bounding box collisions
            UnmanagedQuickSort.Sort(bodies, 0, bodiesIndex,
                (a, b) => a.Transform.Position.X < b.Transform.Position.X);

            // find all unique *POTENTIAL* collisions
            for (var i = 0; i < bodiesIndex; i++)
            {
                var boundingBoxA = bodies[i].Body.GetColliderShape(bodies[i].Transform).BoundingBox;
                for (var j = 0; j < bodiesIndex; j++)
                {
                    if (i == j)
                        continue;

                    // skip B vs. A check when A vs. B check has already occured
                    if (bodies[j].Transform.Position.X < bodies[i].Transform.Position.X)
                        continue;

                    var boundingBoxB = bodies[j].Body.GetColliderShape(bodies[j].Transform).BoundingBox;
                    if (!boundingBoxA.IntersectsAABB(boundingBoxB))
                        continue;

                    collisionPairs[collisionPairsIndex++] = new CollisionPair(bodies[i].Entity, bodies[j].Entity);
                }
            }

            // no collisions are possible
            if (collisionPairsIndex == 0)
                return;

            if (_potentialCollisionsLength < collisionPairsIndex && _potentialCollisions != null)
                Marshal.FreeHGlobal((IntPtr)_potentialCollisions);

            var size = Marshal.SizeOf<CollisionPair>() * collisionPairsIndex;
            _potentialCollisions = (CollisionPair*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(collisionPairs, _potentialCollisions, size, size);
            _confirmedCollisionsLength = collisionPairsIndex;
        }

        private void NarrowPhase(Frame f)
        {
            
        }

        public void Dispose(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2dIterator.NextUnsafe(out _, out var dynamicBody2D))
                dynamicBody2D->Dispose();
        }
    }
}