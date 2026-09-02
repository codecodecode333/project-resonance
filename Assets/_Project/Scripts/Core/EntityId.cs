using System;

namespace ProjectResonance.Core
{
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public EntityId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Entity ID must be greater than zero.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(EntityId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"EntityId({Value})";
        }

        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityId left, EntityId right)
        {
            return !left.Equals(right);
        }
    }
}
