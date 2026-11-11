using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tomorrow
{
    public enum ItemId
    {
        None,
        Crowbar, IronPipe, RedTape,
        BridgeKit, Plank, Rope,
        Heater, GlowFlower, TinCan,
        Battery, CopperWire, JamJar, Bread,
        GrandmasWrench, YellowGlove, Letter,
        // Chapter 2 — the harbor
        Magnet, IronBar, LongRope, ShortRope, Spanner, Bolt, Lantern, FishingNet, String, Stick, Button,
        EnginePart, Gear, Spring,
        // Chapter 3 — market and greenhouse
        Scale, BrassPlate, Apple, SeedPacket, GlowSeed, WateringCan, Handle, CartWheel, Spade,
        GlassPanes, GlassShard, Frame, Mirror, Polish, DiaryPage, BirthdayCard
    }

    public static class Items
    {
        static readonly Dictionary<ItemId, string> names = new Dictionary<ItemId, string>
        {
            { ItemId.Crowbar, "Crowbar" }, { ItemId.IronPipe, "Iron pipe" }, { ItemId.RedTape, "Red tape" },
            { ItemId.BridgeKit, "Plank Bridge Kit" }, { ItemId.Plank, "Plank" }, { ItemId.Rope, "Rope" },
            { ItemId.Heater, "Heater" }, { ItemId.GlowFlower, "Glow-flower" }, { ItemId.TinCan, "Tin can" },
            { ItemId.Battery, "Battery" }, { ItemId.CopperWire, "Copper wire" }, { ItemId.JamJar, "Jam jar" },
            { ItemId.Bread, "Warm bread" }, { ItemId.GrandmasWrench, "Grandma's wrench" },
            { ItemId.YellowGlove, "Yellow glove" }, { ItemId.Letter, "Letter" },
            { ItemId.Magnet, "Big red magnet" }, { ItemId.IronBar, "Iron bar" }, { ItemId.LongRope, "Long rope" },
            { ItemId.ShortRope, "Short rope" }, { ItemId.Spanner, "Spanner" }, { ItemId.Bolt, "Bolt" },
            { ItemId.Lantern, "Lantern" }, { ItemId.FishingNet, "Fishing net" }, { ItemId.String, "String" },
            { ItemId.Stick, "Stick" }, { ItemId.Button, "Shiny button" }, { ItemId.EnginePart, "Engine part" },
            { ItemId.Gear, "Gear" }, { ItemId.Spring, "Spring" },
            { ItemId.Scale, "Mira's scale" }, { ItemId.BrassPlate, "Brass plate" }, { ItemId.Apple, "Free apple" },
            { ItemId.SeedPacket, "Glow-flower seed packet" }, { ItemId.GlowSeed, "Glow seed" },
            { ItemId.WateringCan, "Nell's watering can" }, { ItemId.Handle, "Handle" }, { ItemId.CartWheel, "Cart wheel" },
            { ItemId.Spade, "Nell's spade" }, { ItemId.GlassPanes, "Glass panes" }, { ItemId.GlassShard, "Glass shard" },
            { ItemId.Frame, "Window frame" }, { ItemId.Mirror, "Mirror" }, { ItemId.Polish, "Polish" },
            { ItemId.DiaryPage, "Grandma's diary page" }, { ItemId.BirthdayCard, "Birthday card" },
        };

        public static string Name(ItemId id) => names.TryGetValue(id, out var n) ? n : id.ToString();
    }

    /// <summary>What it costs to build each Tomorrow item at the workbench.</summary>
    public static class Recipes
    {
        public static readonly Dictionary<ItemId, Dictionary<ItemId, int>> All = new Dictionary<ItemId, Dictionary<ItemId, int>>
        {
            { ItemId.Crowbar,   new Dictionary<ItemId, int> { { ItemId.IronPipe, 1 }, { ItemId.RedTape, 1 } } },
            { ItemId.BridgeKit, new Dictionary<ItemId, int> { { ItemId.Plank, 3 }, { ItemId.Rope, 1 } } },
            { ItemId.Heater,    new Dictionary<ItemId, int> { { ItemId.GlowFlower, 1 }, { ItemId.TinCan, 1 } } },
            { ItemId.Battery,   new Dictionary<ItemId, int> { { ItemId.CopperWire, 1 }, { ItemId.JamJar, 1 } } },
            { ItemId.Magnet,    new Dictionary<ItemId, int> { { ItemId.IronBar, 1 }, { ItemId.CopperWire, 1 } } },
            { ItemId.LongRope,  new Dictionary<ItemId, int> { { ItemId.ShortRope, 3 } } },
            { ItemId.Lantern,   new Dictionary<ItemId, int> { { ItemId.JamJar, 1 }, { ItemId.GlowFlower, 1 }, { ItemId.CopperWire, 1 } } },
            { ItemId.FishingNet, new Dictionary<ItemId, int> { { ItemId.String, 1 }, { ItemId.Stick, 2 } } },
            { ItemId.EnginePart, new Dictionary<ItemId, int> { { ItemId.Gear, 1 }, { ItemId.Spring, 1 } } },
            { ItemId.Spanner,   new Dictionary<ItemId, int> { { ItemId.IronBar, 1 }, { ItemId.Bolt, 1 } } },
            { ItemId.Scale,     new Dictionary<ItemId, int> { { ItemId.Spring, 1 }, { ItemId.BrassPlate, 1 } } },
            { ItemId.SeedPacket, new Dictionary<ItemId, int> { { ItemId.GlowSeed, 3 } } },
            { ItemId.WateringCan, new Dictionary<ItemId, int> { { ItemId.TinCan, 1 }, { ItemId.IronPipe, 1 }, { ItemId.Handle, 1 } } },
            { ItemId.CartWheel, new Dictionary<ItemId, int> { { ItemId.Plank, 2 }, { ItemId.IronBar, 1 } } },
            { ItemId.Spade,     new Dictionary<ItemId, int> { { ItemId.Stick, 1 }, { ItemId.TinCan, 1 } } },
            { ItemId.GlassPanes, new Dictionary<ItemId, int> { { ItemId.GlassShard, 2 }, { ItemId.Frame, 1 } } },
            { ItemId.Mirror,    new Dictionary<ItemId, int> { { ItemId.TinCan, 1 }, { ItemId.Polish, 1 } } },
        };

        public static string Describe(ItemId result)
        {
            if (!All.TryGetValue(result, out var parts)) return "";
            var list = new List<string>();
            foreach (var p in parts) list.Add(p.Value > 1 ? $"{p.Value} × {Items.Name(p.Key)}" : Items.Name(p.Key));
            return string.Join(" + ", list);
        }
    }

    /// <summary>Pip's pockets.</summary>
    public class Inventory
    {
        readonly Dictionary<ItemId, int> counts = new Dictionary<ItemId, int>();
        public event Action Changed;

        public int Count(ItemId id) => counts.TryGetValue(id, out var c) ? c : 0;
        public bool Has(ItemId id, int n = 1) => Count(id) >= n;

        public void Add(ItemId id, int n = 1)
        {
            counts[id] = Count(id) + n;
            Changed?.Invoke();
        }

        public bool Remove(ItemId id, int n = 1)
        {
            if (!Has(id, n)) return false;
            counts[id] = Count(id) - n;
            if (counts[id] <= 0) counts.Remove(id);
            Changed?.Invoke();
            return true;
        }

        public bool CanBuild(ItemId result)
        {
            if (!Recipes.All.TryGetValue(result, out var parts)) return false;
            foreach (var p in parts) if (!Has(p.Key, p.Value)) return false;
            return true;
        }

        public bool Build(ItemId result)
        {
            if (!CanBuild(result)) return false;
            foreach (var p in Recipes.All[result]) counts[p.Key] = Count(p.Key) - p.Value;
            var empty = new List<ItemId>();
            foreach (var kv in counts) if (kv.Value <= 0) empty.Add(kv.Key);
            foreach (var e in empty) counts.Remove(e);
            Add(result);
            return true;
        }

        public IEnumerable<KeyValuePair<ItemId, int>> All() => counts;
    }

    /// <summary>A loan from tomorrow: what must be built and dropped in the Return Box before sunset.</summary>
    [Serializable]
    public class Debt
    {
        public ItemId item;
        public int amount = 1;
        public bool returned;
        public string fromWhom = "Tomorrow";
        [Tooltip("In-game hour it must be back by; 99 = no clock deadline.")]
        public float dueHour = 99f;
        [Tooltip("Set for a friend's tomorrow (pink box): lateness costs a heart instead of interest.")]
        public string friend;
        [NonSerialized] public bool lateHandled;
    }
}
