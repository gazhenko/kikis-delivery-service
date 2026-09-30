using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    /// <summary>The collected parcel hangs from the broom behind Kiki, and the moonlight
    /// lantern hangs from the handle once fitted. Both swing on the broom's motion and use
    /// the rider's cel paint, so a smear drawing bends them with the rest of the pose.</summary>
    [DefaultExecutionOrder(45)]
    public sealed class RiderProps : MonoBehaviour
    {
        public GameApp App;
        public TownLife Life;
        public Material Paper,String,FragilePaper,FragileRibbon,Crate,Strap,Canvas,Rope,Brass,LanternGlass,Outline,LanternHalo;
        public bool ParcelShown=>parcel&&parcel.gameObject.activeSelf&&parcelScale>.5f;
        public bool LanternShown=>lantern&&lantern.gameObject.activeSelf;
        public Vector3 ParcelPosition=>parcel?parcel.position:Vector3.zero;
        RiderPerformance rider;
        Transform parcel,parcelBody,string1,lantern,lanternWire;
        Renderer body,outline,band1,band2,knot;
        MeshFilter bodyFilter,outlineFilter;
        Mesh boxMesh,boxInk,sackMesh;
        Vector3 lastTie,tieVelocity,swing,swingVelocity,lastHook,hookVelocity,lanternSwing,lanternVelocity;
        float parcelScale,popClock=10;bool popping,delivering;
        string activeId="";int delivered;DeliveryKind kind;double weight;

        void Start()
        {
            rider=GetComponent<RiderPerformance>();
            boxMesh=Box(false);boxInk=Box(true);sackMesh=Sack();
            // Hang from the unscaled flight-pose frame; the imported rider root carries an anchor-fit scale.
            var root=App?App.Motor.Visual:transform;
            parcel=new GameObject("Carried parcel").transform;parcel.SetParent(root,false);
            parcelBody=new GameObject("Parcel body").transform;parcelBody.SetParent(parcel,false);
            (bodyFilter,body)=Shape(parcelBody,"Wrapped parcel",boxMesh,Paper);
            (outlineFilter,outline)=Shape(parcelBody,"Ink edge",boxInk,Outline);
            band1=Shape(parcelBody,"String",boxMesh,String).renderer;band2=Shape(parcelBody,"String",boxMesh,String).renderer;
            knot=Shape(parcelBody,"Knot",sackMesh,String).renderer;
            string1=Shape(root,"Parcel string",Cylinder(),Rope).renderer.transform;
            parcel.gameObject.SetActive(false);string1.gameObject.SetActive(false);
            lantern=new GameObject("Moonlight lantern").transform;lantern.SetParent(root,false);
            var cylinder=Cylinder();
            Shape(lantern,"Lantern cap",cylinder,Brass).renderer.transform.localScale=new Vector3(.13f,.022f,.13f);
            var glass=Shape(lantern,"Lantern glass",cylinder,LanternGlass).renderer.transform;glass.localPosition=new Vector3(0,-.085f,0);glass.localScale=new Vector3(.10f,.06f,.10f);
            var foot=Shape(lantern,"Lantern base",cylinder,Brass).renderer.transform;foot.localPosition=new Vector3(0,-.16f,0);foot.localScale=new Vector3(.12f,.016f,.12f);
            if(LanternHalo)
            {
                var halo=Shape(lantern,"Lantern light",Halo(),LanternHalo).renderer.transform;halo.localPosition=new Vector3(0,-.085f,0);
            }
            lanternWire=Shape(root,"Lantern hook",cylinder,Brass).renderer.transform;
            lantern.gameObject.SetActive(false);lanternWire.gameObject.SetActive(false);
            if(App){delivered=App.Rules.State.delivered;var active=App.Rules.Active;if(active!=null)Collect(active,false);}
        }
        (MeshFilter filter,Renderer renderer) Shape(Transform parent,string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.layer=9;
            var f=go.AddComponent<MeshFilter>();f.sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
            r.shadowCastingMode=material==Outline||material==LanternHalo?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            return (f,r);
        }
        void Collect(Parcel p,bool animate)
        {
            activeId=p.id;kind=p.kind;weight=p.weight;delivering=popping=false;
            float w=Mathf.Min((float)p.weight,2.5f);
            var size=kind==DeliveryKind.Heavy?new Vector3(.50f,.36f,.42f):kind==DeliveryKind.Airship?new Vector3(.40f,.46f,.40f):new Vector3(.27f+w*.07f,.18f+w*.05f,.23f+w*.06f);
            bool sack=kind==DeliveryKind.Airship;
            bodyFilter.sharedMesh=sack?sackMesh:boxMesh;outlineFilter.sharedMesh=sack?sackMesh:boxInk;
            body.sharedMaterial=kind==DeliveryKind.Fragile?FragilePaper:kind==DeliveryKind.Heavy?Crate:sack?Canvas:Paper;
            var ribbon=kind==DeliveryKind.Fragile?FragileRibbon:kind==DeliveryKind.Heavy?Strap:sack?Rope:String;
            band1.sharedMaterial=band2.sharedMaterial=knot.sharedMaterial=ribbon;
            parcelBody.localPosition=Vector3.down*size.y*.5f;
            body.transform.localScale=size;outline.transform.localScale=size;
            // A sack is gathered at the neck by a rope tie; boxes are tied with a crossed string.
            band1.GetComponent<MeshFilter>().sharedMesh=sack?sackMesh:boxMesh;
            band1.transform.localScale=sack?new Vector3(size.x*.36f,.07f,size.z*.36f):new Vector3(size.x+.012f,size.y+.012f,.035f);
            band1.transform.localPosition=sack?new Vector3(0,size.y*.40f,0):Vector3.zero;
            band2.transform.localScale=sack?Vector3.zero:new Vector3(.035f,size.y+.012f,size.z+.012f);
            knot.transform.localPosition=Vector3.up*(sack?size.y*.5f:size.y*.5f+.012f);knot.transform.localScale=new Vector3(.09f,.045f,.07f);
            parcel.gameObject.SetActive(true);string1.gameObject.SetActive(true);
            parcelScale=animate?0:1;popClock=animate?0:10;
            if(rider&&rider.BroomNode)lastTie=Tie();swing=swingVelocity=Vector3.zero;
        }
        Vector3 Along(float t)=>Vector3.LerpUnclamped(rider.BroomTip.position,rider.BroomNode.position,t);
        // Broom model: bristle tip at z = -1.86, broom origin at z = -0.15, handle end at z = +1.33.
        Vector3 Tie()=>Along(.53f);
        Vector3 Hook()=>Along(1.80f);
        void LateUpdate()
        {
            if(!App||!rider||!rider.BroomNode||!rider.BroomTip)return;
            float dt=Mathf.Min(Time.deltaTime,.075f);if(dt<=0)return;
            var rules=App.Rules;var active=rules.Active;
            if(active!=null&&active.id!=activeId)Collect(active,true);
            if(active==null&&activeId!="")
            {
                // Handed over at the court, or the window closed and Osono took it back.
                bool handed=rules.State.delivered>delivered;activeId="";delivering=true;popClock=0;
                if(handed&&Life)Life.Burst(parcel.position,8,1.4f,.05f,.42f,new Color(1,.93f,.66f,.9f));
                if(!handed){parcel.gameObject.SetActive(false);string1.gameObject.SetActive(false);}
            }
            delivered=rules.State.delivered;
            popClock+=dt;
            if(parcel.gameObject.activeSelf)
            {
                if(delivering){parcelScale=Mathf.Clamp01(1-popClock/.22f);if(parcelScale<=0){parcel.gameObject.SetActive(false);string1.gameObject.SetActive(false);delivering=false;}}
                else if(popClock<.4f){float u=popClock/.4f;parcelScale=1+Mathf.Sin(u*Mathf.PI)*.18f*(1-u)-Mathf.Pow(1-u,3);}
                else parcelScale=1;
                var tie=Tie();
                Hang(tie,ref lastTie,ref tieVelocity,ref swing,ref swingVelocity,dt,.05f,4.2f);
                Vector3 hang=(Vector3.down+swing).normalized;float length=.16f;
                parcel.position=tie+hang*length;
                var facing=Vector3.ProjectOnPlane(App.Motor.Visual.forward,hang);
                parcel.rotation=Quaternion.LookRotation(facing.sqrMagnitude>.001f?facing:Vector3.forward,-hang)*Quaternion.Euler(0,Mathf.Sin(Time.time*1.3f)*4*(1-rider.FlightPose),0);
                parcel.localScale=Vector3.one*Mathf.Max(.001f,parcelScale);
                string1.position=tie+hang*length*.5f;string1.rotation=Quaternion.FromToRotation(Vector3.up,-hang);string1.localScale=new Vector3(.012f,length*.5f,.012f);
                string1.gameObject.SetActive(parcelScale>.05f);
            }
            bool fitted=rules.Level(Upgrade.Lantern)>0&&App.InGame;
            if(lantern.gameObject.activeSelf!=fitted){lantern.gameObject.SetActive(fitted);lanternWire.gameObject.SetActive(fitted);lastHook=Hook();lanternSwing=lanternVelocity=Vector3.zero;}
            if(fitted)
            {
                var hook=Hook();
                Hang(hook,ref lastHook,ref hookVelocity,ref lanternSwing,ref lanternVelocity,dt,.035f,5.5f);
                Vector3 hang=(Vector3.down+lanternSwing).normalized;
                lantern.position=hook+hang*.10f;lantern.rotation=Quaternion.FromToRotation(Vector3.up,-hang);
                lanternWire.position=hook+hang*.05f;lanternWire.rotation=lantern.rotation;lanternWire.localScale=new Vector3(.008f,.05f,.008f);
            }
        }
        static void Hang(Vector3 point,ref Vector3 last,ref Vector3 velocity,ref Vector3 swing,ref Vector3 swingVelocity,float dt,float response,float frequency)
        {
            // A damped pendulum: the load trails behind the broom's acceleration and settles.
            var v=(point-last)/dt;last=point;var accel=(v-velocity)/dt;velocity=v;
            var target=Vector3.ClampMagnitude(-accel*response,.9f);target.y=0;
            float w=frequency*2*Mathf.PI;int steps=Mathf.Max(1,Mathf.CeilToInt(dt*120));float h=dt/steps;
            for(int i=0;i<steps;i++){swingVelocity+=(w*w*(target-swing)-2*.35f*w*swingVelocity)*h;swing+=swingVelocity*h;}
            swing=Vector3.ClampMagnitude(swing,.9f);
        }
        static Mesh Box(bool smooth)
        {
            var mesh=new Mesh{name=smooth?"Parcel ink shell":"Parcel box"};
            if(smooth)
            {
                var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f);
                var normals=new Vector3[8];var colors=new Color[8];for(int i=0;i<8;i++){normals[i]=v[i].normalized;colors[i]=Color.white;}
                mesh.vertices=v;mesh.normals=normals;mesh.colors=colors;
                mesh.triangles=new[]{0,2,3,0,3,1, 4,5,7,4,7,6, 0,1,5,0,5,4, 2,6,7,2,7,3, 0,4,6,0,6,2, 1,3,7,1,7,5};
            }
            else
            {
                var verts=new System.Collections.Generic.List<Vector3>();var norms=new System.Collections.Generic.List<Vector3>();var tris=new System.Collections.Generic.List<int>();
                foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
                {
                    Vector3 a=n.x!=0?Vector3.up:Vector3.right,b=Vector3.Cross(n,a);int s=verts.Count;
                    foreach(var c in new[]{-a-b,a-b,a+b,-a+b}){verts.Add((n+c)*.5f);norms.Add(n);}
                    tris.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});
                }
                mesh.SetVertices(verts);mesh.SetNormals(norms);var colors=new Color[verts.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;mesh.SetTriangles(tris,0);
            }
            mesh.RecalculateBounds();return mesh;
        }
        static Mesh Sack()
        {
            // A soft gathered sack: a squashed sphere with a pinched top.
            var verts=new System.Collections.Generic.List<Vector3>();var tris=new System.Collections.Generic.List<int>();
            int rings=8,segments=12;
            for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++)
            {
                float v=(float)r/rings,a=(float)s/segments*Mathf.PI*2,phi=v*Mathf.PI;
                float radius=Mathf.Sin(phi)*(v<.3f?Mathf.Lerp(.55f,1,v/.3f):1)*.5f;
                verts.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Cos(phi)*.5f,Mathf.Sin(a)*radius));
            }
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int i=r*(segments+1)+s;tris.AddRange(new[]{i,i+1,i+segments+1,i+1,i+segments+2,i+segments+1});}
            var mesh=new Mesh{name="Canvas sack"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
            var colors=new Color[verts.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;mesh.RecalculateBounds();return mesh;
        }
        static Mesh Cylinder()
        {
            var verts=new System.Collections.Generic.List<Vector3>();var norms=new System.Collections.Generic.List<Vector3>();var tris=new System.Collections.Generic.List<int>();
            int segments=12;
            for(int s=0;s<=segments;s++){float a=(float)s/segments*Mathf.PI*2;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));verts.Add(n*.5f+Vector3.down);verts.Add(n*.5f+Vector3.up);norms.Add(n);norms.Add(n);}
            for(int s=0;s<segments;s++){int i=s*2;tris.AddRange(new[]{i,i+1,i+3,i,i+3,i+2});}
            foreach(int side in new[]{-1,1})
            {
                int center=verts.Count;verts.Add(Vector3.up*side);norms.Add(Vector3.up*side);
                for(int s=0;s<=segments;s++){float a=(float)s/segments*Mathf.PI*2;verts.Add(new Vector3(Mathf.Cos(a)*.5f,side,Mathf.Sin(a)*.5f));norms.Add(Vector3.up*side);}
                for(int s=0;s<segments;s++){if(side>0)tris.AddRange(new[]{center,center+s+2,center+s+1});else tris.AddRange(new[]{center,center+s+1,center+s+2});}
            }
            var mesh=new Mesh{name="Prop cylinder"};mesh.SetVertices(verts);mesh.SetNormals(norms);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
        }
        static Mesh Halo()
        {
            var mesh=new Mesh{name="Lantern halo"};
            mesh.vertices=new Vector3[4];mesh.uv=new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)};
            mesh.SetUVs(1,new[]{new Vector2(.42f,1),new Vector2(.42f,1),new Vector2(.42f,1),new Vector2(.42f,1)});
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.bounds=new Bounds(Vector3.zero,Vector3.one*1.5f);return mesh;
        }
    }
}
