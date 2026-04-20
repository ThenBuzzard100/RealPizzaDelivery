using System;

// ─────────────────────────────────────────────
// Example items
// ─────────────────────────────────────────────
public class Sword : InventoryItem
{
    public int Damage { get; }
    public Sword(string id, string name, int damage)
        : base(id, name, ItemTag.Weapon)
    {
        Damage = damage;
    }
}

public class Pizza : InventoryItem
{
    // Tagged as Pizza — the only item type the off-hand slot accepts
    public Pizza(string id, string name)
        : base(id, name, ItemTag.Pizza) { }
}

public class Shield : InventoryItem
{
    public int Defense { get; }
    public Shield(string id, string name, int defense)
        : base(id, name, ItemTag.Shield)
    {
        Defense = defense;
    }

}

// ─────────────────────────────────────────────
// Demo
// ─────────────────────────────────────────────
public static class Program
{
    public static void Main()
    {
        var inventory = new Inventory();

        //Create items
        var longsword = new Sword ("sword_01", "Longsword", 45);
        var dagger = new Sword ("dagger_01", "Dagger", 20);
        var pizza = new Pizza("pizza_01", "Margherita Pizza");
        var kiteShield = new Shield("shield_01", "Kite Shield", 30);

        // Add everything to the bag first (like picking up from the world)
        inventory.AddToBag(longsword);
        inventory.AddToBag(dagger);
        inventory.AddToBag(pizza);
        inventory.AddToBag(kiteShield);

        Console.WriteLine("\n--- Initial State ---");
        inventory.PrintState();

         // ── Equip to main-hand slots ──────────────────────────────────

        Console.WriteLine("\n--- Equipping Longsword to Main Hand 1 ---");
        var r1 = inventory.Equip(longsword, inventory.MainHand1);
        Console.WriteLine($"Result: {r1}");
        
        Console.WriteLine("\n--- Equipping Shield to Main Hand 1 ---");
        var r2 = inventory.Equip(kiteShield, inventory.MainHand1);
        Console.WriteLine($"Result: {r2}");

        // ── Off-hand: reject a non-Pizza item ───────────────────────

        Console.WriteLine("\n--- Trying to equip Dagger to Off Hand (should FAIL) ---");
        var r3 = inventory.Equip(dagger, inventory.OffHand);
        Console.WriteLine($"Result: {r3}"); // TagMismatch

        // ── Off-hand: accept a Pizza item ───────────────────────────

        Console.WriteLine("\n--- Equipping Margherita Pizza to Off Hand (Should SUCCEED)");
        var r5 = inventory.SwapSlots(inventory.MainHand1, inventory.MainHand2);
        Console.WriteLine($"Result: {r5}");

        // Test: Try swapping a non-Pizza into the off-hand via slot swap

        Console.WriteLine("\n--- Trying to swap Longsword into Off Hand via SwapSlots (should FAIL) ---");
        var r6 = inventory.SwapSlots(inventory.MainHand1, inventory.OffHand);

        // Test: Unequip ───────────────────────────────────────────────────

        Console.WriteLine ("\n--- Auto-equip Margherita Pizza (lands in Off Hand automatically) ---");
        var r7 = inventory.AutoEquip(pizza);
        Console.WriteLine($"Result: {r7}");

        Console.WriteLine("\n--- Final State ---");
        inventory.PrintState();
    }
}