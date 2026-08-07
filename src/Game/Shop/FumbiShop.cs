using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Netsphere.Network;

namespace Netsphere.Shop
{
    internal class FumbiRollEntry
    {
        public ItemNumber ItemNumber { get; set; }
        public ItemPriceType PriceType { get; set; }
        public ItemPeriodType PeriodType { get; set; }
        public ushort Period { get; set; }
        public byte Color { get; set; }
        public int Weight { get; set; }
    }

    internal static class FumbiShop
    {
        public const uint RollCostPEN = 10000;

        private static readonly object Sync = new object();
        private static readonly Random Rng = new Random();
        private static readonly ConcurrentDictionary<Player, ulong> LastRoll = new ConcurrentDictionary<Player, ulong>();

        private static List<FumbiRollEntry> _weaponPool;
        private static List<FumbiRollEntry> _costumePool;
        private static string _builtVersion;

        public static bool HasPool
        {
            get
            {
                EnsureBuilt();
                return _weaponPool.Count + _costumePool.Count > 0;
            }
        }

        public static FumbiRollEntry Roll(bool isWeapon)
        {
            EnsureBuilt();

            var pool = isWeapon ? _weaponPool : _costumePool;
            if (pool.Count == 0)
                pool = _weaponPool.Count > 0 ? _weaponPool : _costumePool;
            if (pool.Count == 0)
                return null;

            var total = 0;
            for (var i = 0; i < pool.Count; i++)
                total += pool[i].Weight;
            if (total <= 0)
                return pool[Rng.Next(pool.Count)];

            var roll = Rng.Next(total);
            var acc = 0;
            for (var i = 0; i < pool.Count; i++)
            {
                acc += pool[i].Weight;
                if (roll < acc)
                    return pool[i];
            }
            return pool[pool.Count - 1];
        }

        public static void SetLastRoll(Player player, ulong itemId)
        {
            LastRoll[player] = itemId;
        }

        public static bool TryTakeLastRoll(Player player, out ulong itemId)
        {
            return LastRoll.TryRemove(player, out itemId);
        }

        public static void ClearLastRoll(Player player)
        {
            ulong itemId;
            LastRoll.TryRemove(player, out itemId);
        }

        public static void Remove(Player player)
        {
            if (player == null)
                return;
            ulong itemId;
            LastRoll.TryRemove(player, out itemId);
        }

        private static void EnsureBuilt()
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();
            if (_weaponPool != null && _builtVersion == shop.Version)
                return;

            lock (Sync)
            {
                if (_weaponPool != null && _builtVersion == shop.Version)
                    return;

                var weapons = new List<FumbiRollEntry>();
                var costumes = new List<FumbiRollEntry>();

                foreach (var item in shop.Items.Values)
                {
                    var info = item.GetItemInfo(ItemPriceType.PEN);
                    if (info == null || !info.IsEnabled)
                        continue;

                    ShopPrice best = null;
                    foreach (var price in info.PriceGroup.Prices)
                    {
                        if (!price.IsEnabled || price.Price <= 0)
                            continue;
                        if (best == null || price.Price < best.Price)
                            best = price;
                    }
                    if (best == null)
                        continue;

                    var weight = Math.Max(1, 100000 / best.Price);
                    var entry = new FumbiRollEntry
                    {
                        ItemNumber = item.ItemNumber,
                        PriceType = ItemPriceType.PEN,
                        PeriodType = best.PeriodType,
                        Period = best.Period,
                        Color = 0,
                        Weight = weight
                    };

                    uint number = item.ItemNumber;
                    if (number >= 2000000 && number < 3000000)
                        weapons.Add(entry);
                    else
                        costumes.Add(entry);
                }

                _weaponPool = weapons;
                _costumePool = costumes;
                _builtVersion = shop.Version;
            }
        }
    }
}
