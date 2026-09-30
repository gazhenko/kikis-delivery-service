using System;
using System.Collections.Generic;

namespace Koriko.Core
{
    /// <summary>Rendering-independent rules, ported from the browser game. Time is real seconds.</summary>
    public sealed class Rules
    {
        public const double DaySeconds = 150, CycleSeconds = 300, SleepHours = 8;
        public const double SleepSeconds = CycleSeconds * SleepHours / 24;
        public State State { get; private set; } = new State();
        public string Notice { get; private set; } = "Choose a delivery at Osono’s bakery.";
        public int NoticeRevision { get; private set; }
        public int HomeReturns { get; private set; }
        public int LastPay { get; private set; }
        public int LastTip { get; private set; }
        public bool Grounded { get; private set; } = true;
        public double CurrentSpeed { get; private set; }
        public int Day => 1 + (int)Math.Floor(State.elapsed / CycleSeconds);
        public bool IsNight => State.elapsed % CycleSeconds >= DaySeconds;
        public double Hour => (6 + State.elapsed / CycleSeconds * 24) % 24;
        public int Capacity => 2 + Level(Upgrade.Sling) * 2;
        public double CruiseSpeed => 16 * (1 + Level(Upgrade.Bristles) * .18) * (Has(Advantage.Tailwind) ? 1.25 : 1);
        public bool AtHome => Near(Catalog.Home) && Grounded;
        public bool Cooking => !string.IsNullOrEmpty(State.recipeId);
        public Parcel Active => State.jobs.Find(p => p.id == State.activeId && p.accepted);
        public Rules() { RefreshJobs(); }
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));
        public int Level(Upgrade id) => State.upgrades[(int)id];
        public bool Has(Advantage kind) => State.buffs.Exists(b => b.kind == kind && b.expires > State.elapsed);
        public bool Near(Destination d) => d != null && State.position.HorizontalDistance(d.Landing) <= d.Radius && Math.Abs(State.position.y - d.Landing.y) <= 2.2;
        public int Cost(Upgrade id) => (int)Math.Round(Catalog.Upgrades[(int)id].Cost * (1 + Level(id) * .7), MidpointRounding.AwayFromZero);
        public void Say(string text) { Notice = text; NoticeRevision++; }

        public void Start(Difficulty difficulty)
        {
            SetDifficulty(difficulty);
            State.started = true;
            Say("Osono has parcels ready. Choose a delivery.");
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            if (!Enum.IsDefined(typeof(Difficulty), difficulty)) return;
            double old = State.difficulty == Difficulty.Cozy ? 1.5 : 1;
            double next = difficulty == Difficulty.Cozy ? 1.5 : 1;
            foreach (var p in State.jobs) if (!p.accepted) p.duration = Math.Round(p.duration / old * next);
            State.difficulty = difficulty;
        }

        public void Observe(FlightFrame frame)
        {
            if (!frame.Position.IsFinite || !Finite(frame.Speed) || frame.Speed < 0) return;
            State.position = frame.Position;
            CurrentSpeed = frame.Speed;
            Grounded = frame.Grounded;
        }

        private string HomeReason()
        {
            if (!State.started) return "Choose cozy or challenging to begin.";
            if (!AtHome) return "Land in Osono’s bakery courtyard.";
            return null;
        }

        public string JobReason(Parcel p)
        {
            string home = HomeReason();
            if (home != null) return home;
            if (p == null || !State.jobs.Contains(p)) return "This delivery is no longer available.";
            if (Active != null) return "Finish your current delivery first.";
            if (p.accepted) return "This parcel has already been collected.";
            if (Cooking) return "Your recipe is still cooking.";
            if (p.kind == DeliveryKind.Rush && Level(Upgrade.Bristles) == 0) return "Requires silver birch bristles.";
            if (p.kind == DeliveryKind.Heavy && Level(Upgrade.Sling) == 0) return "Requires a cargo sling.";
            if (p.kind == DeliveryKind.Fragile && Level(Upgrade.Stabilizer) == 0) return "Requires a feather stabilizer.";
            if (p.kind == DeliveryKind.Night && Level(Upgrade.Lantern) == 0) return "Requires a moonlight lantern.";
            if (p.kind == DeliveryKind.Night && !IsNight) return "Available after sunset.";
            if (p.kind == DeliveryKind.Airship && Level(Upgrade.Compass) == 0) return "Requires a skyfarer’s compass.";
            if (p.weight > Capacity) return $"Needs {p.weight:0.#} kg capacity; your broom carries {Capacity} kg.";
            return null;
        }

        public bool Accept(string id)
        {
            var p = State.jobs.Find(j => j.id == id);
            string reason = JobReason(p);
            if (reason != null) { Say(reason); return false; }
            p.accepted = true;
            p.deadline = State.elapsed + p.duration;
            State.activeId = p.id;
            Say($"{p.title} — {Catalog.FindDestination(p.destination).Name}.");
            return true;
        }

        public bool CanDeliver => Active != null && State.elapsed < Active.deadline && Near(Catalog.FindDestination(Active.destination)) && Grounded && CurrentSpeed < 2.5;
        public bool Deliver()
        {
            var p = Active;
            if (p == null) return false;
            if (State.elapsed >= p.deadline) { FailDelivery(); return false; }
            if (!CanDeliver) { Say("Slow down and land in the delivery court."); return false; }
            double integrity = p.kind == DeliveryKind.Fragile ? .6 + p.integrity * .004 : 1;
            int pay = (int)Math.Round(p.reward * integrity * (Has(Advantage.Lucky) ? 1.2 : 1), MidpointRounding.AwayFromZero);
            int tip = (int)Math.Round(pay * .2 * Clamp((p.deadline - State.elapsed) / p.duration, 0, 1), MidpointRounding.AwayFromZero);
            LastPay = pay; LastTip = tip;
            State.money += pay + tip;
            State.earned += pay + tip;
            State.delivered++;
            State.jobs.Remove(p);
            State.activeId = "";
            Say($"Delivered! +{pay} coins" + (tip > 0 ? $" · +{tip} timely tip. Jiji approves." : ". Jiji approves."));
            return true;
        }

        public string IngredientReason(Ingredient ingredient)
        {
            if (!Enum.IsDefined(typeof(Ingredient), ingredient)) return "Unknown ingredient.";
            string reason = HomeReason();
            if (reason != null) return reason;
            int price = Catalog.IngredientCosts[(int)ingredient];
            return State.money < price ? $"Need {price} coins." : null;
        }

        public bool Buy(Ingredient ingredient)
        {
            string reason = IngredientReason(ingredient);
            if (reason != null) { Say(reason); return false; }
            State.money -= Catalog.IngredientCosts[(int)ingredient];
            State.pantry[(int)ingredient]++;
            Say($"{Catalog.IngredientNames[(int)ingredient]} added to the pantry.");
            return true;
        }

        public string RecipeReason(Recipe recipe)
        {
            string reason = HomeReason();
            if (reason != null) return reason;
            if (recipe == null) return "Unknown recipe.";
            if (Cooking) return "The oven is busy.";
            for (int i = 0; i < 6; i++) if (State.pantry[i] < recipe.Ingredients[i]) return $"Need {recipe.Ingredients[i]} {Catalog.IngredientNames[i].ToLowerInvariant()}.";
            return null;
        }

        public bool Cook(string id)
        {
            var recipe = Array.Find(Catalog.Recipes, r => r.Id == id);
            string reason = RecipeReason(recipe);
            if (reason != null) { Say(reason); return false; }
            for (int i = 0; i < 6; i++) State.pantry[i] -= recipe.Ingredients[i];
            State.recipeId = id;
            State.cookingRemaining = recipe.Seconds;
            Say($"Making {recipe.Name.ToLowerInvariant()}…");
            return true;
        }

        public string UpgradeReason(Upgrade id)
        {
            if (!Enum.IsDefined(typeof(Upgrade), id)) return "Unknown upgrade.";
            string reason = HomeReason();
            if (reason != null) return reason;
            if (Level(id) >= Catalog.Upgrades[(int)id].Max) return "Fully upgraded.";
            if (id == Upgrade.Compass && (Level(Upgrade.Bristles) == 0 || Level(Upgrade.Sling) == 0)) return "Fit birch bristles and a cargo sling first.";
            return State.money < Cost(id) ? $"Need {Cost(id)} coins." : null;
        }

        public bool Purchase(Upgrade id)
        {
            string reason = UpgradeReason(id);
            if (reason != null) { Say(reason); return false; }
            State.money -= Cost(id);
            State.upgrades[(int)id]++;
            Say($"{Catalog.Upgrades[(int)id].Name} fitted.");
            return true;
        }

        public bool Sleep()
        {
            string reason = HomeReason();
            if (reason != null) { Say(reason); return false; }
            if (Cooking) { Say("Finish cooking before going upstairs."); return false; }
            bool hadParcel = Active != null;
            Advance(SleepSeconds, new FlightFrame(Catalog.Home.Landing));
            State.energy = 100;
            Say("Eight hours later. Rested, with Jiji beside you." + (hadParcel && Active == null ? " Your delivery window closed." : ""));
            return true;
        }

        public void Advance(double seconds, FlightFrame frame)
        {
            if (!State.started || !Finite(seconds) || seconds <= 0 || seconds > 86400 || !frame.Position.IsFinite || !Finite(frame.Speed) || frame.Speed < 0) return;
            Observe(frame);
            int returns = HomeReturns;
            double end = Math.Round(State.elapsed + seconds, 9);
            while (State.elapsed < end - 1e-9)
            {
                double dt = Math.Min(.1, end - State.elapsed);
                Tick(dt, HomeReturns == returns && frame.Boosting);
            }
            State.elapsed = end;
        }

        private void Tick(double dt, bool boosting)
        {
            int previousDay = Day;
            State.elapsed = Math.Round(State.elapsed + dt, 9);
            State.buffs.RemoveAll(b => b.expires <= State.elapsed);
            if (Active != null && State.elapsed >= Active.deadline) FailDelivery();
            if (Day != previousDay) RefreshJobs();
            if (Cooking)
            {
                State.cookingRemaining = Math.Max(0, State.cookingRemaining - dt);
                if (State.cookingRemaining < 1e-8) FinishCooking();
            }
            bool moving = CurrentSpeed > .7;
            bool cozy = State.difficulty == Difficulty.Cozy;
            State.fullness = Math.Max(0, State.fullness - dt * (cozy ? .075 : .105) * (moving ? 1.25 : 1));
            if (!AtHome || moving)
            {
                double fatigue = moving ? (cozy ? .52 : .8) : (cozy ? .065 : .095);
                if (moving && Grounded) fatigue *= .30;
                if (boosting && !Has(Advantage.FreeBoost)) fatigue *= 2;
                if (Has(Advantage.Awake)) fatigue *= .6;
                if (IsNight) fatigue *= 1.18;
                if (State.fullness <= 0) fatigue += cozy ? .35 : .6;
                State.energy = Math.Max(0, State.energy - fatigue * dt);
            }
            if (State.energy <= 0) Hospital();
        }

        private void FinishCooking()
        {
            var recipe = Array.Find(Catalog.Recipes, r => r.Id == State.recipeId);
            State.recipeId = "";
            State.cookingRemaining = 0;
            if (recipe == null) return;
            State.energy = Clamp(State.energy + recipe.Energy, 0, 100);
            State.fullness = Clamp(State.fullness + recipe.Fullness, 0, 100);
            if (recipe.Buff.HasValue)
            {
                State.buffs.RemoveAll(b => b.kind == recipe.Buff.Value);
                State.buffs.Add(new Buff { kind = recipe.Buff.Value, expires = State.elapsed + recipe.BuffSeconds });
            }
            Say($"{recipe.Name}, enjoyed warm. {recipe.Description}.");
        }

        public void CrowHit(bool boosting)
        {
            if (!State.started || State.position.HorizontalDistance(Catalog.Home.Landing) < 22 || boosting || State.elapsed < State.invulnerableUntil) return;
            bool cozy = State.difficulty == Difficulty.Cozy;
            State.energy = Math.Max(0, State.energy - (cozy ? 8 : 15) * (1 - Level(Upgrade.Bell) * .2));
            State.invulnerableUntil = State.elapsed + (cozy ? 4 : 2.8);
            if (Active?.kind == DeliveryKind.Fragile) Active.integrity = Math.Max(0, Active.integrity - 24.0 / (1 + Level(Upgrade.Stabilizer)));
            Say("A cheeky crow! Boost to slip past it.");
            if (State.energy <= 0) Hospital();
        }

        private void FailDelivery()
        {
            State.jobs.RemoveAll(p => p.id == State.activeId);
            State.activeId = "";
            int fee = State.difficulty == Difficulty.Challenging ? Math.Min(5, State.money) : 0;
            State.money -= fee;
            Say("The delivery window closed. Osono has another parcel." + (fee > 0 ? $" −{fee} coins." : ""));
        }

        private void Hospital()
        {
            int fee = Math.Min(State.money, Math.Max(5, (int)Math.Round(State.money * (State.difficulty == Difficulty.Cozy ? .1 : .2), MidpointRounding.AwayFromZero)));
            State.money -= fee;
            State.hospitalVisits++;
            State.jobs.RemoveAll(p => p.id == State.activeId);
            State.activeId = "";
            State.recipeId = "";
            State.cookingRemaining = 0;
            State.position = Catalog.Home.Landing;
            State.energy = 80;
            State.fullness = Math.Max(35, State.fullness);
            State.invulnerableUntil = State.elapsed + 6;
            CurrentSpeed = 0;
            Grounded = true;
            HomeReturns++;
            Say($"Osono brought you home from the hospital. −{fee} coins. Rest before your next flight.");
        }

        private void RefreshJobs()
        {
            Parcel active = Active;
            State.jobs.Clear();
            if (active != null) State.jobs.Add(active);
            AddJob(0, "A warm loaf for the market", "clock", DeliveryKind.Parcel, 1, 28, 65);
            AddJob(1, "Letters from far away", "harbor", DeliveryKind.Parcel, .5, 34, 70);
            AddJob(2, "Tea for Madame", "madame", DeliveryKind.Parcel, 1, 38, 75);
            AddJob(3, "A parcel for Tombo", "tombo", DeliveryKind.Parcel, 1.5, 32, 65);
            AddJob(4, "Madame’s porcelain teapot", "madame", DeliveryKind.Fragile, 1.5, 64, 70);
            AddJob(5, "Before the bell rings", "clock", DeliveryKind.Rush, 1, 58, 25);
            AddJob(6, "Tombo’s new invention", "tombo", DeliveryKind.Heavy, 4, 72, 65);
            AddJob(7, "A lantern across the water", "harbor", DeliveryKind.Night, 2, 76, 60);
            AddJob(8, "Special airship freight", "airship", DeliveryKind.Airship, 4, 125, 70);
        }

        private void AddJob(int index, string title, string destination, DeliveryKind kind, double weight, int reward, double duration)
        {
            string id = $"day-{Day}-{index}";
            if (State.jobs.Exists(p => p.id == id)) return;
            State.jobs.Add(new Parcel { id = id, title = title, destination = destination, kind = kind, weight = weight, reward = reward, duration = Math.Round(duration * (State.difficulty == Difficulty.Cozy ? 1.5 : 1)) });
        }

        public bool Restore(State candidate)
        {
            if (!Validate(candidate)) return false;
            State = candidate;
            CurrentSpeed = 0;
            Grounded = Near(Catalog.Home) || Array.Exists(Catalog.Destinations, d => Near(d));
            Say("Welcome home, Kiki. Your deliveries are right where you left them.");
            return true;
        }

        public static bool Validate(State s)
        {
            if (s == null || s.version != 1 || !Enum.IsDefined(typeof(Difficulty), s.difficulty) || !Finite(s.elapsed) || s.elapsed < 0 || s.elapsed > 1e9) return false;
            if (!s.position.IsFinite || Math.Abs(s.position.x) > 240 || s.position.z < -165 || s.position.z > 225 || s.position.y < -5 || s.position.y > 100) return false;
            if (!Finite(s.energy) || !Finite(s.fullness) || s.energy <= 0 || s.energy > 100 || s.fullness < 0 || s.fullness > 100) return false;
            if (!Finite(s.invulnerableUntil) || s.invulnerableUntil < 0 || s.money < 0 || s.money > 10000000 || s.delivered < 0 || s.earned < 0 || s.hospitalVisits < 0) return false;
            if (s.pantry == null || s.pantry.Length != 6 || s.upgrades == null || s.upgrades.Length != 6) return false;
            for (int i = 0; i < 6; i++) if (s.pantry[i] < 0 || s.pantry[i] > 10000 || s.upgrades[i] < 0 || s.upgrades[i] > Catalog.Upgrades[i].Max) return false;
            if (s.jobs == null || s.jobs.Count > 10 || s.buffs == null || s.buffs.Count > 4 || s.activeId == null || s.recipeId == null) return false;
            var ids = new HashSet<string>();
            int accepted = 0;
            foreach (var p in s.jobs)
            {
                if (p == null || string.IsNullOrEmpty(p.id) || p.id.Length > 60 || !ids.Add(p.id) || string.IsNullOrEmpty(p.title) || p.title.Length > 150 || Catalog.FindDestination(p.destination) == null || p.destination == "bakery") return false;
                if (!Enum.IsDefined(typeof(DeliveryKind), p.kind) || !Finite(p.weight) || p.weight <= 0 || p.weight > 8 || !Finite(p.duration) || p.duration <= 0 || p.duration > 600 || p.reward < 0 || p.reward > 10000) return false;
                if (!Finite(p.deadline) || p.deadline < 0 || !Finite(p.integrity) || p.integrity < 0 || p.integrity > 100) return false;
                if (p.accepted) { accepted++; if (p.id != s.activeId || p.deadline > s.elapsed + p.duration + .001) return false; }
            }
            if (accepted > 1 || (accepted == 0) != (s.activeId == "")) return false;
            var buffs = new HashSet<Advantage>();
            foreach (var b in s.buffs) if (b == null || !Enum.IsDefined(typeof(Advantage), b.kind) || !buffs.Add(b.kind) || !Finite(b.expires) || b.expires < 0 || b.expires > s.elapsed + 120.001) return false;
            if (!Finite(s.cookingRemaining) || s.cookingRemaining < 0) return false;
            if (s.recipeId == "") return s.cookingRemaining == 0;
            var recipe = Array.Find(Catalog.Recipes, r => r.Id == s.recipeId);
            return recipe != null && s.cookingRemaining > 0 && s.cookingRemaining <= recipe.Seconds && s.position.HorizontalDistance(Catalog.Home.Landing) <= Catalog.Home.Radius && Math.Abs(s.position.y - Catalog.Home.Landing.y) <= 2.2;
        }
    }
}
