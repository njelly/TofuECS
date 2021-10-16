using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Entity
    {
        public int Id { get; private set; }

        private readonly Dictionary<Type, EntityComponentAssignment> _typeToComponentAssignments;
        private int _destroyedOnFrame;

        internal Entity()
        {
            Id = -1;
            _typeToComponentAssignments = new Dictionary<Type, EntityComponentAssignment>();
            _destroyedOnFrame = -1;
        }

        internal Entity(Entity copyFrom)
        {
            Id = copyFrom.Id;
            _typeToComponentAssignments =
                new Dictionary<Type, EntityComponentAssignment>(copyFrom._typeToComponentAssignments);
            _destroyedOnFrame = copyFrom._destroyedOnFrame;
        }

        internal void Recycle(int id)
        {
            Id = id;
            _typeToComponentAssignments.Clear();
            _destroyedOnFrame = -1;
        }

        internal void Recycle(Entity copyFrom)
        {
            Id = copyFrom.Id;
            
            _typeToComponentAssignments.Clear();
            foreach (var kvp in copyFrom._typeToComponentAssignments)
                _typeToComponentAssignments.Add(kvp.Key, kvp.Value);
            
            _destroyedOnFrame = copyFrom._destroyedOnFrame;
        }

        internal void Destroy(int frameNumber)
        {
            _destroyedOnFrame = frameNumber;
        }

        public bool IsDestroyed(Frame f) => Id >= 0 && f.Number > _destroyedOnFrame && _destroyedOnFrame >= 0;

        internal void AssignComponent(Type type, int frameNumber, bool isVerified, int index)
        {
            if(!_typeToComponentAssignments.TryGetValue(type, out var assignment))
            {
                assignment = new EntityComponentAssignment();
                _typeToComponentAssignments.Add(type, assignment);
            }

            if(isVerified)
            {
                assignment.VerifiedFrameNumber = frameNumber;
                assignment.RollbackIndex = index;
            }

            assignment.UnverifiedFrameNumber = frameNumber;
            assignment.CurrentIndex = index;
        }

        internal void UnassignComponent(Type type, int frameNumber, bool isVerified)
        {
            if (!_typeToComponentAssignments.TryGetValue(type, out var assignment))
                return;

            if (isVerified)
            {
                assignment.VerifiedFrameNumber = frameNumber;
                assignment.RollbackIndex = -1;
            }

            assignment.UnverifiedFrameNumber = frameNumber;
            assignment.CurrentIndex = -1;
        }
        
        internal bool TryGetComponentIndex(Type type, out int index)
        {
            if (!_typeToComponentAssignments.TryGetValue(type, out var assignment))
            {
                index = -1;
                return false;
            }

            index = assignment.CurrentIndex;
            return true;
        }

        internal void VerifyUpToFrameNumber(int frameNumber)
        {
            foreach(var assignment in _typeToComponentAssignments.Values)
            {
                if(assignment.VerifiedFrameNumber <= frameNumber)
                {
                    assignment.VerifiedFrameNumber = frameNumber;
                    assignment.RollbackIndex = assignment.CurrentIndex;
                    assignment.UnverifiedFrameNumber = frameNumber;
                }
            }
        }

        internal void RollbackToFrameNumber(int frameNumber)
        {
            foreach (var assignment in _typeToComponentAssignments.Values)
            {
                if (assignment.UnverifiedFrameNumber <= frameNumber)
                {
                    assignment.CurrentIndex = assignment.RollbackIndex;
                    assignment.UnverifiedFrameNumber = frameNumber;
                }
            }

            if (frameNumber < _destroyedOnFrame)
                _destroyedOnFrame = -1;
        }

        private class EntityComponentAssignment
        {
            public int VerifiedFrameNumber;
            public int RollbackIndex;
            public int UnverifiedFrameNumber;
            public int CurrentIndex;

            public EntityComponentAssignment()
            {
                VerifiedFrameNumber = -1;
                RollbackIndex = -1;
                UnverifiedFrameNumber = -1;
                CurrentIndex = -1;
            }
        }
    }
}