using System;
using Koriko.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Koriko
{
    public sealed class GameHud : MonoBehaviour
    {
        public Font Font;
        public Font Bold;
        public Texture2D Icons;
        public Texture2D Paper;
        public GameApp App { get; private set; }
        public bool PanelOpen => panel != null;
        readonly Color ink=new Color(.19f,.24f,.24f),paper=new Color(.97f,.93f,.83f,.96f),accent=new Color(.68f,.31f,.23f);
        Sprite[] icons;
        Sprite paperSprite;
        RectTransform root,panel,rows;
        Text clock,money,energyText,foodText,jobText,prompt,notice,controls,bakeryStatus;
        RectTransform promptCard,noticeCard;
        Image energy,food,fade;
        GameObject flightHud;
        ScrollRect activeScroll;
        GameObject lastSelection;
        int tab,noticeId=-1,day;
        bool night;
        string recipe="";
        float noticeUntil;

        public void Initialize(GameApp app)
        {
            App=app;root=(RectTransform)transform;
            if(!Font)Font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!Bold)Bold=Font;
            icons=new Sprite[16];
            if(Paper)paperSprite=Sprite.Create(Paper,new Rect(2,2,Paper.width/4f-4,Paper.height/4f-4),new Vector2(.5f,.5f));
            if(Icons)for(int i=0;i<16;i++)icons[i]=Sprite.Create(Icons,new Rect((i%4)*Icons.width/4f,(3-i/4)*Icons.height/4f,Icons.width/4f,Icons.height/4f),new Vector2(.5f,.5f));
            var hud=Rect("Flight HUD",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);hud.offsetMin=Vector2.zero;hud.offsetMax=Vector2.zero;flightHud=hud.gameObject;
            var time=Panel("Clock",hud,new Vector2(0,1),new Vector2(18,-18),new Vector2(242,62));
            Icon(time,2,new Vector2(13,-10),38);clock=Label(time,"",new Vector2(60,-10),new Vector2(165,40),23,true);
            var purse=Panel("Purse",hud,new Vector2(0,1),new Vector2(18,-89),new Vector2(146,46));
            Icon(purse,0,new Vector2(10,-6),34);money=Label(purse,"",new Vector2(51,-9),new Vector2(92,29),22,true);
            var vitality=Panel("Kiki’s energy",hud,new Vector2(0,0),new Vector2(18,18),new Vector2(290,99));
            Icon(vitality,1,new Vector2(12,-8),35);Icon(vitality,10,new Vector2(12,-53),35);
            energy=Bar(vitality,new Vector2(58,-18),new Color(.38f,.56f,.43f));food=Bar(vitality,new Vector2(58,-64),new Color(.77f,.53f,.30f));
            energyText=Label(vitality,"",new Vector2(218,-12),new Vector2(62,27),18);
            foodText=Label(vitality,"",new Vector2(218,-58),new Vector2(62,27),18);
            var parcel=Panel("Current parcel",hud,new Vector2(.5f,1),new Vector2(0,-18),new Vector2(420,70));
            Icon(parcel,0,new Vector2(10,-10),45);jobText=Label(parcel,"",new Vector2(67,-10),new Vector2(340,54),19);
            var map=Panel("Minimap",hud,new Vector2(1,1),new Vector2(-18,-18),new Vector2(226,244));
            Label(map,"KORIKO   ·   N ↑",new Vector2(13,-9),new Vector2(202,23),16,true);
            var mapRect=Rect("Town plan",map,new Vector2(0,1),new Vector2(0,1),new Vector2(13,-36),new Vector2(200,194));
            mapRect.gameObject.AddComponent<TownMap>().App=App;
            promptCard=Panel("Landing prompt",hud,new Vector2(.5f,0),new Vector2(0,142),new Vector2(500,42));
            prompt=Label(promptCard,"",new Vector2(12,-4),new Vector2(476,34),21,true);prompt.alignment=TextAnchor.MiddleCenter;
            noticeCard=Panel("Delivery notice",hud,new Vector2(.5f,0),new Vector2(0,78),new Vector2(500,54));
            notice=Label(noticeCard,"",new Vector2(12,-5),new Vector2(476,44),19);notice.alignment=TextAnchor.MiddleCenter;
            var controlCard=Panel("Flight controls",hud,Vector2.zero,new Vector2(328,18),new Vector2(934,46));
            controlCard.anchorMax=new Vector2(1,0);controlCard.offsetMin=new Vector2(328,18);controlCard.offsetMax=new Vector2(-18,64);
            controls=Label(controlCard,"",new Vector2(12,-5),new Vector2(910,36),15);controls.alignment=TextAnchor.MiddleCenter;
            controls.rectTransform.anchorMax=new Vector2(1,1);controls.rectTransform.offsetMin=new Vector2(12,-41);controls.rectTransform.offsetMax=new Vector2(-12,-5);
            flightHud.SetActive(false);
            ShowTitle();
            var shade=Rect("Sleep transition",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);shade.offsetMin=Vector2.zero;shade.offsetMax=Vector2.zero;
            fade=shade.gameObject.AddComponent<Image>();fade.color=new Color(.10f,.14f,.20f,0);fade.raycastTarget=false;
        }

        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.pivot=min==max?new Vector2(min.x,min.y):new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var r=Rect(name,parent,anchor,anchor,pos,size);var image=r.gameObject.AddComponent<Image>();image.sprite=paperSprite;image.color=paperSprite?new Color(1,1,1,.97f):paper;
            var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.31f,.32f,.27f,.3f);outline.effectDistance=new Vector2(1,-1);return r;
        }
        Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize=20,bool bold=false,Vector2? anchor=null)
        {
            Vector2 a=anchor??new Vector2(0,1);var r=Rect("Text",parent,a,a,pos,size);
            var t=r.gameObject.AddComponent<Text>();t.font=bold?Bold:Font;t.fontSize=fontSize;t.color=ink;t.text=value;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        Image Icon(Transform parent,int index,Vector2 pos,float size)
        {
            var r=Rect("Illustration",parent,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(size,size));var i=r.gameObject.AddComponent<Image>();i.sprite=icons[Mathf.Clamp(index,0,15)];i.preserveAspect=true;i.raycastTarget=false;return i;
        }
        Image Bar(Transform parent,Vector2 pos,Color color)
        {
            var back=Rect("Meter",parent,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(150,15));back.gameObject.AddComponent<Image>().color=new Color(.80f,.78f,.68f);
            var fill=Rect("Fill",back,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);fill.offsetMin=Vector2.zero;fill.offsetMax=Vector2.zero;fill.pivot=new Vector2(0,.5f);
            var image=fill.gameObject.AddComponent<Image>();image.color=color;return image;
        }
        Button Button(Transform parent,string title,Vector2 pos,Vector2 size,Action action,bool enabled=true)
        {
            var r=Rect(title,parent,new Vector2(0,1),new Vector2(0,1),pos,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.87f,.84f,.73f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.92f,.73f);colors.selectedColor=new Color(1,.87f,.60f);colors.pressedColor=new Color(.76f,.68f,.50f);colors.disabledColor=new Color(.9f,.9f,.9f,.5f);b.colors=colors;
            var t=Label(r,title,new Vector2(7,-3),size-new Vector2(14,6),18,true);t.alignment=TextAnchor.MiddleCenter;
            b.onClick.AddListener(()=>action());return b;
        }
        void Focus(Button button){if(button&&EventSystem.current)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        public void ClosePanel(){if(panel)Destroy(panel.gameObject);panel=null;rows=null;bakeryStatus=null;activeScroll=null;lastSelection=null;if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}

        void ShowTitle()
        {
            ClosePanel();panel=Panel("Choose your pace",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(530,280));
            Icon(panel,1,new Vector2(31,-23),64);Label(panel,"Kiki’s Delivery Service",new Vector2(106,-31),new Vector2(410,43),32,true);
            Label(panel,"A bakery, a broom, and a town to get to know.",new Vector2(33,-98),new Vector2(470,35),21);
            var first=Button(panel,"Cozy",new Vector2(33,-151),new Vector2(221,46),()=>App.Begin(Difficulty.Cozy));
            Button(panel,"Challenging",new Vector2(269,-151),new Vector2(228,46),()=>App.Begin(Difficulty.Challenging));
            if(App.HasSave)first=Button(panel,"Continue your deliveries",new Vector2(33,-212),new Vector2(464,43),()=>App.Continue());
            else Label(panel,"Keyboard + mouse or controller",new Vector2(33,-218),new Vector2(464,30),17);
            Focus(first);
        }

        public void ShowBakery(){tab=0;BuildBakery();}
        public void RefreshPanel(){if(panel&&rows)BuildBakery();}
        void BuildBakery()
        {
            ClosePanel();panel=Panel("Bakery board",root,new Vector2(0,1),new Vector2(18,-151),new Vector2(604,530));
            // Reserve the vitality meter below the board at every window aspect ratio.
            panel.anchorMin=Vector2.zero;panel.anchorMax=new Vector2(0,1);
            panel.offsetMin=new Vector2(18,129);panel.offsetMax=new Vector2(622,-151);
            Label(panel,"Osono’s bakery",new Vector2(20,-13),new Vector2(365,37),27,true);
            Button(panel,"Fly",new Vector2(494,-14),new Vector2(88,34),ClosePanel);
            string[] tabs={"Deliveries","Pantry","Kitchen","Broom"};
            for(int i=0;i<4;i++){int index=i;var button=Button(panel,tabs[i],new Vector2(18+i*145,-62),new Vector2(136,38),()=>{tab=index;BuildBakery();});if(i==tab)button.image.color=new Color(.84f,.72f,.52f);}
            var viewport=Rect("Scroll",panel,new Vector2(0,1),new Vector2(0,1),new Vector2(15,-112),new Vector2(574,349));
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
            viewport.offsetMin=new Vector2(15,69);viewport.offsetMax=new Vector2(-15,-112);
            viewport.gameObject.AddComponent<Image>().color=new Color(1,1,1,.015f);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();activeScroll=scroll;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            rows=Rect("Rows",viewport,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);rows.pivot=new Vector2(.5f,1);scroll.content=rows;scroll.viewport=viewport;
            var layout=rows.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=9;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
            rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Button first=null;var rules=App.Rules;
            if(tab==0)
            {
                foreach(var parcel in rules.State.jobs)
                {
                    var p=parcel;string reason=rules.JobReason(p);
                    string detail=$"{Catalog.FindDestination(p.destination).Name} · {p.weight:0.#} kg · {p.reward} coins\n"+(reason??$"{Mathf.RoundToInt((float)p.duration)} seconds after pickup");
                    var b=Row(0,p.title,detail,p.accepted?"On broom":"Collect",()=>{if(rules.Accept(p.id)){ClosePanel();App.Save();}else RefreshPanel();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            else if(tab==1)
            {
                for(int i=0;i<6;i++)
                {
                    var ingredient=(Ingredient)i;string reason=rules.IngredientReason(ingredient);
                    var b=Row(Catalog.IngredientIcons[i],Catalog.IngredientNames[i],$"In pantry: {rules.State.pantry[i]}\n"+(reason??"For something warm when you get home."),Catalog.IngredientCosts[i]+" coins",()=>{rules.Buy(ingredient);BuildBakery();App.Save();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            else if(tab==2)
            {
                foreach(var recipe in Catalog.Recipes)
                {
                    var r=recipe;string reason=rules.RecipeReason(r);
                    var b=Row(r.Icon,r.Name,r.Description+"\n"+(reason??Ingredients(r)),"Cook",()=>{rules.Cook(r.Id);BuildBakery();App.Save();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            else
            {
                foreach(var upgrade in Catalog.Upgrades)
                {
                    var u=upgrade;string reason=rules.UpgradeReason(u.Id);
                    var b=Row(u.Id==Upgrade.Lantern?2:1,u.Name+$"  {rules.Level(u.Id)}/{u.Max}",u.Description+"\n"+(reason??"Ready to fit to your broom."),rules.Cost(u.Id)+" coins",()=>{rules.Purchase(u.Id);BuildBakery();App.Save();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            var restIcon=Icon(panel,3,new Vector2(18,-481),32).rectTransform;
            restIcon.anchorMin=restIcon.anchorMax=Vector2.zero;restIcon.anchoredPosition=new Vector2(18,49);
            var rest=Button(panel,"Sleep · 8 hours",new Vector2(62,-479),new Vector2(179,35),App.Sleep,!rules.Cooking);
            var restRect=(RectTransform)rest.transform;restRect.anchorMin=restRect.anchorMax=Vector2.zero;restRect.anchoredPosition=new Vector2(62,51);
            bakeryStatus=Label(panel,"",new Vector2(255,-484),new Vector2(330,31),16);
            bakeryStatus.rectTransform.anchorMin=bakeryStatus.rectTransform.anchorMax=Vector2.zero;bakeryStatus.rectTransform.anchoredPosition=new Vector2(255,46);
            Focus(first??rest);
        }
        string Ingredients(Recipe r)
        {
            string value="";for(int i=0;i<6;i++)if(r.Ingredients[i]>0)value+=(value.Length>0?" · ":"")+r.Ingredients[i]+" "+Catalog.IngredientNames[i].ToLowerInvariant();return value;
        }
        Button Row(int icon,string title,string detail,string action,Action onClick,bool enabled)
        {
            var r=Rect(title,rows,new Vector2(0,1),new Vector2(1,1),Vector2.zero,new Vector2(0,90));r.gameObject.AddComponent<LayoutElement>().preferredHeight=90;
            r.gameObject.AddComponent<Image>().color=new Color(1,.98f,.90f,.65f);
            Icon(r,icon,new Vector2(9,-13),56);Label(r,title,new Vector2(76,-9),new Vector2(366,25),21,true);Label(r,detail,new Vector2(76,-36),new Vector2(353,46),15);
            return Button(r,action,new Vector2(447,-28),new Vector2(114,37),onClick,enabled);
        }
        public void ToggleOptions()
        {
            if(PanelOpen){ClosePanel();return;}
            panel=Panel("Flight settings",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(550,466));
            Label(panel,"Flight settings",new Vector2(25,-22),new Vector2(500,40),30,true);
            Label(panel,"WASD / left stick: fly\nSpace / A: rise       Ctrl / B: descend\nShift / right trigger: boost\nE / X: land, deliver, visit the bakery\nRight mouse drag / right stick: look around\nTab / Y: bakery or settings",new Vector2(25,-78),new Vector2(500,166),21);
            Button(panel,"Cozy",new Vector2(25,-265),new Vector2(238,44),()=>{App.Rules.SetDifficulty(Difficulty.Cozy);ClosePanel();});
            Button(panel,"Challenging",new Vector2(279,-265),new Vector2(246,44),()=>{App.Rules.SetDifficulty(Difficulty.Challenging);ClosePanel();});
            Focus(Button(panel,"Back to flying",new Vector2(25,-333),new Vector2(500,44),ClosePanel));
            Button(panel,"Save & quit",new Vector2(25,-395),new Vector2(500,44),()=>{App.Save();Application.Quit();});
        }
        void Update()
        {
            if(App==null)return;
            flightHud.SetActive(App.InGame);
            if(fade){fade.color=new Color(.10f,.14f,.20f,App.SleepFade);fade.transform.SetAsLastSibling();}
            if(!App.InGame)return;
            var r=App.Rules;
            int hour=(int)r.Hour;int minute=(int)((r.Hour-hour)*60);
            clock.text=$"Day {r.Day} · {hour:00}:{minute:00}";money.text=r.State.money+" coins";
            energy.rectTransform.localScale=new Vector3((float)r.State.energy/100,1,1);food.rectTransform.localScale=new Vector3((float)r.State.fullness/100,1,1);
            energyText.text=$"{r.State.energy:0}%";foodText.text=$"{r.State.fullness:0}%";
            var p=r.Active;
            jobText.text=p==null?"Your broom is light.\nPick up a delivery at the bakery.":Catalog.FindDestination(p.destination).Name+$"\n{Mathf.CeilToInt((float)(p.deadline-r.State.elapsed))}s · {r.State.position.HorizontalDistance(Catalog.FindDestination(p.destination).Landing):0} m";
            prompt.text=PanelOpen?"":r.CanDeliver?(App.Input.Controller?"X  ·  Deliver parcel":"E  ·  Deliver parcel"):r.AtHome?(App.Input.Controller?"X  ·  Visit Osono":"E  ·  Visit Osono"):App.Motor.Approaching?"Settling into the courtyard…":"";
            promptCard.gameObject.SetActive(prompt.text.Length>0);
            controls.text=App.Input.Controller?"Left stick fly  ·  A / B rise & descend  ·  RT boost  ·  X land / deliver  ·  Y menu":"WASD fly  ·  Space / Ctrl rise & descend  ·  Shift boost  ·  E land / deliver  ·  Right mouse look  ·  Tab menu";
            if(noticeId!=r.NoticeRevision){noticeId=r.NoticeRevision;noticeUntil=Time.unscaledTime+6;notice.text=r.Notice;}
            noticeCard.gameObject.SetActive(!PanelOpen&&Time.unscaledTime<noticeUntil&&!string.IsNullOrEmpty(notice.text));
            if(bakeryStatus)bakeryStatus.text=r.Cooking?$"Cooking… {Mathf.CeilToInt((float)r.State.cookingRemaining)}s":"The clock keeps moving while you plan.";
            if(day!=r.Day||night!=r.IsNight||recipe!=r.State.recipeId){day=r.Day;night=r.IsNight;recipe=r.State.recipeId;RefreshPanel();}
        }
        void LateUpdate()
        {
            if(!activeScroll||!EventSystem.current)return;
            var selected=EventSystem.current.currentSelectedGameObject;
            if(selected==lastSelection)return;
            lastSelection=selected;
            if(!selected||!selected.transform.IsChildOf(rows))return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(activeScroll.viewport,(RectTransform)selected.transform);
            var viewport=activeScroll.viewport.rect;
            float correction=bounds.max.y>viewport.yMax?viewport.yMax-bounds.max.y:bounds.min.y<viewport.yMin?viewport.yMin-bounds.min.y:0;
            var position=rows.anchoredPosition;position.y+=correction;
            position.y=Mathf.Clamp(position.y,0,Mathf.Max(0,rows.rect.height-viewport.height));
            rows.anchoredPosition=position;activeScroll.StopMovement();
        }
    }
}
