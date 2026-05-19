using System;
using UnityEngine;

namespace BoatGame.Economy
{
    [Serializable]
    public struct SerializableResourceAmount
    {
        public ResourceType type;
        [Min(0)] public int amount;

        public SerializableResourceAmount(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = Mathf.Max(0, amount);
        }
    }
}
