using Koriko.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Koriko
{
    public sealed class CrowFlock : MonoBehaviour
    {
        public GameApp App;
        public Material Material;
        readonly Vector3[] nests={new Vector3(42,15,47),new Vector3(70,18,107),new Vector3(84,12,-28),new Vector3(-68,15,73)};
        Transform[] birds,wingsLeft,wingsRight;
        void Start()
        {
            birds=new Transform[nests.Length];wingsLeft=new Transform[nests.Length];wingsRight=new Transform[nests.Length];
            for(int i=0;i<nests.Length;i++)
            {
                var root=new GameObject("Crow "+i).transform;root.SetParent(transform);root.position=nests[i];birds[i]=root;
                Shape(root,PrimitiveType.Sphere,new Vector3(0,0,0),new Vector3(.36f,.4f,.68f));
                Shape(root,PrimitiveType.Sphere,new Vector3(0,.18f,.28f),new Vector3(.3f,.3f,.3f));
                wingsLeft[i]=Wing(root,-1);wingsRight[i]=Wing(root,1);
            }
        }
        Transform Shape(Transform parent,PrimitiveType kind,Vector3 pos,Vector3 scale)
        {
            var obj=GameObject.CreatePrimitive(kind);Destroy(obj.GetComponent<Collider>());obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=Material;return obj.transform;
        }
        Transform Wing(Transform parent,int side)
        {
            var pivot=new GameObject(side<0?"Left wing":"Right wing").transform;pivot.SetParent(parent,false);pivot.localPosition=new Vector3(side*.13f,.05f,0);
            var points=new[]{new Vector3(0,0,.1f),new Vector3(.3f,.025f,.17f),new Vector3(.65f,.02f,.12f),new Vector3(.84f,0,-.13f),new Vector3(.6f,0,-.1f),new Vector3(.74f,0,-.26f),new Vector3(.47f,0,-.19f),new Vector3(.5f,0,-.4f),new Vector3(.25f,0,-.29f),new Vector3(0,0,-.18f)};
            var normals=new Vector3[points.Length];for(int i=0;i<points.Length;i++){points[i].x*=side;normals[i]=Vector3.up;}
            var indices=new List<int>();for(int i=1;i<points.Length-1;i++){indices.AddRange(new[]{0,i,i+1,0,i+1,i});}
            var mesh=new Mesh{name="Drawn feather silhouette",vertices=points,triangles=indices.ToArray(),normals=normals};mesh.RecalculateBounds();
            pivot.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;pivot.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Material;
            return pivot;
        }
        void Update()
        {
            if(!App||!App.InGame||birds==null)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);var player=App.Motor.transform.position;var rules=App.Rules;
            bool safe=Vector3.Distance(player,new Vector3(-113,0,9))<22;
            for(int i=0;i<birds.Length;i++)
            {
                float range=(rules.State.difficulty==Difficulty.Cozy?18:28)-rules.Level(Upgrade.Bell)*5;
                bool chase=!safe&&Vector3.Distance(birds[i].position,player+Vector3.up)<range;
                float angle=(float)rules.State.elapsed*.4f+i*2;
                var target=chase?player+Vector3.up*1.2f:nests[i]+new Vector3(Mathf.Cos(angle)*8,Mathf.Sin(angle*1.2f),Mathf.Sin(angle)*8);
                var direction=target-birds[i].position;
                birds[i].position=Vector3.MoveTowards(birds[i].position,target,dt*(chase?(rules.State.difficulty==Difficulty.Cozy?10:13.5f):4));
                if(direction.sqrMagnitude>.01f)birds[i].rotation=Quaternion.Slerp(birds[i].rotation,Quaternion.LookRotation(direction),dt*6);
                float flap=Mathf.Sin(Mathf.Floor(Time.time*12)/12*13+i)*38;
                wingsLeft[i].localRotation=Quaternion.Euler(0,0,flap);wingsRight[i].localRotation=Quaternion.Euler(0,0,-flap);
                if(chase&&Vector3.Distance(birds[i].position,player+Vector3.up)<1.6f)rules.CrowHit(App.Motor.Boosting);
            }
        }
    }
}
