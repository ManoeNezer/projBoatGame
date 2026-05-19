using UnityEngine;

namespace BoatGame.Damage
{
    [DisallowMultipleComponent]
    public sealed class RepairResource : MonoBehaviour
    {
        [Header("Repair Stock")]
        [SerializeField, Min(0)] private int startingWoodPatches = 10;
        [SerializeField, Min(1)] private int maxWoodPatches = 16;

        private int currentWoodPatches;

        public int CurrentWoodPatches => currentWoodPatches;
        public int MaxWoodPatches => maxWoodPatches;

        private void Awake()
        {
            currentWoodPatches = Mathf.Clamp(startingWoodPatches, 0, maxWoodPatches);
        }

        private void OnValidate()
        {
            maxWoodPatches = Mathf.Max(1, maxWoodPatches);
            startingWoodPatches = Mathf.Clamp(startingWoodPatches, 0, maxWoodPatches);
        }

        public bool CanSpend(int amount)
        {
            return amount <= 0 || currentWoodPatches >= amount;
        }

        public bool TrySpend(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (currentWoodPatches < amount)
            {
                return false;
            }

            currentWoodPatches -= amount;
            return true;
        }

        public void AddWoodPatches(int amount)
        {
            currentWoodPatches = Mathf.Clamp(currentWoodPatches + Mathf.Max(0, amount), 0, maxWoodPatches);
        }

        public void SetMaxWoodPatches(int maximumAmount, bool fillAddedCapacity)
        {
            int previousMax = maxWoodPatches;
            maxWoodPatches = Mathf.Max(1, maximumAmount);
            if (fillAddedCapacity && maxWoodPatches > previousMax)
            {
                currentWoodPatches += maxWoodPatches - previousMax;
            }

            currentWoodPatches = Mathf.Clamp(currentWoodPatches, 0, maxWoodPatches);
        }

        public void Configure(int startingAmount, int maximumAmount)
        {
            maxWoodPatches = Mathf.Max(1, maximumAmount);
            startingWoodPatches = Mathf.Clamp(startingAmount, 0, maxWoodPatches);
            currentWoodPatches = startingWoodPatches;
        }
    }
}
