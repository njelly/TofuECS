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
            public readonly DynamicBody2D* Body;
            public readonly Transform2D* Transform;

            public BodyInfo(int entity, DynamicBody2D* body, Transform2D* transform)
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
            Integrate(f);
            Broadphase();
            NarrowPhase();
            ResolveCollisions();
        }

        private void Integrate(Frame f)
        {
            var dynamicBody2dIterator = f.GetIterator<DynamicBody2D>();
            var bodies = stackalloc BodyInfo[dynamicBody2dIterator.Count];
            var bodiesIndex = 0;
            while (dynamicBody2dIterator.NextUnsafe(out var entityId, out var dynamicBody2d))
            {
                // integrate the forces
                if (dynamicBody2d->ForcesNextIndex > 0)
                {
                    dynamicBody2d->Velocity += dynamicBody2d->SumForces() / new Fix64(dynamicBody2d->ForcesNextIndex) * f.DeltaTime;
                    dynamicBody2d->ClearForces();
                }

                if (!f.TryGetComponentUnsafe<Transform2D>(entityId, out var transform2d))
                    continue;
                
                bodies[bodiesIndex++] = new BodyInfo(entityId, dynamicBody2d, transform2d);

                // move the transform
                transform2d->PrevPosition = transform2d->Position;
                if (transform2d != null)
                    transform2d->Position += dynamicBody2d->Velocity * f.DeltaTime;
            }
            
            // store the bodies in memory
            if (_bodiesLength < bodiesIndex && _bodies != null)
                Marshal.FreeHGlobal((IntPtr)_bodies);

            var size = Marshal.SizeOf<BodyInfo>() * bodiesIndex;
            _bodies = (BodyInfo*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(bodies, _bodies, size, size);
            _bodiesLength = bodiesIndex;
        }

        private void Broadphase()
        {
            // sort based on x position, so we don't double check bounding box collisions
            UnmanagedQuickSort.Sort(_bodies, _bodiesLength,
                (a, b) => a.Transform->Position.X < b.Transform->Position.X);
            
            var collisionPairs = stackalloc CollisionPair[_bodiesLength * _bodiesLength - _bodiesLength];
            var collisionPairsIndex = 0;

            // find all unique *POTENTIAL* collisions
            for (var i = 0; i < _bodiesLength; i++)
            {
                var boundingBoxA = _bodies[i].Body->GetColliderShape(*_bodies[i].Transform).BoundingBox;
                for (var j = 0; j < _bodiesLength; j++)
                {
                    if (i == j)
                        continue;

                    // skip B vs. A check when A vs. B check has already occured
                    if (_bodies[j].Transform->Position.X < _bodies[i].Transform->Position.X)
                        continue;

                    var boundingBoxB = _bodies[j].Body->GetColliderShape(*_bodies[j].Transform).BoundingBox;
                    if (!boundingBoxA.IntersectsAABB(boundingBoxB))
                        continue;

                    collisionPairs[collisionPairsIndex++] = new CollisionPair(i, j);
                }
            }

            // store the potential collisions in memory
            if (_collisionsLength < collisionPairsIndex && _collisions != null)
                Marshal.FreeHGlobal((IntPtr)_collisions);
            
            _collisionsLength = collisionPairsIndex;

            // no collisions are possible
            if (_collisionsLength == 0)
                return;

            var size = Marshal.SizeOf<CollisionPair>() * collisionPairsIndex;
            _collisions = (CollisionPair*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(collisionPairs, _collisions, size, size);
        }

        private void NarrowPhase()
        {
            var confirmedCollisions = stackalloc CollisionPair[_collisionsLength];
            var confirmedCollisionsLength = 0;
            for (var i = 0; i < _collisionsLength; i++)
            {
                var bodyA = _bodies[_collisions[i].BodyA];
                var bodyB = _bodies[_collisions[i].BodyB];
                
                if(!bodyA.Body->GetColliderShape(*bodyA.Transform).Intersects(bodyB.Body->GetColliderShape(*bodyB.Transform)))
                    continue;

                confirmedCollisions[confirmedCollisionsLength++] = _collisions[i];
            }
            
            var size = Marshal.SizeOf<CollisionPair>() * confirmedCollisionsLength;
            Buffer.MemoryCopy(confirmedCollisions, _collisions, size, size);
            _collisionsLength = confirmedCollisionsLength;
        }
        
        private void ResolveCollisions()
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