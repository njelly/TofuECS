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
            public readonly int BodyA;
            public readonly int BodyB;

            public CollisionPair(int bodyA, int bodyB)
            {
                BodyA = bodyA;
                BodyB = bodyB;
            }

            public override bool Equals(object obj)
            {
                if (obj == null) 
                    return false;
                
                var otherPair = (CollisionPair)obj;
                return otherPair.BodyA == BodyA && otherPair.BodyB == BodyB;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (BodyA * 397) ^ BodyB;
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

        private BodyInfo* _bodies;
        private CollisionPair* _collisions;
        private int _bodiesLength;
        private int _collisionsLength;
        
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

                    collisionPairs[collisionPairsIndex++] = new CollisionPair(i, j);
                }
            }

            // no collisions are possible
            if (collisionPairsIndex == 0)
                return;
            
            // store the bodies in memory
            if (_bodiesLength < bodiesIndex && _bodies != null)
                Marshal.FreeHGlobal((IntPtr)_bodies);

            var size = Marshal.SizeOf<BodyInfo>() * collisionPairsIndex;
            _bodies = (BodyInfo*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(bodies, _bodies, size, size);
            _bodiesLength = bodiesIndex;

            // store the potential collisions in memory
            if (_collisionsLength < collisionPairsIndex && _collisions != null)
                Marshal.FreeHGlobal((IntPtr)_collisions);

            size = Marshal.SizeOf<CollisionPair>() * collisionPairsIndex;
            _collisions = (CollisionPair*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(collisionPairs, _collisions, size, size);
            _collisionsLength = collisionPairsIndex;
        }

        private void NarrowPhase(Frame f)
        {
            var confirmedCollisions = stackalloc CollisionPair[_collisionsLength];
            var confirmedCollisionsLength = 0;
            for (var i = 0; i < _collisionsLength; i++)
            {
                var bodyA = _bodies[_collisions[i].BodyA];
                var bodyB = _bodies[_collisions[i].BodyB];
                
                if(!bodyA.Body.GetColliderShape(bodyA.Transform).Intersects(bodyB.Body.GetColliderShape(bodyB.Transform)))
                    continue;

                confirmedCollisions[confirmedCollisionsLength++] = _collisions[i];
            }
            
            var size = Marshal.SizeOf<CollisionPair>() * confirmedCollisionsLength;
            Buffer.MemoryCopy(confirmedCollisions, _collisions, size, size);
            _collisionsLength = confirmedCollisionsLength;
        }

        public void Dispose(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            while (dynamicBody2dIterator.NextUnsafe(out _, out var dynamicBody2D))
                dynamicBody2D->Dispose();
        }
    }
}