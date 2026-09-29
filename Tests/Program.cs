using System;
using System.Linq;
using System.Text.Json;
using Koriko.Core;

static class Program
{
    static int passed, failed;
    static void Main()
    {
        Check("clock waits for start, then has exact 150-second phases", () => {
            var r = new Rules(); r.Advance(600, Home()); Equal(r.State.elapsed, 0d);
            r.Start(Difficulty.Cozy); r.Advance(149.99, Home()); True(!r.IsNight);
            r.Advance(.01, Home()); True(r.IsNight); Equal(r.Day, 1);
            r.Advance(150, Home()); True(!r.IsNight); Equal(r.Day, 2); Equal(r.Hour, 6d);
        });
        Check("sleep advances exactly eight game hours and restores energy", () => {
            var r = Ready(); r.State.energy = 5; var before = r.State.elapsed;
            True(r.Sleep()); Equal(r.State.elapsed - before, 100d); Equal(r.State.energy, 100d); Equal(r.Hour, 14d);
        });
        Check("sleep expires promises, buffs, hunger and passes dawn", () => {
            var r = Ready(Difficulty.Challenging); r.Advance(250, Home());
            True(r.Accept(r.State.jobs[0].id)); r.State.buffs.Add(new Buff { kind=Advantage.Awake, expires=280 });
            double fullness=r.State.fullness; int money=r.State.money;
            True(r.Sleep()); Equal(r.Day,2); True(r.Active == null); True(!r.Has(Advantage.Awake)); True(r.State.fullness < fullness); Equal(r.State.money,money-5);
        });
        Check("jobs are collected at home and paid exactly once at the correct court", () => {
            var r=Ready(); var p=r.State.jobs[0]; Land(r,"harbor"); True(!r.Accept(p.id));
            Land(r,"bakery"); True(r.Accept(p.id)); True(!r.Accept(r.State.jobs[1].id));
            int money=r.State.money; Land(r,"harbor"); True(!r.Deliver());
            Land(r,p.destination); True(r.Deliver()); True(r.State.money>money); Equal(r.State.delivered,1); True(!r.Deliver()); Equal(r.State.delivered,1);
        });
        Check("deliveries require a slow grounded approach at the correct height", () => {
            var r=Ready(); var p=r.State.jobs[0]; r.Accept(p.id); var target=Catalog.FindDestination(p.destination).Landing;
            r.Observe(new FlightFrame(new Point(target.x, target.y+10,target.z),0,false)); True(!r.Deliver());
            r.Observe(new FlightFrame(target,8,true)); True(!r.Deliver());
            r.Observe(new FlightFrame(target,1,false)); True(!r.Deliver());
            r.Observe(new FlightFrame(target,1,true)); True(r.Deliver());
        });
        Check("deadline starts at pickup and equality means too late", () => {
            var r=Ready(Difficulty.Challenging); r.Advance(10,Home()); var p=r.State.jobs[0]; r.Accept(p.id);
            Equal(p.deadline,10+p.duration); r.Advance(p.duration,Home()); True(r.Active==null); Equal(r.State.money,40);
        });
        Check("daily refresh keeps an accepted parcel and its deadline", () => {
            var r=Ready(); r.Advance(290,Home()); var p=r.State.jobs[0]; r.Accept(p.id); double deadline=p.deadline;
            r.Advance(20,Home()); Equal(r.Day,2); Equal(r.State.jobs.Count,10); Equal(r.Active.deadline,deadline); Equal(r.State.jobs.Select(j=>j.id).Distinct().Count(),10);
        });
        Check("changing difficulty never alters an accepted promise or restores completed jobs", () => {
            var r=Ready(); var p=r.State.jobs[0]; r.Accept(p.id); double deadline=p.deadline;
            r.SetDifficulty(Difficulty.Challenging); Equal(r.Active.deadline,deadline);
            Land(r,p.destination); True(r.Deliver()); int count=r.State.jobs.Count;
            r.SetDifficulty(Difficulty.Cozy); Equal(r.State.jobs.Count,count); True(!r.State.jobs.Exists(j=>j.id==p.id));
        });
        Check("shop spending cannot make money or pantry negative", () => {
            var r=Ready(); True(r.Buy(Ingredient.Milk)); Equal(r.State.money,40); Equal(r.State.pantry[1],3);
            r.State.money=0; True(!r.Buy(Ingredient.Fish)); True(!r.Cook("fish_pie")); True(!r.Buy((Ingredient)99)); Equal(r.State.money,0);
        });
        Check("upgrades enforce prerequisites, capacity, cost and maximum level", () => {
            var r=Ready(); r.State.money=3000; var p=r.State.jobs.Find(j=>j.kind==DeliveryKind.Heavy);
            True(!r.Accept(p.id)); True(!r.Purchase(Upgrade.Compass)); True(r.Purchase(Upgrade.Sling)); Equal(r.Capacity,4);
            True(r.Accept(p.id)); Land(r,p.destination); True(r.Deliver()); Land(r,"bakery");
            True(r.Purchase(Upgrade.Bristles)); True(r.CruiseSpeed>16); True(r.Purchase(Upgrade.Compass)); True(!r.Purchase(Upgrade.Compass));
            True(r.Accept(r.State.jobs.Find(j=>j.kind==DeliveryKind.Airship).id));
        });
        Check("night jobs require both a lantern and sunset", () => {
            var r=Ready(); r.State.money=500; var p=r.State.jobs.Find(j=>j.kind==DeliveryKind.Night);
            True(!r.Accept(p.id)); True(r.Purchase(Upgrade.Lantern)); True(!r.Accept(p.id)); r.Advance(150,Home()); True(r.Accept(p.id));
        });
        Check("cooking consumes ingredients once, runs time and applies an expiring buff", () => {
            var r=Ready(); r.State.energy=30; int milk=r.State.pantry[1]; True(r.Cook("coffee")); True(!r.Cook("coffee"));
            Equal(r.State.pantry[1],milk-1); True(!r.Sleep()); r.Advance(3,Home()); True(!r.Cooking); Equal(r.State.energy,65d); True(r.Has(Advantage.Awake));
            r.Advance(100,Home()); True(!r.Has(Advantage.Awake));
        });
        Check("walking conserves energy while delivery time continues", () => {
            var walk=Ready(); var fly=Ready(); var pos=new Point(30,.1,0);
            walk.Advance(10,new FlightFrame(pos,2,true)); fly.Advance(10,new FlightFrame(pos,2,false));
            Near(100-walk.State.energy,(100-fly.State.energy)*.30); Equal(walk.State.elapsed,fly.State.elapsed);
        });
        Check("coffee reduces flight fatigue by forty percent", () => {
            var a=Ready(); var b=Ready(); b.State.buffs.Add(new Buff{kind=Advantage.Awake,expires=100});
            var f=new FlightFrame(new Point(30,15,0),10,false);
            a.Advance(10,f); b.Advance(10,f); Near(100-b.State.energy,(100-a.State.energy)*.6);
        });
        Check("mint tea removes extra boost fatigue without altering normal fatigue", () => {
            var a=Ready(); var b=Ready(); b.State.buffs.Add(new Buff{kind=Advantage.FreeBoost,expires=100});
            var pos=new Point(30,15,0); a.Advance(10,new FlightFrame(pos,15,false,false)); b.Advance(10,new FlightFrame(pos,15,false,true)); Near(a.State.energy,b.State.energy);
        });
        Check("tailwind improves speed only for its duration", () => {
            var r=Ready(); double speed=r.CruiseSpeed; r.State.buffs.Add(new Buff{kind=Advantage.Tailwind,expires=1});
            Near(r.CruiseSpeed,speed*1.25); r.Advance(1,Home()); Near(r.CruiseSpeed,speed);
        });
        Check("crow cooldown, boosting and bakery safety prevent repeated damage", () => {
            var r=Ready(); r.CrowHit(false); Equal(r.State.energy,100d);
            Land(r,"clock"); r.CrowHit(true); Equal(r.State.energy,100d); r.CrowHit(false); Equal(r.State.energy,92d); r.CrowHit(false); Equal(r.State.energy,92d);
        });
        Check("fragile parcels lose integrity and stabilizers soften crow damage", () => {
            var r=Ready(); r.State.money=500; r.Purchase(Upgrade.Stabilizer); var p=r.State.jobs.Find(j=>j.kind==DeliveryKind.Fragile); r.Accept(p.id);
            Land(r,"clock"); r.CrowHit(false); Equal(p.integrity,88d);
        });
        Check("exhaustion charges a hospital fee and returns home once", () => {
            var r=Ready(); r.State.energy=.1; r.State.money=100; r.Accept(r.State.jobs[0].id);
            r.Advance(5,new FlightFrame(new Point(50,10,0),15,false)); Equal(r.State.hospitalVisits,1); Equal(r.State.money,90); Equal(r.State.energy,80d); True(r.AtHome); True(r.Active==null);
        });
        Check("safe bakery menus advance time and hunger without causing exhaustion", () => {
            var r=Ready(); r.State.energy=1; r.Advance(600,Home()); Equal(r.Day,3); Equal(r.State.energy,1d); Equal(r.State.hospitalVisits,0);
        });
        Check("large time steps produce the same fatigue as ordinary frames", () => {
            var a=Ready(); var b=Ready(); var f=new FlightFrame(new Point(40,10,0),12,false);
            a.Advance(10,f); for(int i=0;i<600;i++) b.Advance(1d/60,f); Near(a.State.energy,b.State.energy,1e-5); Near(a.State.elapsed,b.State.elapsed,1e-5);
        });
        Check("non-finite and negative inputs are ignored", () => {
            var r=Ready(); r.Advance(double.NaN,Home()); r.Advance(-1,Home()); r.Advance(1,new FlightFrame(new Point(double.PositiveInfinity,0,0))); Equal(r.State.elapsed,0d); True(r.State.position.IsFinite);
        });
        Check("save roundtrip retains inventory, completed jobs and accepted promise", () => {
            var r=Ready(); r.Buy(Ingredient.Milk); r.Accept(r.State.jobs[0].id); r.Advance(5,Home());
            var options=new JsonSerializerOptions{IncludeFields=true}; var s=JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(r.State,options),options);
            var loaded=new Rules(); True(loaded.Restore(s)); Equal(loaded.Active.id,r.Active.id); Equal(loaded.Active.deadline,r.Active.deadline); Equal(loaded.State.money,r.State.money); Equal(loaded.State.pantry[1],r.State.pantry[1]);
        });
        Check("corrupt or unsupported saves do not replace current progress", () => {
            var r=Ready(); var bad=new State{version=2}; True(!r.Restore(bad)); True(r.State.started);
            bad=new State{money=-1}; True(!Rules.Validate(bad)); bad=new State{position=new Point(double.NaN,0,0)}; True(!Rules.Validate(bad));
            bad=new State(); bad.upgrades[0]=999; True(!Rules.Validate(bad)); bad=new State{pantry=null}; True(!Rules.Validate(bad));
        });
        Check("duplicate jobs and forged multiple active parcels are rejected", () => {
            var r=Ready(); r.State.jobs.Add(r.State.jobs[0].Copy()); True(!Rules.Validate(r.State));
            r=Ready(); r.State.jobs[0].accepted=true; r.State.jobs[1].accepted=true; r.State.activeId=r.State.jobs[0].id; True(!Rules.Validate(r.State));
        });
        Check("delivery courts and recipes are complete and unique", () => {
            Equal(Catalog.Destinations.Select(d=>d.Id).Distinct().Count(),Catalog.Destinations.Length);
            True(Catalog.Destinations.All(d=>d.Landing.IsFinite&&d.Radius>=5));
            True(Catalog.Recipes.All(r=>r.Ingredients.Length==6&&r.Ingredients.Sum()>0)); Equal(Catalog.Upgrades.Length,6);
        });
        Console.WriteLine($"\n{passed} passed, {failed} failed. Gameplay rules only; this does not validate Unity rendering or input.");
        Environment.ExitCode=failed==0?0:1;
    }
    static FlightFrame Home()=>new FlightFrame(Catalog.Home.Landing);
    static Rules Ready(Difficulty difficulty=Difficulty.Cozy){var r=new Rules();r.Start(difficulty);return r;}
    static void Land(Rules r,string id)=>r.Observe(new FlightFrame(Catalog.FindDestination(id).Landing));
    static void True(bool condition){if(!condition)throw new Exception("Expected true");}
    static void Equal<T>(T actual,T expected){if(!Equals(actual,expected))throw new Exception($"Expected {expected}, got {actual}");}
    static void Near(double actual,double expected,double tolerance=1e-7){if(Math.Abs(actual-expected)>tolerance)throw new Exception($"Expected {expected}, got {actual}");}
    static void Check(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
}
