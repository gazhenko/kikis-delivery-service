using System;
using Koriko.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Koriko
{
    public sealed class TownMap : MaskableGraphic
    {
        [Serializable] public class Building { public string name;public float x,y,z,width,depth,height; }
        [Serializable] public class Layout { public Building[] buildings; }
        public GameApp App;
        Layout layout;
        float next;
        protected override void Awake()
        {
            base.Awake();raycastTarget=false;
            var data=Resources.Load<TextAsset>("KorikoLayout");if(data)layout=JsonUtility.FromJson<Layout>(data.text);
        }
        void Update(){if(Time.unscaledTime>=next){next=Time.unscaledTime+.08f;SetVerticesDirty();}}
        Vector2 P(float x,float z)
        {
            var r=rectTransform.rect;return new Vector2(r.xMin+(x+175)/355*r.width,r.yMin+(z+145)/330*r.height);
        }
        void Quad(VertexHelper v,Vector2 a,Vector2 b,Color color)
        {
            int i=v.currentVertCount;v.AddVert(new Vector3(a.x,a.y),color,Vector2.zero);v.AddVert(new Vector3(b.x,a.y),color,Vector2.zero);v.AddVert(new Vector3(b.x,b.y),color,Vector2.zero);v.AddVert(new Vector3(a.x,b.y),color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);
        }
        void Dot(VertexHelper v,Vector2 p,float radius,Color color)
        {
            int n=v.currentVertCount;v.AddVert(p,color,Vector2.zero);for(int i=0;i<=12;i++){float a=i*Mathf.PI*2/12;v.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);if(i>0)v.AddTriangle(n,n+i,n+i+1);}
        }
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=rectTransform.rect;Quad(v,r.min,r.max,new Color(.60f,.68f,.49f));Quad(v,P(-175,-145),P(180,-76),new Color(.38f,.60f,.63f));
            var road=new Color(.89f,.83f,.68f);
            Quad(v,P(-155,-4),P(151,4),road);Quad(v,P(-130,-66),P(148,-58),road);Quad(v,P(-46,-62),P(-38,129),road);Quad(v,P(60,-62),P(68,129),road);Quad(v,P(-42,75),P(140,81),road);Quad(v,P(-125,126.5f),P(142,131.5f),road);Quad(v,P(-10,3),P(42,47),road);
            if(layout?.buildings!=null)foreach(var b in layout.buildings)Quad(v,P(b.x-b.width/2,b.z-b.depth/2),P(b.x+b.width/2,b.z+b.depth/2),new Color(.65f,.40f,.29f));
            foreach(var d in Catalog.Destinations)
            {
                bool active=App?.Rules?.Active?.destination==d.Id;
                Dot(v,P((float)d.Landing.x,(float)d.Landing.z),active?5:3,d.Id=="bakery"?new Color(.95f,.85f,.60f):active?new Color(1,.75f,.31f):new Color(.93f,.88f,.76f));
            }
            if(App?.Motor)
            {
                Vector3 world=App.Motor.transform.position;Vector2 center=P(world.x,world.z);
                float angle=(App.Motor.Visual?App.Motor.Visual.eulerAngles.y:0)*Mathf.Deg2Rad;
                Vector2 f=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle)),s=new Vector2(f.y,-f.x);
                int i=v.currentVertCount;v.AddVert(center+f*6,new Color(.45f,.14f,.17f),Vector2.zero);v.AddVert(center-f*4+s*4,new Color(.45f,.14f,.17f),Vector2.zero);v.AddVert(center-f*4-s*4,new Color(.45f,.14f,.17f),Vector2.zero);v.AddTriangle(i,i+1,i+2);
            }
        }
    }
}
