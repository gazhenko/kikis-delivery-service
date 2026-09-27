using System;
using System.Collections.Generic;

namespace Koriko.Core
{
    [Serializable]
    public sealed class Parcel
    {
        public string id, title, destination;
        public DeliveryKind kind;
        public double weight, duration, deadline, integrity = 100;
        public int reward;
        public bool accepted;
        public Parcel Copy() => (Parcel)MemberwiseClone();
    }

    [Serializable]
    public sealed class Buff
    {
        public Advantage kind;
        public double expires;
    }

    [Serializable]
    public sealed class State
    {
        public int version = 1;
        public bool started;
        public Difficulty difficulty;
        public double elapsed, energy = 100, fullness = 85, invulnerableUntil;
        public int money = 45, delivered, earned, hospitalVisits;
        public Point position = Catalog.Home.Landing;
        public int[] pantry = { 2, 2, 1, 0, 1, 1 };
        public int[] upgrades = new int[6];
        public List<Parcel> jobs = new List<Parcel>();
        public List<Buff> buffs = new List<Buff>();
        public string activeId = "", recipeId = "";
        public double cookingRemaining;
    }

    public readonly struct FlightFrame
    {
        public readonly Point Position;
        public readonly double Speed;
        public readonly bool Grounded, Boosting;
        public FlightFrame(Point position, double speed = 0, bool grounded = true, bool boosting = false)
        { Position = position; Speed = speed; Grounded = grounded; Boosting = boosting; }
    }
}
