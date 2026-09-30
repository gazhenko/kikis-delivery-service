using System.Collections.Generic;
using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    /// <summary>Background life around the delivery route: chimney smoke, gulls over the
    /// harbour, dust at takeoff and touchdown, lamplight after dusk and the painted ring
    /// on the court Kiki is heading for. Built at startup from the exported town layout.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class TownLife : MonoBehaviour
    {
        public GameApp App;
        public Material Puff,Lamps,Ring,GullBody,GullWing,GullTip,GullBeak;
        public Vector3 BellTower {get;private set;}=new Vector3(16,31,40);
        public int ChimneyCount=>chimneys.Count;
        public int LampCount {get;private set;}
        public int PuffCount=>puffs.Count;
        public int BoatCount=>boats!=null?boats.Length:0;
        public bool Lighthouse=>lighthouse;
        public bool RingVisible=>ring&&ring.gameObject.activeSelf;
        public Destination RingTarget {get;private set;}
        struct PuffState{public Vector3 Position,Velocity;public float Age,Life,Size,Growth,Seed,Rise;public Color Color;}
        sealed class Chimney{public Vector3 Top;public float Interval,Timer,Size;public bool Oven;}
        readonly List<PuffState> puffs=new List<PuffState>(320);
        readonly List<Chimney> chimneys=new List<Chimney>();
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> corners=new List<Vector2>(),sizes=new List<Vector2>();
        readonly List<Color> colors=new List<Color>();readonly List<int> indices=new List<int>();
        readonly List<int> order=new List<int>();
        Mesh puffMesh;
        Transform[] gulls,wingsLeft,wingsRight;
        Vector3[] gullCenter;float[] gullRadius,gullSpeed,gullAngle,gullFlap;
        Transform ring;Material ringPaint;float ringAlpha,ringReady;
        Transform[] boats;Vector3[] boatBase;Quaternion[] boatTurn;
        Transform lighthouse;Material beamPaint;
        RiderPerformance rider;
        int takeoffs,landings;double invulnerable;
        readonly System.Random dice=new System.Random(7);
        static readonly Vector3 Wind=new Vector3(.35f,0,-.55f);

        void Start()
        {
            var data=Resources.Load<TextAsset>("KorikoLayout");
            var layout=data?JsonUtility.FromJson<TownMap.Layout>(data.text):null;
            if(layout?.buildings!=null)foreach(var b in layout.buildings)
            {
                if(b.name=="Clock tower"){BellTower=new Vector3(b.x,b.y+31,b.z);continue;}
                if(b.name.Contains("greenhouse")||b.name.Contains("Greenhouse"))continue;
                // Chimney placement mirrors Tools/build_art.py building(): 27% across, 20% deep, lip at 1.18 h + 1.5 m.
                bool oven=b.name=="Osono_Bakery";int hash=0;foreach(char c in b.name)hash=hash*31+c;
                if(!oven&&((hash&0x7fffffff)%6)!=0)continue;
                chimneys.Add(new Chimney{Top=new Vector3(b.x+b.width*.27f,b.y+b.height*1.18f+1.62f,b.z+b.depth*.2f),Interval=oven?.55f:1.15f+(hash&7)*.07f,Timer=(hash&15)*.1f,Size=oven?1.25f:.9f,Oven=oven});
            }
            puffMesh=new Mesh{name="Drawn smoke and dust"};puffMesh.MarkDynamic();
            var puffObject=new GameObject("Chimney smoke and dust");puffObject.transform.SetParent(transform,false);
            puffObject.AddComponent<MeshFilter>().sharedMesh=puffMesh;var puffRenderer=puffObject.AddComponent<MeshRenderer>();
            puffRenderer.sharedMaterial=Puff;puffRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;puffRenderer.receiveShadows=false;
            BuildLamps();BuildRing();BuildGulls();BuildBoats();
            if(App)rider=App.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            if(rider){takeoffs=rider.Takeoffs;landings=rider.Landings;}
            if(App){invulnerable=App.Rules.State.invulnerableUntil;homeReturns=App.Rules.HomeReturns;}
            // Settle the chimneys so the first frame already has drifting smoke.
            for(int i=0;i<90;i++)Simulate(.1f);
        }
        public Vector3? NearestGull(Vector3 from)
        {
            if(gulls==null)return null;Vector3? best=null;float distance=110;
            foreach(var g in gulls){float d=Vector3.Distance(g.position,from);if(d<distance){distance=d;best=g.position;}}
            return best;
        }
        public void Emit(Vector3 position,Vector3 velocity,float size,float growth,float life,Color color,float rise=0)
        {
            if(puffs.Count>=300)return;
            puffs.Add(new PuffState{Position=position,Velocity=velocity,Size=size,Growth=growth,Life=life,Color=color,Seed=(float)dice.NextDouble()*6.28f,Rise=rise});
        }
        public void Burst(Vector3 position,int count,float speed,float size,float life,Color color)
        {
            for(int i=0;i<count;i++)
            {
                float a=(i+(float)dice.NextDouble()*.6f)/count*Mathf.PI*2;
                var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Emit(position+dir*.35f,dir*speed*(.7f+(float)dice.NextDouble()*.6f)+Vector3.up*.18f,size,size*1.3f,life*(.8f+(float)dice.NextDouble()*.4f),color);
            }
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.1f);if(dt<=0)return;
            Simulate(dt);
            if(App&&rider&&App.InGame)
            {
                var feet=App.Motor.transform.position+Vector3.up*.15f;
                // A low scuff of dust that hugs streets and paths, not a cloud; none on the airship's deck.
                var dust=new Color(.86f,.81f,.68f,.28f);bool street=App.Motor.transform.position.y<12;
                if(rider.Takeoffs!=takeoffs){takeoffs=rider.Takeoffs;if(street)Burst(feet,7,1.6f,.12f,.65f,dust);}
                if(rider.Landings!=landings){landings=rider.Landings;if(street)Burst(feet,8,1.8f,.13f,.75f,dust);}
                var rules=App.Rules;
                if(rules.State.invulnerableUntil>invulnerable+.5&&rules.HomeReturns==homeReturns)
                    Burst(App.Motor.transform.position+Vector3.up*1.3f,6,2.2f,.18f,.55f,new Color(.12f,.12f,.16f,.8f));
                invulnerable=rules.State.invulnerableUntil;homeReturns=rules.HomeReturns;
            }
            Gulls(dt);UpdateRing(dt);Boats();Beam();
        }
        void BuildBoats()
        {
            var found=new List<Transform>();
            foreach(var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name.StartsWith("Boat_",System.StringComparison.Ordinal))found.Add(t);
            boats=found.ToArray();boatBase=new Vector3[boats.Length];boatTurn=new Quaternion[boats.Length];
            for(int i=0;i<boats.Length;i++){boatBase[i]=boats[i].position;boatTurn[i]=boats[i].rotation;}
        }
        void Boats()
        {
            if(boats==null)return;
            // Moored hulls ride a slow swell on the same 12 Hz drawing clock as the painted sea.
            float t=Mathf.Floor(Time.time*12)/12;
            for(int i=0;i<boats.Length;i++)
            {
                if(!boats[i])continue;
                float lift=Mathf.Sin(t*.85f+i*1.7f)*.09f+Mathf.Sin(t*1.9f+i*.6f)*.03f;
                boats[i].SetPositionAndRotation(boatBase[i]+Vector3.up*lift,boatTurn[i]*Quaternion.Euler(Mathf.Sin(t*1.05f+i)*1.3f,0,Mathf.Sin(t*.78f+i*2.1f)*2.4f));
            }
        }
        void Beam()
        {
            if(!lighthouse||!beamPaint)return;
            // The lighthouse lamp turns once every fourteen seconds; it flares as it faces the viewer.
            var camera=Camera.main;if(!camera)return;
            var toward=camera.transform.position-lighthouse.position;
            float bearing=Mathf.Atan2(toward.x,toward.z),beam=Time.time*Mathf.PI*2/14;
            float facing=Mathf.Pow(Mathf.Max(0,Mathf.Cos(beam-bearing)),10);
            beamPaint.SetFloat("_Intensity",1.1f+facing*2.6f);
        }
        int homeReturns;
        void LateUpdate(){DrawPuffs();}
        void Simulate(float dt)
        {
            foreach(var c in chimneys)
            {
                c.Timer-=dt;if(c.Timer>0)continue;
                c.Timer=c.Interval*(.8f+(float)dice.NextDouble()*.4f);
                var tint=c.Oven?new Color(.95f,.93f,.89f,.50f):new Color(.89f,.89f,.88f,.36f);
                Emit(c.Top+new Vector3((float)dice.NextDouble()-.5f,0,(float)dice.NextDouble()-.5f)*.25f,Vector3.up*(c.Oven?1.15f:.9f),c.Size*.55f,c.Size*1.4f,6.5f+(float)dice.NextDouble()*2,tint,1);
            }
            for(int i=puffs.Count-1;i>=0;i--)
            {
                var p=puffs[i];p.Age+=dt;
                if(p.Age>=p.Life){puffs.RemoveAt(i);continue;}
                // Chimney smoke slows as it rises and leans away with the sea breeze; dust just spreads and settles.
                p.Velocity=Vector3.Lerp(p.Velocity,p.Rise>0?Wind+Vector3.up*.35f:Vector3.zero,1-Mathf.Exp(-dt*(p.Rise>0?.45f:2.6f)));
                p.Position+=p.Velocity*dt;puffs[i]=p;
            }
        }
        void DrawPuffs()
        {
            if(!puffMesh)return;
            var camera=Camera.main;Vector3 eye=camera?camera.transform.position:Vector3.zero;
            order.Clear();for(int i=0;i<puffs.Count;i++)order.Add(i);
            order.Sort((a,b)=>(puffs[b].Position-eye).sqrMagnitude.CompareTo((puffs[a].Position-eye).sqrMagnitude));
            vertices.Clear();corners.Clear();sizes.Clear();colors.Clear();indices.Clear();
            foreach(int i in order)
            {
                var p=puffs[i];float u=p.Age/p.Life;
                // Drawings change on the 12 Hz background clock: size steps rather than glides.
                float stepped=Mathf.Floor(u*p.Life*12)/(p.Life*12);
                float size=p.Size+p.Growth*Mathf.Sqrt(stepped);
                float alpha=p.Color.a*Mathf.Clamp01(stepped/.08f)*(1-Mathf.SmoothStep(0,1,(stepped-.55f)/.45f));
                var color=new Color(p.Color.r,p.Color.g,p.Color.b,alpha);
                int b=vertices.Count;
                for(int k=0;k<4;k++){vertices.Add(p.Position);corners.Add(new Vector2(k==0||k==3?-1:1,k<2?-1:1));sizes.Add(new Vector2(size,p.Seed+stepped*.6f));colors.Add(color);}
                indices.Add(b);indices.Add(b+1);indices.Add(b+2);indices.Add(b);indices.Add(b+2);indices.Add(b+3);
            }
            puffMesh.Clear();puffMesh.SetVertices(vertices);puffMesh.SetUVs(0,corners);puffMesh.SetUVs(1,sizes);puffMesh.SetColors(colors);puffMesh.SetTriangles(indices,0,false);
            puffMesh.bounds=new Bounds(new Vector3(0,40,0),new Vector3(900,200,900));
        }
        void BuildLamps()
        {
            var lamps=new List<Vector3>();
            foreach(var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(t.name.StartsWith("LampGlow",System.StringComparison.Ordinal))lamps.Add(t.position);
                if(t.name.StartsWith("LighthouseGlow",System.StringComparison.Ordinal))lighthouse=t;
            }
            if(lighthouse&&Lamps)
            {
                var beam=new Mesh{name="Lighthouse lamp"};beam.vertices=new Vector3[4];
                beam.uv=new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)};
                beam.SetUVs(1,new[]{new Vector2(5,1),new Vector2(5,1),new Vector2(5,1),new Vector2(5,1)});
                beam.triangles=new[]{0,1,2,0,2,3};beam.bounds=new Bounds(Vector3.zero,Vector3.one*12);
                var lamp=new GameObject("Lighthouse lamp");lamp.transform.SetParent(lighthouse,false);
                lamp.AddComponent<MeshFilter>().sharedMesh=beam;var r=lamp.AddComponent<MeshRenderer>();
                beamPaint=new Material(Lamps);r.sharedMaterial=beamPaint;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            LampCount=lamps.Count;if(lamps.Count==0||!Lamps)return;
            var v=new List<Vector3>();var uv=new List<Vector2>();var kind=new List<Vector2>();var tris=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 k)
            {
                int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
                uv.Add(new Vector2(-1,-1));uv.Add(new Vector2(1,-1));uv.Add(new Vector2(1,1));uv.Add(new Vector2(-1,1));
                for(int n=0;n<4;n++)kind.Add(k);tris.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            foreach(var lamp in lamps)
            {
                Quad(lamp,lamp,lamp,lamp,new Vector2(1.05f,1));
                float floor=Physics.Raycast(lamp+Vector3.down*.5f,Vector3.down,out var hit,7,1<<8,QueryTriggerInteraction.Ignore)?hit.point.y+.05f:lamp.y-4.15f;
                const float r=3.4f;
                Quad(new Vector3(lamp.x-r,floor,lamp.z-r),new Vector3(lamp.x+r,floor,lamp.z-r),new Vector3(lamp.x+r,floor,lamp.z+r),new Vector3(lamp.x-r,floor,lamp.z+r),new Vector2(0,0));
            }
            var mesh=new Mesh{name="Painted lamplight"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,kind);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();
            var b=mesh.bounds;b.Expand(4);mesh.bounds=b;
            var go=new GameObject("Street lamplight");go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r2=go.AddComponent<MeshRenderer>();r2.sharedMaterial=Lamps;
            r2.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r2.receiveShadows=false;
        }
        void BuildRing()
        {
            if(!Ring)return;
            var mesh=new Mesh{name="Delivery court ring"};
            mesh.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)};
            mesh.uv=new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();
            var go=new GameObject("Delivery court ring");go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();
            ringPaint=new Material(Ring);r.sharedMaterial=ringPaint;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            ring=go.transform;go.SetActive(false);
        }
        void UpdateRing(float dt)
        {
            if(!ring||!App)return;
            var rules=App.Rules;Destination target=null;
            if(App.InGame)
            {
                var active=rules.Active;
                if(active!=null)target=Catalog.FindDestination(active.destination);
                else if(!rules.AtHome&&rules.State.position.HorizontalDistance(Catalog.Home.Landing)>9)target=Catalog.Home;
            }
            if(target!=null)RingTarget=target;
            ringAlpha=Mathf.MoveTowards(ringAlpha,target!=null?1:0,dt*2);
            ring.gameObject.SetActive(ringAlpha>.01f&&RingTarget!=null);
            if(!ring.gameObject.activeSelf)return;
            var p=RingTarget.Landing;
            ring.position=new Vector3((float)p.x,(float)p.y+.03f,(float)p.z);ring.localScale=Vector3.one*(float)RingTarget.Radius;
            bool inside=rules.Near(RingTarget)&&rules.Grounded&&rules.CurrentSpeed<2.5;
            ringReady=Mathf.MoveTowards(ringReady,inside?1:0,dt*3);
            ringPaint.SetFloat("_Alpha",ringAlpha*(RingTarget==Catalog.Home&&rules.Active==null?.75f:1));ringPaint.SetFloat("_Ready",ringReady);
        }
        void BuildGulls()
        {
            if(!GullBody||!GullWing)return;
            Vector3[] centers={new Vector3(-62,17,-102),new Vector3(-14,13,-92),new Vector3(38,22,-112),new Vector3(92,15,-90),new Vector3(142,19,-101),new Vector3(12,27,-140),new Vector3(70,11,-84)};
            int n=centers.Length;gulls=new Transform[n];wingsLeft=new Transform[n];wingsRight=new Transform[n];
            gullCenter=centers;gullRadius=new float[n];gullSpeed=new float[n];gullAngle=new float[n];gullFlap=new float[n];
            for(int i=0;i<n;i++)
            {
                var root=new GameObject("Gull "+i).transform;root.SetParent(transform,false);gulls[i]=root;
                Part(root,PrimitiveType.Sphere,Vector3.zero,new Vector3(.26f,.24f,.72f),GullBody);
                Part(root,PrimitiveType.Sphere,new Vector3(0,.09f,.36f),new Vector3(.21f,.2f,.23f),GullBody);
                Part(root,PrimitiveType.Sphere,new Vector3(0,.07f,.51f),new Vector3(.05f,.045f,.15f),GullBeak);
                Part(root,PrimitiveType.Sphere,new Vector3(0,.02f,-.40f),new Vector3(.22f,.05f,.2f),GullBody);
                wingsLeft[i]=Wing(root,-1);wingsRight[i]=Wing(root,1);
                root.localScale=Vector3.one*1.25f;
                gullRadius[i]=11+(i*7)%17;gullSpeed[i]=(.20f+(i%3)*.05f)*(i%2==0?1:-1);gullAngle[i]=i*1.7f;gullFlap[i]=i*2.3f;
            }
        }
        void Part(Transform parent,PrimitiveType kind,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(kind);Destroy(obj.GetComponent<Collider>());obj.name="Gull shape";obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;var r=obj.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Transform Wing(Transform parent,int side)
        {
            // A long bent gull wing with a dark drawn tip, double-sided.
            var pivot=new GameObject(side<0?"Left wing":"Right wing").transform;pivot.SetParent(parent,false);pivot.localPosition=new Vector3(side*.1f,.06f,.05f);
            Vector3[] wing={new Vector3(0,0,.16f),new Vector3(.45f,.07f,.14f),new Vector3(.86f,0,.02f),new Vector3(.80f,0,-.12f),new Vector3(.42f,.03f,-.15f),new Vector3(0,0,-.12f)};
            Vector3[] tip={new Vector3(.86f,0,.02f),new Vector3(1.12f,-.04f,-.10f),new Vector3(.80f,0,-.12f)};
            Surface(pivot,wing,side,GullWing);Surface(pivot,tip,side,GullTip?GullTip:GullWing);
            return pivot;
        }
        static void Surface(Transform pivot,Vector3[] source,int side,Material material)
        {
            var points=(Vector3[])source.Clone();var normals=new Vector3[points.Length];
            for(int i=0;i<points.Length;i++){points[i].x*=side;normals[i]=Vector3.up;}
            var tris=new List<int>();for(int i=1;i<points.Length-1;i++)tris.AddRange(new[]{0,i,i+1,0,i+1,i});
            var mesh=new Mesh{name="Drawn gull wing",vertices=points,triangles=tris.ToArray(),normals=normals};mesh.RecalculateBounds();
            var go=new GameObject("Wing surface");go.transform.SetParent(pivot,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void Gulls(float dt)
        {
            if(gulls==null)return;
            float drawn=Mathf.Floor(Time.time*12)/12;
            for(int i=0;i<gulls.Length;i++)
            {
                gullAngle[i]+=gullSpeed[i]*dt;float a=gullAngle[i],r=gullRadius[i];
                var c=gullCenter[i];
                var position=c+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a*.7f+i)*1.6f,Mathf.Sin(a)*r);
                var tangent=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*Mathf.Sign(gullSpeed[i]);
                gulls[i].position=position;
                float bank=Mathf.Sign(gullSpeed[i])*22;
                gulls[i].rotation=Quaternion.LookRotation(tangent,Vector3.up)*Quaternion.Euler(0,0,bank);
                // Long glides with an occasional run of wingbeats.
                float cycle=Mathf.Repeat(drawn+gullFlap[i],9.5f);
                float flap=cycle<1.6f?Mathf.Sin(cycle*10.5f)*34:9+Mathf.Sin(drawn*1.3f+i)*3;
                wingsLeft[i].localRotation=Quaternion.Euler(0,0,-flap);wingsRight[i].localRotation=Quaternion.Euler(0,0,flap);
            }
        }
    }
}
