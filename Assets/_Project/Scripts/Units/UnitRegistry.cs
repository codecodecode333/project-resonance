using System.Collections.Generic;
using Riftchord.Core;

namespace Riftchord.Units
{
    public sealed class UnitRegistry
    {
        private readonly Dictionary<EntityId, UnitState> _unitsById =
            new Dictionary<EntityId, UnitState>();

        public bool Register(UnitState unit)
        {
            if (unit == null || _unitsById.ContainsKey(unit.Id))
            {
                return false;
            }

            _unitsById.Add(unit.Id, unit);
            return true;
        }

        public bool TryGetUnit(EntityId id, out UnitState unit)
        {
            return _unitsById.TryGetValue(id, out unit);
        }
    }
}
