using System;

namespace Koriko.Core
{
    [Serializable]
    public struct Point
    {
        public double x, y, z;
        public Point(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public double HorizontalDistance(Point other) => Math.Sqrt((x - other.x) * (x - other.x) + (z - other.z) * (z - other.z));
        public bool IsFinite => Rules.Finite(x) && Rules.Finite(y) && Rules.Finite(z);
    }

    public enum Difficulty { Cozy, Challenging }
    public enum DeliveryKind { Parcel, Fragile, Rush, Heavy, Night, Airship }
    public enum Ingredient { Flour, Milk, Egg, Fish, Mint, Coffee }
    public enum Upgrade { Bristles, Sling, Stabilizer, Lantern, Bell, Compass }
    public enum Advantage { Awake, Tailwind, FreeBoost, Lucky }

    public sealed class Destination
    {
        public readonly string Id, Name, Description;
        public readonly Point Landing;
        public readonly double Radius;
        public Destination(string id, string name, string description, Point landing, double radius = 6)
        { Id = id; Name = name; Description = description; Landing = landing; Radius = radius; }
    }

    public sealed class Recipe
    {
        public readonly string Id, Name, Description;
        public readonly int[] Ingredients;
        public readonly double Seconds, Energy, Fullness, BuffSeconds;
        public readonly Advantage? Buff;
        public readonly int Icon;
        public Recipe(string id, string name, string description, int[] ingredients, double seconds, double energy, double fullness, int icon, Advantage? buff = null, double buffSeconds = 0)
        { Id = id; Name = name; Description = description; Ingredients = ingredients; Seconds = seconds; Energy = energy; Fullness = fullness; Icon = icon; Buff = buff; BuffSeconds = buffSeconds; }
    }

    public sealed class BroomUpgrade
    {
        public readonly Upgrade Id;
        public readonly string Name, Description;
        public readonly int Cost, Max;
        public BroomUpgrade(Upgrade id, string name, string description, int cost, int max)
        { Id = id; Name = name; Description = description; Cost = cost; Max = max; }
    }

    public static class Catalog
    {
        public static readonly Destination[] Destinations = {
            new Destination("bakery", "Osono’s bakery", "Your room above the warm ovens", new Point(-113, 0.16, 9), 6),
            new Destination("clock", "Clock-tower square", "The little court beside the market", new Point(10, 0.16, 18)),
            new Destination("harbor", "Harbor post house", "Blue doors facing the sea", new Point(121, 0.16, -55)),
            new Destination("madame", "Madame’s garden", "A rose garden above the harbor", new Point(90, 7.16, 91)),
            new Destination("tombo", "Tombo’s workshop", "Bicycles, propellers and a hopeful inventor", new Point(-31, 0.16, 49)),
            new Destination("airship", "Spirit of Freedom", "The open cargo platform beneath the airship", new Point(126, 40.16, -106), 5)
        };
        public static Destination Home => Destinations[0];
        public static Destination FindDestination(string id) => Array.Find(Destinations, d => d.Id == id);
        public static readonly string[] IngredientNames = { "Flour", "Milk", "Egg", "Herring", "Mint", "Coffee beans" };
        public static readonly int[] IngredientCosts = { 4, 5, 5, 9, 4, 7 };
        public static readonly int[] IngredientIcons = { 4, 5, 6, 7, 8, 9 };
        public static readonly Recipe[] Recipes = {
            new Recipe("bread", "Warm bread", "+18 energy · +35 fullness", new[]{1,1,0,0,0,0}, 4,18,35,10),
            new Recipe("pancakes", "Sunday pancakes", "+30 energy · +60 fullness", new[]{1,1,1,0,0,0}, 6,30,60,11),
            new Recipe("coffee", "Café au lait", "40% less fatigue for 100 seconds", new[]{0,1,0,0,0,1}, 3,35,5,12,Advantage.Awake,100),
            new Recipe("tailwind", "Tailwind mint bun", "25% faster flight for 90 seconds", new[]{1,0,1,0,1,0}, 5,15,35,13,Advantage.Tailwind,90),
            new Recipe("mint_tea", "Ursula’s mint tea", "Boost without extra fatigue for 90 seconds", new[]{0,1,0,0,1,0}, 3,20,10,14,Advantage.FreeBoost,90),
            new Recipe("fish_pie", "Madame’s herring pie", "20% more delivery earnings for 120 seconds", new[]{1,0,1,1,0,0}, 7,25,70,15,Advantage.Lucky,120)
        };
        public static readonly BroomUpgrade[] Upgrades = {
            new BroomUpgrade(Upgrade.Bristles,"Silver birch bristles","18% more speed per level; express deliveries",65,3),
            new BroomUpgrade(Upgrade.Sling,"Woven cargo sling","2 kg more capacity per level; heavy deliveries",80,3),
            new BroomUpgrade(Upgrade.Stabilizer,"Feather stabilizer","Protect fragile parcels from crow bumps",75,2),
            new BroomUpgrade(Upgrade.Lantern,"Moonlight lantern","Open night deliveries; light the way home",70,1),
            new BroomUpgrade(Upgrade.Bell,"Little wind bell","Crows approach less closely and do less damage",85,2),
            new BroomUpgrade(Upgrade.Compass,"Skyfarer’s compass","Airship freight; requires bristles and cargo sling",180,1)
        };

        public static double TerrainHeight(double x, double z)
        {
            double t = Rules.Clamp((z - 56) / 47, 0, 1);
            return 7 * t * t * (3 - 2 * t);
        }
    }
}
