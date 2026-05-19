using System;
using UnityEngine;

namespace BoatGame.Economy
{
    [DisallowMultipleComponent]
    public sealed class ResourceInventory : MonoBehaviour
    {
        [Serializable]
        private struct ResourceStack
        {
            public ResourceType type;
            [Min(0)] public int amount;
        }

        [Header("Capacity")]
        [SerializeField, Min(1)] private int baseCapacityPerResource = 24;
        [SerializeField, Min(0)] private int capacityBonus;

        [Header("Starting Resources")]
        [SerializeField] private ResourceStack[] resources =
        {
            new ResourceStack { type = ResourceType.Wood, amount = 8 },
            new ResourceStack { type = ResourceType.Fabric, amount = 4 },
            new ResourceStack { type = ResourceType.Rope, amount = 5 },
            new ResourceStack { type = ResourceType.Iron, amount = 3 }
        };

        public int CapacityPerResource => baseCapacityPerResource + capacityBonus;

        private void OnValidate()
        {
            baseCapacityPerResource = Mathf.Max(1, baseCapacityPerResource);
            capacityBonus = Mathf.Max(0, capacityBonus);
            EnsureStacks();
        }

        private void Awake()
        {
            EnsureStacks();
            ClampAll();
        }

        public int Get(ResourceType type)
        {
            EnsureStacks();
            return resources[(int)type].amount;
        }

        public bool CanAdd(ResourceType type, int amount)
        {
            amount = Mathf.Max(0, amount);
            return Get(type) + amount <= CapacityPerResource;
        }

        public int Add(ResourceType type, int amount)
        {
            EnsureStacks();
            amount = Mathf.Max(0, amount);
            int index = (int)type;
            int room = Mathf.Max(0, CapacityPerResource - resources[index].amount);
            int added = Mathf.Min(room, amount);
            resources[index].amount += added;
            return added;
        }

        public bool CanSpend(ResourceType type, int amount)
        {
            return amount <= 0 || Get(type) >= amount;
        }

        public bool CanSpend(SerializableResourceAmount[] costs)
        {
            if (costs == null)
            {
                return true;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                if (!CanSpend(costs[i].type, costs[i].amount))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySpend(ResourceType type, int amount)
        {
            EnsureStacks();
            amount = Mathf.Max(0, amount);
            int index = (int)type;
            if (resources[index].amount < amount)
            {
                return false;
            }

            resources[index].amount -= amount;
            return true;
        }

        public bool TrySpend(SerializableResourceAmount[] costs)
        {
            if (!CanSpend(costs))
            {
                return false;
            }

            if (costs == null)
            {
                return true;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                TrySpend(costs[i].type, costs[i].amount);
            }

            return true;
        }

        public void AddDebugBundle()
        {
            Add(ResourceType.Wood, 12);
            Add(ResourceType.Fabric, 8);
            Add(ResourceType.Rope, 8);
            Add(ResourceType.Iron, 6);
        }

        public void SetCapacityBonus(int bonus)
        {
            capacityBonus = Mathf.Max(0, bonus);
            ClampAll();
        }

        public string GetSummary()
        {
            return $"Bois {Get(ResourceType.Wood)}/{CapacityPerResource}  Tissu {Get(ResourceType.Fabric)}/{CapacityPerResource}  Corde {Get(ResourceType.Rope)}/{CapacityPerResource}  Fer {Get(ResourceType.Iron)}/{CapacityPerResource}";
        }

        private void EnsureStacks()
        {
            int count = Enum.GetValues(typeof(ResourceType)).Length;
            if (resources == null || resources.Length != count)
            {
                ResourceStack[] replacement = new ResourceStack[count];
                for (int i = 0; i < count; i++)
                {
                    replacement[i].type = (ResourceType)i;
                    replacement[i].amount = GetExistingAmount((ResourceType)i);
                }

                resources = replacement;
            }

            for (int i = 0; i < resources.Length; i++)
            {
                resources[i].type = (ResourceType)i;
                resources[i].amount = Mathf.Max(0, resources[i].amount);
            }
        }

        private int GetExistingAmount(ResourceType type)
        {
            if (resources == null)
            {
                return 0;
            }

            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i].type == type)
                {
                    return Mathf.Max(0, resources[i].amount);
                }
            }

            return 0;
        }

        private void ClampAll()
        {
            EnsureStacks();
            for (int i = 0; i < resources.Length; i++)
            {
                resources[i].amount = Mathf.Clamp(resources[i].amount, 0, CapacityPerResource);
            }
        }
    }
}
