using UnityEngine;

namespace BoatGame.Economy
{
    [DisallowMultipleComponent]
    public sealed class PlayerCurrency : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField, Min(0)] private int startingCoins = 120;
        [SerializeField, Min(0)] private int coins;

        public int Coins => coins;

        private void Awake()
        {
            if (coins <= 0 && startingCoins > 0)
            {
                coins = startingCoins;
            }
        }

        private void OnValidate()
        {
            startingCoins = Mathf.Max(0, startingCoins);
            coins = Mathf.Max(0, coins);
        }

        public bool CanSpend(int amount)
        {
            return amount <= 0 || coins >= amount;
        }

        public bool TrySpend(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (coins < amount)
            {
                return false;
            }

            coins -= amount;
            return true;
        }

        public void AddCoins(int amount)
        {
            coins = Mathf.Max(0, coins + amount);
        }

        public void Configure(int amount)
        {
            startingCoins = Mathf.Max(0, amount);
            coins = startingCoins;
        }
    }
}
