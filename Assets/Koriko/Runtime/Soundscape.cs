using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    public enum Cue { Tick, Confirm, Back, Page, Coin, Deliver, Ding, Sizzle, Lullaby, Hospital, Low, Accept }

    /// <summary>Procedural sound for the town: wind, sea, crickets, birds, gulls, footsteps,
    /// broom, interface, a clock bell and a short original waltz. Every clip is synthesized
    /// from this source at startup on a worker thread; no recordings or film music ship.</summary>
    public sealed class Soundscape : MonoBehaviour
    {
        public GameApp App;
        public TownLife Life;
        public Vector3 BellPosition=new Vector3(16,33,40);
        public float Master=.8f,Music=.65f,Effects=.9f;
        public bool Ready=>clips!=null;
        public string Summary {get;private set;}="";
        public float WindLevel=>wind?wind.volume:0;
        public float SeaLevel=>sea?sea.volume:0;
        public float MusicLevel=>music?music.volume:0;
        public float CricketLevel=>crickets?crickets.volume:0;
        public int Footsteps {get;private set;}
        public System.Collections.Generic.IEnumerable<string> ClipNames=>clips!=null?clips.Keys:new string[0];
        const int Rate=44100,BedRate=22050;
        const string Preferences="koriko.audio.";
        Task<Bank> building;
        Dictionary<string,AudioClip> clips;
        AudioSource wind,sea,crickets,music,ui,self;
        readonly List<AudioSource> world=new List<AudioSource>();
        RiderPerformance rider;
        Rules tracked;
        System.Random dice=new System.Random(1989);
        int footfalls,takeoffs,landings,delivered,homeReturns,nextWorld;
        int bellHour=-1,bellStrikes;float bellClock;
        float birdTimer=3,gullTimer=4,musicQuiet;
        bool boosting,lowWarned;
        string recipe="";
        double lastElapsed;

        void Awake()
        {
            if(DevelopmentFlightCheck.Requested)Master=DevelopmentTourCheck.Requested?.35f:0;
            else
            {
                Master=Mathf.Clamp01(PlayerPrefs.GetFloat(Preferences+"master",.8f));
                Music=Mathf.Clamp01(PlayerPrefs.GetFloat(Preferences+"music",.65f));
                Effects=Mathf.Clamp01(PlayerPrefs.GetFloat(Preferences+"effects",.9f));
            }
            AudioListener.volume=Master;
            building=Task.Run(()=>Bank.Build());
        }
        public void SavePreferences()
        {
            AudioListener.volume=Master;
            if(DevelopmentFlightCheck.Requested)return;
            PlayerPrefs.SetFloat(Preferences+"master",Master);PlayerPrefs.SetFloat(Preferences+"music",Music);PlayerPrefs.SetFloat(Preferences+"effects",Effects);PlayerPrefs.Save();
        }
        AudioSource Source(string name,bool loop,float spatial)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var s=go.AddComponent<AudioSource>();s.loop=loop;s.playOnAwake=false;s.spatialBlend=spatial;s.dopplerLevel=0;
            s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=6;s.maxDistance=160;return s;
        }
        void Create(Bank bank)
        {
            clips=new Dictionary<string,AudioClip>();
            foreach(var entry in bank.Clips)
            {
                var clip=AudioClip.Create(entry.Key,entry.Value.Data.Length,1,entry.Value.Rate,false);
                clip.SetData(entry.Value.Data,0);clips.Add(entry.Key,clip);
            }
            Summary=bank.Summary;
            wind=Source("Wind",true,0);sea=Source("Sea",true,0);crickets=Source("Crickets",true,0);music=Source("Waltz",true,0);
            ui=Source("Interface",false,0);self=Source("Kiki",false,0);ui.ignoreListenerPause=true;
            for(int i=0;i<6;i++)world.Add(Source("Town "+i,false,1));
            wind.clip=clips["wind"];sea.clip=clips["sea"];crickets.clip=clips["crickets"];music.clip=clips["waltz"];
            foreach(var s in new[]{wind,sea,crickets,music}){s.volume=0;s.Play();}
            rider=App?App.Motor.Visual.GetComponentInChildren<RiderPerformance>():null;
            if(rider){footfalls=rider.Footfalls;takeoffs=rider.Takeoffs;landings=rider.Landings;}
            if(App){tracked=App.Rules;delivered=App.Rules.State.delivered;homeReturns=App.Rules.HomeReturns;recipe=App.Rules.State.recipeId;lastElapsed=App.Rules.State.elapsed;}
        }
        public AudioClip Clip(string name)=>clips!=null&&clips.TryGetValue(name,out var clip)?clip:null;
        public void Play(Cue cue,float volume=1)
        {
            if(!Ready)return;
            string name=cue.ToString().ToLowerInvariant();
            if(cue==Cue.Coin)name="coin";
            var clip=Clip(name);if(clip)ui.PlayOneShot(clip,volume*Effects*Level(cue));
        }
        static float Level(Cue cue)=>cue switch{Cue.Tick=>.28f,Cue.Page=>.45f,Cue.Confirm=>.42f,Cue.Back=>.38f,Cue.Coin=>.55f,Cue.Deliver=>.62f,Cue.Ding=>.5f,Cue.Sizzle=>.35f,Cue.Lullaby=>.5f,Cue.Hospital=>.45f,Cue.Low=>.5f,Cue.Accept=>.5f,_=>.5f};
        public void At(string name,Vector3 position,float volume,float minDistance=6,float maxDistance=160,float pitch=1)
        {
            var clip=Clip(name);if(!clip||world.Count==0)return;
            var source=world[nextWorld];nextWorld=(nextWorld+1)%world.Count;
            source.transform.position=position;source.minDistance=minDistance;source.maxDistance=maxDistance;source.pitch=pitch;
            source.clip=clip;source.volume=volume*Effects;source.Play();
        }
        public void Caw(Vector3 position){At("caw"+dice.Next(2),position,.55f,5,110,.94f+(float)dice.NextDouble()*.12f);}
        void PlaySelf(string name,float volume,float pitch=1)
        {
            var clip=Clip(name);if(!clip)return;self.pitch=pitch;self.PlayOneShot(clip,volume*Effects);
        }

        void Update()
        {
            if(!Ready)
            {
                if(building==null||!building.IsCompleted)return;
                if(building.IsFaulted){Debug.LogWarning("Sound synthesis failed: "+building.Exception?.GetBaseException().Message);building=null;return;}
                Create(building.Result);building=null;
            }
            if(!App)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            var motor=App.Motor;var rules=App.Rules;bool playing=App.InGame;
            Vector3 position=motor.transform.position;
            float night=App.Lighting?App.Lighting.Night:0;
            float speed=Mathf.Clamp01(motor.Velocity.magnitude/26);
            // Beds: wind follows speed and height, the sea follows the quay, crickets follow the night.
            float windTarget=playing?.035f+.05f*Mathf.Clamp01(position.y/40)+(motor.Mounted?.07f+.50f*speed:0)+(motor.Boosting?.10f:0):.06f;
            Fade(wind,windTarget*Effects,.9f,dt);wind.pitch=Mathf.Lerp(wind.pitch,.78f+speed*.55f+(motor.Boosting?.08f:0),dt*2.5f);
            float shore=Mathf.Clamp01(1-Mathf.Max(0,position.z+76)/150f);
            float seaTarget=playing?(.08f+.40f*shore*shore)*(1-.45f*Mathf.Clamp01((position.y-8)/60)):.22f;
            Fade(sea,seaTarget*Effects,.5f,dt);
            Fade(crickets,night*.15f*(1-Mathf.Clamp01((position.y-6)/30))*(playing?1:.5f)*Effects,.3f,dt);
            float musicTarget=!playing?1:App.Hud&&App.Hud.BakeryOpen?.55f:App.SleepFade>.05f?.5f:0;
            if(musicTarget<=0){musicQuiet+=dt;}else{if(musicQuiet>4&&music.volume<.01f)music.time=0;musicQuiet=0;}
            Fade(music,musicTarget*Music,musicTarget*Music>music.volume?.35f:.45f,dt);
            if(!playing)return;
            // A new game replaces the rules; resynchronise instead of announcing the difference.
            if(rules!=tracked){tracked=rules;delivered=rules.State.delivered;homeReturns=rules.HomeReturns;recipe=rules.State.recipeId;lastElapsed=rules.State.elapsed;bellHour=-1;}
            if(rider)
            {
                if(rider.Footfalls!=footfalls){footfalls=rider.Footfalls;if(motor.Grounded&&!motor.Mounted)Step(rider.LastFootfall,rider.LastFootfallSoft);}
                if(rider.Takeoffs!=takeoffs){takeoffs=rider.Takeoffs;PlaySelf("takeoff",.48f,.96f+(float)dice.NextDouble()*.08f);}
                if(rider.Landings!=landings){landings=rider.Landings;PlaySelf("landing",.62f);}
            }
            if(motor.Boosting&&!boosting)PlaySelf("boost",.42f,.95f+(float)dice.NextDouble()*.1f);
            boosting=motor.Boosting;
            if(rules.State.delivered>delivered)Play(Cue.Deliver);
            delivered=rules.State.delivered;
            if(rules.HomeReturns>homeReturns)Play(Cue.Hospital);
            homeReturns=rules.HomeReturns;
            if(recipe!=rules.State.recipeId)
            {
                if(recipe==""&&rules.State.recipeId!="")Play(Cue.Sizzle);
                else if(rules.State.recipeId=="")Play(Cue.Ding);
                recipe=rules.State.recipeId;
            }
            if(rules.State.energy<20&&!lowWarned){lowWarned=true;Play(Cue.Low);}
            else if(rules.State.energy>30)lowWarned=false;
            ClockTower(rules,dt);
            // Songbirds by day near the ground; gulls call over the harbour.
            birdTimer-=dt;
            if(birdTimer<=0)
            {
                birdTimer=2.5f+(float)dice.NextDouble()*6;
                if(night<.4f&&position.y<32&&shore<.9f)
                {
                    float angle=(float)dice.NextDouble()*Mathf.PI*2,range=9+(float)dice.NextDouble()*18;
                    At("bird"+dice.Next(5),position+new Vector3(Mathf.Cos(angle)*range,4+(float)dice.NextDouble()*5,Mathf.Sin(angle)*range),.30f*(1-night),4,60,.92f+(float)dice.NextDouble()*.16f);
                }
            }
            gullTimer-=dt;
            if(gullTimer<=0&&Life)
            {
                gullTimer=3.5f+(float)dice.NextDouble()*7;
                var gull=Life.NearestGull(Camera.main?Camera.main.transform.position:position);
                if(gull.HasValue&&night<.7f)At("gull"+dice.Next(2),gull.Value,.5f,8,130,.9f+(float)dice.NextDouble()*.2f);
            }
        }
        void ClockTower(Rules rules,float dt)
        {
            if(Life)BellPosition=Life.BellTower;
            // The clock tower strikes at six, noon and six in the evening. A sleep skip is silent.
            int hour=Mathf.FloorToInt((float)rules.Hour);
            bool jumped=rules.State.elapsed-lastElapsed>5;lastElapsed=rules.State.elapsed;
            if(bellHour<0||jumped){bellHour=hour;}
            else if(hour!=bellHour){bellHour=hour;if(hour==6||hour==12||hour==18){bellStrikes=hour==12?3:2;bellClock=0;}}
            if(bellStrikes>0)
            {
                bellClock-=dt;
                if(bellClock<=0){bellStrikes--;bellClock=1.7f;At("bell",BellPosition,.9f,25,520,1);}
            }
        }
        void Step(Vector3 point,bool soft)
        {
            string surface="stone";
            if(Physics.Raycast(point+Vector3.up*.4f,Vector3.down,out var hit,1.2f,1<<8,QueryTriggerInteraction.Ignore))
            {
                string n=hit.collider.name;
                if(n.Contains("Ground"))surface="grass";
                else if(n.Contains("Airship")||n.Contains("Paint_6"))surface="wood";
            }
            Footsteps++;
            PlaySelf(surface+dice.Next(surface=="wood"?3:4),(soft?.16f:.28f)*(surface=="grass"?1.1f:1),.93f+(float)dice.NextDouble()*.14f);
        }
        static void Fade(AudioSource source,float target,float rate,float dt){if(source)source.volume=Mathf.MoveTowards(source.volume,target,rate*dt);}

        // ---------------------------------------------------------------- synthesis
        sealed class Sample { public float[] Data; public int Rate; }
        sealed class Noise
        {
            uint state;float b0,b1,b2,b3,b4,b5,b6,brown;
            public Noise(uint seed){state=seed|1;}
            public float White(){state^=state<<13;state^=state>>17;state^=state<<5;return (state&0xFFFFFF)/8388608f-1;}
            public float Uniform()=>(White()+1)*.5f;
            public float Pink()
            {
                float w=White();b0=.99886f*b0+w*.0555179f;b1=.99332f*b1+w*.0750759f;b2=.969f*b2+w*.153852f;b3=.8665f*b3+w*.3104856f;b4=.55f*b4+w*.5329522f;b5=-.7616f*b5-w*.016898f;
                float p=b0+b1+b2+b3+b4+b5+b6+w*.5362f;b6=w*.115926f;return p*.11f;
            }
            public float Brown(){brown=(brown+.02f*White())/1.02f;return brown*3.5f;}
        }
        sealed class Filter
        {
            float b0,b1,b2,a1,a2,x1,x2,y1,y2;
            public static Filter Band(float f,float q,int rate){var r=new Filter();r.SetBand(f,q,rate);return r;}
            public static Filter Low(float f,float q,int rate){var r=new Filter();r.SetLow(f,q,rate);return r;}
            public static Filter High(float f,float q,int rate){var r=new Filter();r.SetHigh(f,q,rate);return r;}
            public void SetBand(float f,float q,int rate){float w=2*MathF.PI*f/rate,a=MathF.Sin(w)/(2*q),c=MathF.Cos(w),n=1+a;b0=a/n;b1=0;b2=-a/n;a1=-2*c/n;a2=(1-a)/n;}
            public void SetLow(float f,float q,int rate){float w=2*MathF.PI*f/rate,a=MathF.Sin(w)/(2*q),c=MathF.Cos(w),n=1+a;b0=(1-c)*.5f/n;b1=(1-c)/n;b2=b0;a1=-2*c/n;a2=(1-a)/n;}
            public void SetHigh(float f,float q,int rate){float w=2*MathF.PI*f/rate,a=MathF.Sin(w)/(2*q),c=MathF.Cos(w),n=1+a;b0=(1+c)*.5f/n;b1=-(1+c)/n;b2=b0;a1=-2*c/n;a2=(1-a)/n;}
            public float Run(float x){float y=b0*x+b1*x1+b2*x2-a1*y1-a2*y2;x2=x1;x1=x;y2=y1;y1=y;return y;}
        }
        sealed class Bank
        {
            public readonly Dictionary<string,Sample> Clips=new Dictionary<string,Sample>();
            public string Summary="";
            void Add(string name,float[] data,int rate,float peak)
            {
                // Remove DC from the looping beds; a mean subtraction keeps a seamless loop seamless.
                // One-shots start and end at silence and are left untouched to avoid a click.
                if(name=="wind"||name=="sea"||name=="crickets"||name=="waltz")
                {
                    double sum=0;foreach(var v in data)sum+=v;float dc=(float)(sum/Math.Max(1,data.Length));
                    for(int i=0;i<data.Length;i++)data[i]-=dc;
                }
                float max=0;foreach(var v in data)max=Math.Max(max,Math.Abs(v));
                if(max>0)for(int i=0;i<data.Length;i++)data[i]*=peak/max;
                Clips[name]=new Sample{Data=data,Rate=rate};
            }
            public static Bank Build()
            {
                var b=new Bank();var noise=new Noise(19890729);
                var watch=System.Diagnostics.Stopwatch.StartNew();
                b.Add("wind",Wind(noise),BedRate,.9f);
                b.Add("sea",Sea(noise),BedRate,.9f);
                b.Add("crickets",Crickets(noise),BedRate,.8f);
                for(int i=0;i<5;i++)b.Add("bird"+i,Bird(noise,i),Rate,.7f);
                for(int i=0;i<2;i++)b.Add("gull"+i,Gull(noise,i),Rate,.75f);
                for(int i=0;i<2;i++)b.Add("caw"+i,Caw(noise,i),Rate,.8f);
                for(int i=0;i<4;i++){b.Add("stone"+i,Stone(noise,i),Rate,.8f);b.Add("grass"+i,Grass(noise),Rate,.7f);}
                for(int i=0;i<3;i++)b.Add("wood"+i,Wood(noise,i),Rate,.8f);
                b.Add("takeoff",Sweep(noise,1.05f,260,1500,.8f,.18f),Rate,.85f);
                b.Add("boost",Sweep(noise,.8f,520,2600,1.1f,.06f),Rate,.8f);
                b.Add("landing",Landing(noise),Rate,.85f);
                b.Add("tick",Tick(noise),Rate,.6f);
                b.Add("confirm",Bells(new[]{(0f,88),(.07f,93)},.16f,.35f),Rate,.6f);
                b.Add("accept",Bells(new[]{(0f,81),(.08f,85),(.16f,88)},.2f,.6f),Rate,.6f);
                b.Add("back",Bells(new[]{(0f,81),(.07f,76)},.14f,.3f),Rate,.55f);
                b.Add("page",Page(noise),Rate,.6f);
                b.Add("coin",Coins(noise),Rate,.7f);
                var deliver=Bells(new[]{(0f,77),(.11f,81),(.22f,84),(.36f,89)},.55f,1.7f);
                var coin=Coins(noise);for(int i=0;i<coin.Length&&i+(int)(.55f*Rate)<deliver.Length;i++)deliver[i+(int)(.55f*Rate)]+=coin[i]*.45f;
                b.Add("deliver",deliver,Rate,.75f);
                b.Add("ding",Ding(),Rate,.7f);
                b.Add("bell",ClockBell(noise),Rate,.9f);
                b.Add("sizzle",Sizzle(noise),Rate,.5f);
                b.Add("lullaby",Bells(new[]{(0f,84),(.38f,81),(.76f,77),(1.14f,72)},.9f,2.6f),Rate,.6f);
                b.Add("hospital",Bells(new[]{(0f,74),(.3f,69)},.7f,1.4f),Rate,.5f);
                b.Add("low",Bells(new[]{(0f,64),(.22f,60)},.35f,.9f),Rate,.5f);
                b.Add("waltz",Waltz(noise),Rate,.85f);
                b.Summary=$"{b.Clips.Count} clips synthesized in {watch.ElapsedMilliseconds} ms";
                return b;
            }
            static float Midi(int note)=>440*MathF.Pow(2,(note-69)/12f);
            static float[] Loop(float[] raw,int length)
            {
                int fade=raw.Length-length;var o=new float[length];Array.Copy(raw,o,length);
                for(int i=0;i<fade;i++){float t=(float)i/fade;o[i]=raw[length+i]*MathF.Cos(t*MathF.PI*.5f)+raw[i]*MathF.Sin(t*MathF.PI*.5f);}
                return o;
            }
            static float[] Wind(Noise n)
            {
                int length=8*BedRate;var raw=new float[length+BedRate/2];
                var body=Filter.Band(380,.6f,BedRate);var soften=Filter.Low(1500,.7f,BedRate);var whistle=Filter.Band(880,4,BedRate);
                for(int i=0;i<raw.Length;i++)
                {
                    float t=(float)i/BedRate,p=n.Pink();
                    float gust=.70f+.18f*MathF.Sin(2*MathF.PI*.125f*t)+.12f*MathF.Sin(2*MathF.PI*.375f*t+1);
                    float hint=.5f+.5f*MathF.Sin(2*MathF.PI*.25f*t+2);
                    raw[i]=soften.Run(body.Run(p))*gust+whistle.Run(p)*.35f*hint*gust;
                }
                return Loop(raw,length);
            }
            static float[] Sea(Noise n)
            {
                int length=12*BedRate;var raw=new float[length+BedRate/2];
                var rumble=Filter.Low(380,.7f,BedRate);var washHigh=Filter.High(650,.7f,BedRate);var washLow=Filter.Low(4200,.7f,BedRate);
                float[] strength={1,.78f,.9f};
                for(int i=0;i<raw.Length;i++)
                {
                    float t=(float)i/BedRate,phase=(t%4)/4;int wave=(int)(t/4)%3;
                    float e=phase<.22f?(phase/.22f)*(phase/.22f):MathF.Exp(-(phase-.22f)*4.2f);
                    e*=strength[wave];
                    raw[i]=rumble.Run(n.Brown())*.55f*(.8f+.2f*e)+washLow.Run(washHigh.Run(n.Pink()))*e*.85f;
                }
                return Loop(raw,length);
            }
            static float[] Crickets(Noise n)
            {
                int length=6*BedRate;var raw=new float[length+BedRate/2];
                float[] carrier={4400,4850,5200},period={.62f,.51f,.74f},amp={.5f,.34f,.26f};int[] pulses={3,4,3};
                for(int c=0;c<3;c++)
                {
                    float offset=c*.17f;
                    for(int i=0;i<raw.Length;i++)
                    {
                        float t=(float)i/BedRate+offset,local=t%period[c];
                        int pulse=(int)(local/.045f);float within=local-pulse*.045f;
                        if(pulse>=pulses[c]||within>.02f)continue;
                        float env=MathF.Sin(MathF.PI*within/.02f);
                        raw[i]+=MathF.Sin(2*MathF.PI*carrier[c]*t)*env*env*amp[c];
                    }
                }
                return Loop(raw,length);
            }
            static float[] Bird(Noise n,int variant)
            {
                var r=new System.Random(40+variant);int notes=3+r.Next(5);
                var o=new float[(int)(1.4f*Rate)];float time=0,phase=0;
                for(int k=0;k<notes&&time<1.2f;k++)
                {
                    float duration=.045f+(float)r.NextDouble()*.08f,f0=2500+(float)r.NextDouble()*2700,f1=f0*(.7f+(float)r.NextDouble()*.7f);
                    bool trill=r.NextDouble()<.3;int start=(int)(time*Rate),count=(int)(duration*Rate);
                    for(int i=0;i<count&&start+i<o.Length;i++)
                    {
                        float u=(float)i/count,f=f0+(f1-f0)*u;phase+=2*MathF.PI*f/Rate;
                        float env=MathF.Sin(MathF.PI*u);env*=env;if(trill)env*=.6f+.4f*MathF.Sin(2*MathF.PI*45*i/Rate);
                        o[start+i]+=(MathF.Sin(phase)+.12f*MathF.Sin(phase*2))*env;
                    }
                    time+=duration+.02f+(float)r.NextDouble()*.07f;
                }
                return o;
            }
            static float[] Gull(Noise n,int variant)
            {
                int calls=2+variant;var o=new float[(int)((calls*.44f+.2f)*Rate)];var formant=Filter.Band(1650,1.1f,Rate);
                for(int c=0;c<calls;c++)
                {
                    int start=(int)(c*.44f*Rate),count=(int)(.34f*Rate);float phase=0,drop=1-c*.06f;
                    for(int i=0;i<count;i++)
                    {
                        float u=(float)i/count,t=(float)i/Rate;
                        float f=(1450*MathF.Exp(-u*.62f))*drop*(1+.03f*MathF.Sin(2*MathF.PI*14*t));phase+=2*MathF.PI*f/Rate;
                        float s=0;for(int h=1;h<=6;h++)s+=MathF.Sin(phase*h)/h;
                        float env=MathF.Min(1,u/.08f)*MathF.Exp(-u*2.2f);
                        o[start+i]+=formant.Run(s+n.White()*.08f)*env;
                    }
                }
                return o;
            }
            static float[] Caw(Noise n,int variant)
            {
                var o=new float[(int)(.8f*Rate)];var f1=Filter.Band(1150,1.5f,Rate);var f2=Filter.Band(2500,2,Rate);
                for(int c=0;c<2;c++)
                {
                    int start=(int)(c*.38f*Rate),count=(int)((.26f+variant*.04f)*Rate);float phase=0;
                    for(int i=0;i<count;i++)
                    {
                        float u=(float)i/count,f=(560-90*u)*(1-variant*.05f);phase+=2*MathF.PI*f/Rate;
                        float s=0;for(int h=1;h<=10;h++)s+=MathF.Sin(phase*h)/h;
                        s+=n.White()*.35f*(.5f+.5f*MathF.Sin(phase));
                        float env=MathF.Min(1,u/.05f)*(1-MathF.Pow(u,3));
                        o[start+i]+=(f1.Run(s)+f2.Run(s)*.5f)*env;
                    }
                }
                return o;
            }
            static float[] Stone(Noise n,int variant)
            {
                var o=new float[(int)(.12f*Rate)];var click=Filter.Band(2000+variant*260,1.4f,Rate);var scuff=Filter.Band(620,.8f,Rate);
                float body=118+variant*9;
                for(int i=0;i<o.Length;i++)
                {
                    float t=(float)i/Rate;
                    o[i]=click.Run(n.White())*MathF.Exp(-t/.011f)+MathF.Sin(2*MathF.PI*body*t)*MathF.Exp(-t/.032f)*.55f+scuff.Run(n.Pink())*MathF.Exp(-t/.04f)*.35f;
                }
                return o;
            }
            static float[] Grass(Noise n)
            {
                var o=new float[(int)(.16f*Rate)];var low=Filter.Low(900,.7f,Rate);var rustle=Filter.High(2600,.7f,Rate);
                for(int i=0;i<o.Length;i++)
                {
                    float t=(float)i/Rate,env=MathF.Min(1,t/.012f)*MathF.Exp(-t/.06f);
                    o[i]=low.Run(n.Pink())*env+rustle.Run(n.White())*MathF.Exp(-t/.03f)*.3f;
                }
                return o;
            }
            static float[] Wood(Noise n,int variant)
            {
                var o=new float[(int)(.18f*Rate)];var click=Filter.Band(1600,1.2f,Rate);
                float a=185+variant*14,b=415+variant*21;
                for(int i=0;i<o.Length;i++)
                {
                    float t=(float)i/Rate;
                    o[i]=(MathF.Sin(2*MathF.PI*a*t)+.5f*MathF.Sin(2*MathF.PI*b*t))*MathF.Exp(-t/.05f)+click.Run(n.White())*MathF.Exp(-t/.008f)*.6f;
                }
                return o;
            }
            static float[] Sweep(Noise n,float seconds,float from,float to,float q,float attack)
            {
                var o=new float[(int)(seconds*Rate)];var band=Filter.Band(from,q,Rate);
                for(int i=0;i<o.Length;i++)
                {
                    float u=(float)i/o.Length,t=(float)i/Rate;
                    if(i%64==0)band.SetBand(from*MathF.Pow(to/from,MathF.Sqrt(u)),q,Rate);
                    float env=t<attack?t/attack:MathF.Pow(1-(t-attack)/(seconds-attack),1.6f);
                    o[i]=band.Run(n.Pink())*env+MathF.Sin(2*MathF.PI*78*t)*MathF.Exp(-t/.12f)*.25f;
                }
                return o;
            }
            static float[] Landing(Noise n)
            {
                var o=new float[(int)(.65f*Rate)];var rustle=Filter.Band(900,.7f,Rate);
                for(int i=0;i<o.Length;i++)
                {
                    float t=(float)i/Rate,f=70-15*MathF.Min(1,t/.12f);
                    o[i]=MathF.Sin(2*MathF.PI*f*t)*MathF.Exp(-t/.085f)*.9f+rustle.Run(n.Pink())*MathF.Exp(-t/.14f)*.55f;
                }
                return o;
            }
            static float[] Tick(Noise n)
            {
                var o=new float[(int)(.035f*Rate)];
                for(int i=0;i<o.Length;i++){float t=(float)i/Rate,e=MathF.Exp(-t/.006f);o[i]=MathF.Sin(2*MathF.PI*2500*t)*e+n.White()*e*.18f;}
                return o;
            }
            static float[] Page(Noise n)
            {
                var o=new float[(int)(.24f*Rate)];var high=Filter.High(1800,.7f,Rate);
                for(int i=0;i<o.Length;i++){float t=(float)i/Rate,env=MathF.Min(1,t/.02f)*MathF.Exp(-t/.07f)*(.6f+.4f*MathF.Sin(2*MathF.PI*38*t));o[i]=high.Run(n.Pink())*env;}
                return o;
            }
            /// <summary>Music-box notes: a sine body with a bright inharmonic tine.</summary>
            static void Box(float[] o,float start,float frequency,float amplitude,float decay)
            {
                float[] ratio={1,2,3,4.16f},level={1,.22f,.07f,.10f},life={1,.5f,.33f,.22f};
                int first=(int)(start*Rate);
                for(int k=0;k<4;k++)
                {
                    float w=2*MathF.PI*frequency*ratio[k]/Rate;if(w>MathF.PI*.9f)continue;
                    float c=MathF.Cos(w),s=MathF.Sin(w),re=1,im=0,tau=decay*life[k];
                    int count=Math.Min(o.Length-first,(int)(tau*6*Rate));
                    float env=amplitude*level[k],fall=MathF.Exp(-1f/(tau*Rate)),attack=1,rise=MathF.Exp(-1f/(.002f*Rate));
                    for(int i=0;i<count;i++)
                    {
                        o[first+i]+=im*env*(1-attack);env*=fall;attack*=rise;
                        float nr=re*c-im*s;im=re*s+im*c;re=nr;
                        if((i&1023)==0){float m=1/MathF.Sqrt(re*re+im*im);re*=m;im*=m;}
                    }
                }
            }
            static float[] Bells((float time,int note)[] notes,float decay,float seconds)
            {
                var o=new float[(int)(seconds*Rate)];
                foreach(var (time,note) in notes)Box(o,time,Midi(note),1,decay);
                return o;
            }
            static float[] Coins(Noise n)
            {
                var o=new float[(int)(.6f*Rate)];float[] f={2093,3150,4400,5620,7190},a={1,.6f,.45f,.3f,.2f},d={.25f,.18f,.12f,.09f,.07f};
                float[] hits={0,.07f,.15f},loud={1,.8f,.65f};
                for(int h=0;h<3;h++)
                {
                    float jitter=.98f+n.Uniform()*.04f;int first=(int)(hits[h]*Rate);
                    for(int k=0;k<5;k++)for(int i=0;first+i<o.Length;i++){float t=(float)i/Rate;o[first+i]+=MathF.Sin(2*MathF.PI*f[k]*jitter*t)*a[k]*loud[h]*MathF.Exp(-t/d[k]);}
                }
                return o;
            }
            static float[] Ding()
            {
                var o=new float[(int)(2.4f*Rate)];float[] r={1,2.76f,5.40f,8.93f},a={1,.4f,.2f,.08f},d={1.2f,.5f,.25f,.12f};
                foreach(float start in new[]{0f,.45f})
                {
                    int first=(int)(start*Rate);
                    for(int k=0;k<4;k++)for(int i=0;first+i<o.Length;i++){float t=(float)i/Rate;o[first+i]+=MathF.Sin(2*MathF.PI*1568*r[k]*t)*a[k]*MathF.Exp(-t/d[k])*(start>0?.8f:1);}
                }
                return o;
            }
            static float[] ClockBell(Noise n)
            {
                var o=new float[(int)(4.6f*Rate)];var strike=Filter.Band(2100,1.5f,Rate);
                float[] r={.5f,1,1.19f,1.5f,2,2.51f,2.99f,4},a={.5f,.8f,.5f,.3f,.6f,.25f,.2f,.12f},d={4,3.2f,2.2f,1.8f,1.6f,1,.8f,.5f};
                for(int k=0;k<r.Length;k++)
                {
                    float f=196*r[k];
                    for(int i=0;i<o.Length;i++){float t=(float)i/Rate;o[i]+=(MathF.Sin(2*MathF.PI*f*t)+.6f*MathF.Sin(2*MathF.PI*(f+.55f)*t))*a[k]*MathF.Exp(-t/d[k]);}
                }
                for(int i=0;i<Rate/10;i++){float t=(float)i/Rate;o[i]+=strike.Run(n.White())*MathF.Exp(-t/.012f)*.8f;}
                return o;
            }
            static float[] Sizzle(Noise n)
            {
                var o=new float[(int)(1.3f*Rate)];var high=Filter.High(3200,.7f,Rate);
                for(int i=0;i<o.Length;i++)
                {
                    float t=(float)i/Rate,env=MathF.Min(1,t/.15f)*MathF.Min(1,(1.3f-t)/.4f);
                    float crackle=n.Uniform()>.9985f?n.White()*3:0;
                    o[i]=(high.Run(n.White())*.35f+crackle)*env;
                }
                return o;
            }
            /// <summary>An original sixteen-bar waltz in F: music-box melody, plucked bass and
            /// chords, a small room. The tail is wrapped into the start for a seamless loop.</summary>
            static float[] Waltz(Noise n)
            {
                const float beat=60f/138;int bars=16,length=(int)(bars*3*beat*Rate),tail=(int)(2.6f*Rate);
                var o=new float[length+tail];
                var melody=new (int bar,float at,int note,float beats)[]{
                    (0,0,69,2),(0,2,72,1),(1,0,77,2),(1,2,76,1),(2,0,74,1.5f),(2,1.5f,72,.5f),(2,2,70,1),(3,0,69,3),
                    (4,0,67,1),(4,1,70,1),(4,2,74,1),(5,0,72,2),(5,2,70,1),(6,0,69,1),(6,1,67,1),(6,2,69,1),(7,0,67,3),
                    (8,0,69,1),(8,1,74,1),(8,2,77,1),(9,0,77,1.5f),(9,1.5f,76,.5f),(9,2,74,1),(10,0,72,1),(10,1,69,1),(10,2,65,1),(11,0,67,2),(11,2,72,1),
                    (12,0,74,1),(12,1,77,1),(12,2,81,1),(13,0,79,1.5f),(13,1.5f,77,.5f),(13,2,74,1),(14,0,76,1),(14,1,72,1),(14,2,70,1),(15,0,77,3)};
                foreach(var m in melody)Box(o,(m.bar*3+m.at)*beat,Midi(m.note),.42f,.55f+m.beats*.22f);
                // F F Bb F Gm C7 F C7 | Dm Bb F C Dm Bb C7 F
                int[] roots={41,41,46,41,43,48,41,48,50,46,41,48,50,46,48,41};
                (int a,int b)[] chords={(57,60),(57,60),(58,62),(57,60),(58,62),(58,64),(57,60),(58,64),(57,62),(58,62),(57,60),(55,64),(57,62),(58,62),(58,64),(57,60)};
                for(int bar=0;bar<bars;bar++)
                {
                    float start=bar*3*beat;
                    Pluck(o,n,start,Midi(roots[bar]),.50f,1.25f,.996f);
                    for(int k=1;k<3;k++){Pluck(o,n,start+k*beat,Midi(chords[bar].a),.20f,.5f,.993f);Pluck(o,n,start+k*beat+.012f,Midi(chords[bar].b),.18f,.5f,.993f);}
                }
                Room(o);
                for(int i=0;i<tail;i++)o[i]+=o[length+i];
                var loop=new float[length];Array.Copy(o,loop,length);return loop;
            }
            static void Pluck(float[] o,Noise n,float start,float frequency,float amplitude,float seconds,float damping)
            {
                int period=Math.Max(2,(int)MathF.Round(Rate/frequency));var ring=new float[period];float previous=0,mean=0;
                for(int i=0;i<period;i++){previous=previous*.55f+n.White()*.45f;ring[i]=previous;mean+=previous/period;}
                // Without this the string's averaging filter keeps the burst's DC as a slow low thump.
                for(int i=0;i<period;i++)ring[i]-=mean;
                int first=(int)(start*Rate),count=Math.Min(o.Length-first,(int)(seconds*Rate)),index=0;
                for(int i=0;i<count;i++)
                {
                    int next=index+1==period?0:index+1;float v=ring[index];ring[index]=damping*.5f*(ring[index]+ring[next]);
                    float fade=i>count*.85f?(count-i)/(count*.15f):1;o[first+i]+=v*amplitude*fade;index=next;
                }
            }
            static void Room(float[] o)
            {
                int[] combs={1557,1617,1491,1422};int[] passes={225,556};
                var wet=new float[o.Length];
                foreach(int delay in combs){var line=new float[delay];int p=0;float low=0;for(int i=0;i<o.Length;i++){float y=line[p];low=y*.8f+low*.2f;line[p]=o[i]+low*.78f;p=(p+1)%delay;wet[i]+=y*.25f;}}
                foreach(int delay in passes){var line=new float[delay];int p=0;for(int i=0;i<o.Length;i++){float b=line[p],x=wet[i];float y=-x*.5f+b;line[p]=x+y*.5f;p=(p+1)%delay;wet[i]=y;}}
                for(int i=0;i<o.Length;i++)o[i]+=wet[i]*.22f;
            }
        }
    }
}
