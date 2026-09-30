using System;
using System.Collections.Generic;
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
        public Texture2D Portraits;
        public GameApp App { get; private set; }
        public bool PanelOpen => panel != null;
        public bool BakeryOpen => panel != null && rows != null && page == Page.Bakery;
        public bool MarkerVisible => marker && marker.gameObject.activeSelf;
        public bool MarkerOffscreen => markerArrow && markerArrow.gameObject.activeSelf;
        public float HintAlpha => hintGroup ? hintGroup.alpha : 0;
        public string NoticeText => notice ? notice.text : "";
        public int HintMode { get; private set; }
        enum Page { None, Title, NewGame, Bakery, Settings, Controls, Sound }
        readonly Color ink=new Color(.19f,.24f,.24f),paper=new Color(.97f,.93f,.83f,.96f),accent=new Color(.68f,.31f,.23f),urgent=new Color(.72f,.20f,.16f);
        Sprite[] icons;
        Sprite paperSprite,coin,sun,moon,stamp,arrow,kiki,jiji;
        RectTransform root,panel,rows;
        Page page;
        Text clock,money,energyText,foodText,jobTitle,jobDetail,prompt,notice,hints,bakeryStatus,markerLabel,stampTitle,stampDetail,caption;
        RectTransform promptCard,noticeCard,parcelCard,hintCard,marker,markerArrow,stampCard,stampMark;
        CanvasGroup noticeGroup,hintGroup,stampGroup,captionGroup;
        Image energy,food,fade,dayIcon,markerIcon,noticeFace;
        GameObject flightHud;
        ScrollRect activeScroll;
        GameObject lastSelection;
        int tab,noticeId=-1,day,lastHintHash;
        bool night,noticeTip,lowEnergy,lowFood;
        string recipe="",lastHint="";
        float noticeUntil,hintShown=-100,stampClock=100,idleClock,sessionClock;
        readonly HashSet<string> tipsSeen=new HashSet<string>();
        const string Preferences="koriko.";

        public void Initialize(GameApp app)
        {
            App=app;root=(RectTransform)transform;
            if(!Font)Font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!Bold)Bold=Font;
            icons=new Sprite[16];
            if(Paper)paperSprite=Sprite.Create(Paper,new Rect(2,2,Paper.width/4f-4,Paper.height/4f-4),new Vector2(.5f,.5f));
            if(Icons)for(int i=0;i<16;i++)icons[i]=Sprite.Create(Icons,new Rect((i%4)*Icons.width/4f,(3-i/4)*Icons.height/4f,Icons.width/4f,Icons.height/4f),new Vector2(.5f,.5f));
            if(Portraits)
            {
                // The shared portrait sheet holds Kiki with Jiji (left) and Jiji alone (right).
                float w=Portraits.width,h=Portraits.height;
                kiki=Sprite.Create(Portraits,new Rect(w*.085f,0,w*.38f,h),new Vector2(.5f,0));
                jiji=Sprite.Create(Portraits,new Rect(w*.625f,h*.47f,w*.23f,h*.46f),new Vector2(.5f,.5f));
            }
            coin=Painted(64,DrawCoin);sun=Painted(64,DrawSun);moon=Painted(64,DrawMoon);stamp=Painted(128,DrawStamp);arrow=Painted(48,DrawArrow);
            if(!DevelopmentFlightCheck.Requested)
            {
                HintMode=Mathf.Clamp(PlayerPrefs.GetInt(Preferences+"hints",0),0,2);
                foreach(var key in PlayerPrefs.GetString(Preferences+"tips","").Split(','))if(key.Length>0)tipsSeen.Add(key);
            }
            var hud=Stretch("Flight HUD",root);flightHud=hud.gameObject;
            var status=Panel("Clock and purse",hud,new Vector2(0,1),new Vector2(18,-18),new Vector2(228,84));
            dayIcon=Icon(status,sun,new Vector2(12,-9),32);clock=Label(status,"",new Vector2(52,-7),new Vector2(172,34),23,true);
            Icon(status,coin,new Vector2(15,-50),26);money=Label(status,"",new Vector2(52,-46),new Vector2(172,31),20,true);
            var vitality=Panel("Kiki’s energy",hud,new Vector2(0,0),new Vector2(18,18),new Vector2(270,88));
            Icon(vitality,icons[1],new Vector2(12,-8),32);Icon(vitality,icons[10],new Vector2(12,-48),32);
            energy=Bar(vitality,new Vector2(54,-17),new Color(.38f,.56f,.43f));food=Bar(vitality,new Vector2(54,-57),new Color(.77f,.53f,.30f));
            energyText=Label(vitality,"",new Vector2(212,-10),new Vector2(54,27),17);
            foodText=Label(vitality,"",new Vector2(212,-50),new Vector2(54,27),17);
            parcelCard=Panel("Current parcel",hud,new Vector2(.5f,1),new Vector2(0,-18),new Vector2(420,70));
            Icon(parcelCard,icons[0],new Vector2(10,-10),48);
            jobTitle=Label(parcelCard,"",new Vector2(68,-8),new Vector2(340,28),20,true);
            jobDetail=Label(parcelCard,"",new Vector2(68,-37),new Vector2(340,26),17);
            var map=Panel("Minimap",hud,new Vector2(1,1),new Vector2(-18,-18),new Vector2(226,244));
            Label(map,"KORIKO   ·   N ↑",new Vector2(13,-9),new Vector2(202,23),16,true);
            var mapRect=Rect("Town plan",map,new Vector2(0,1),new Vector2(0,1),new Vector2(13,-36),new Vector2(200,194));
            mapRect.gameObject.AddComponent<TownMap>().App=App;
            promptCard=Panel("Landing prompt",hud,new Vector2(.5f,0),new Vector2(0,112),new Vector2(360,38));
            prompt=Label(promptCard,"",new Vector2(12,-3),new Vector2(336,32),20,true);prompt.alignment=TextAnchor.MiddleCenter;
            noticeCard=Panel("Delivery notice",hud,new Vector2(.5f,1),new Vector2(0,-100),new Vector2(480,54));
            noticeGroup=noticeCard.gameObject.AddComponent<CanvasGroup>();noticeGroup.alpha=0;noticeGroup.blocksRaycasts=false;
            noticeFace=Icon(noticeCard,jiji,new Vector2(8,-5),44);noticeFace.gameObject.SetActive(false);
            notice=Label(noticeCard,"",new Vector2(12,-4),new Vector2(456,46),18);notice.alignment=TextAnchor.MiddleCenter;
            hintCard=Panel("Control hints",hud,new Vector2(.5f,0),new Vector2(0,18),new Vector2(860,38));
            hintGroup=hintCard.gameObject.AddComponent<CanvasGroup>();hintGroup.blocksRaycasts=false;
            hints=Label(hintCard,"",new Vector2(12,-3),new Vector2(836,32),15);hints.alignment=TextAnchor.MiddleCenter;
            hints.resizeTextForBestFit=true;hints.resizeTextMinSize=11;hints.resizeTextMaxSize=15;
            marker=Rect("Destination tag",hud,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(118,36));
            var tag=marker.gameObject.AddComponent<Image>();tag.sprite=paperSprite;tag.color=paperSprite?new Color(1,1,1,.93f):paper;tag.raycastTarget=false;
            var tagEdge=marker.gameObject.AddComponent<Outline>();tagEdge.effectColor=new Color(.55f,.33f,.12f,.55f);tagEdge.effectDistance=new Vector2(1.5f,-1.5f);
            markerIcon=Icon(marker,icons[0],new Vector2(5,-3),30);markerLabel=Label(marker,"",new Vector2(38,-4),new Vector2(76,28),17,true);
            markerArrow=Rect("Direction",marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,22));
            var arrowImage=markerArrow.gameObject.AddComponent<Image>();arrowImage.sprite=arrow;arrowImage.color=new Color(.62f,.28f,.16f);arrowImage.raycastTarget=false;
            stampCard=Panel("Delivery stamp",hud,new Vector2(.5f,1),new Vector2(0,-170),new Vector2(400,98));
            stampGroup=stampCard.gameObject.AddComponent<CanvasGroup>();stampGroup.alpha=0;stampGroup.blocksRaycasts=false;
            stampMark=Icon(stampCard,stamp,new Vector2(12,-8),82).rectTransform;
            var stampWord=Label(stampMark,"PAID",new Vector2(0,0),new Vector2(82,82),18,true,new Vector2(0,1));stampWord.color=new Color(.72f,.16f,.14f,.9f);stampWord.alignment=TextAnchor.MiddleCenter;
            stampTitle=Label(stampCard,"",new Vector2(106,-12),new Vector2(284,32),22,true);stampDetail=Label(stampCard,"",new Vector2(106,-46),new Vector2(284,44),17);
            stampCard.gameObject.SetActive(false);
            flightHud.SetActive(false);
            var shade=Stretch("Sleep transition",root);
            fade=shade.gameObject.AddComponent<Image>();fade.color=new Color(.10f,.14f,.20f,0);fade.raycastTarget=false;
            caption=Label(shade,"",Vector2.zero,new Vector2(900,80),30,true,new Vector2(.5f,.5f));caption.alignment=TextAnchor.MiddleCenter;caption.color=new Color(.95f,.90f,.78f);
            caption.rectTransform.pivot=new Vector2(.5f,.5f);captionGroup=caption.gameObject.AddComponent<CanvasGroup>();captionGroup.alpha=0;
            ShowTitle();
        }

        RectTransform Stretch(string name,Transform parent)
        {
            var r=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;return r;
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
        Image Icon(Transform parent,Sprite sprite,Vector2 pos,float size)
        {
            var r=Rect("Illustration",parent,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(size,size));var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;
        }
        Image Icon(Transform parent,int index,Vector2 pos,float size)=>Icon(parent,icons[Mathf.Clamp(index,0,15)],pos,size);
        Image Bar(Transform parent,Vector2 pos,Color color)
        {
            var back=Rect("Meter",parent,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(150,15));back.gameObject.AddComponent<Image>().color=new Color(.80f,.78f,.68f);
            var fill=Rect("Fill",back,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);fill.offsetMin=Vector2.zero;fill.offsetMax=Vector2.zero;fill.pivot=new Vector2(0,.5f);
            var image=fill.gameObject.AddComponent<Image>();image.color=color;return image;
        }
        Button Button(Transform parent,string title,Vector2 pos,Vector2 size,Action action,bool enabled=true,Cue cue=Cue.Confirm)
        {
            var r=Rect(title,parent,new Vector2(0,1),new Vector2(0,1),pos,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.87f,.84f,.73f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.92f,.73f);colors.selectedColor=new Color(1,.87f,.60f);colors.pressedColor=new Color(.76f,.68f,.50f);colors.disabledColor=new Color(.9f,.9f,.9f,.5f);b.colors=colors;
            var t=Label(r,title,new Vector2(7,-3),size-new Vector2(14,6),size.y<40?17:18,true);t.alignment=TextAnchor.MiddleCenter;
            b.onClick.AddListener(()=>{if(App&&App.Sound)App.Sound.Play(cue);action();});return b;
        }
        void Focus(Button button){if(button&&EventSystem.current)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        public void ClosePanel(){if(panel){if(App&&App.Input)App.Input.RequireLiftRelease();Destroy(panel.gameObject);}panel=null;rows=null;bakeryStatus=null;activeScroll=null;lastSelection=null;page=Page.None;if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
        /// <summary>Escape / controller back: leave a sub-page for settings, or close the current page.</summary>
        public void Back()
        {
            if(page==Page.Title)return;
            if(page==Page.NewGame){App.Sound?.Play(Cue.Back);ShowTitle();return;}
            if(page==Page.Controls||page==Page.Sound){App.Sound?.Play(Cue.Back);ClosePanel();ToggleOptions();return;}
            ToggleOptions();
        }

        void ShowTitle()
        {
            ClosePanel();page=Page.Title;
            var screen=Stretch("Title",root);panel=screen;
            if(kiki){var portrait=Icon(screen,kiki,Vector2.zero,10).rectTransform;portrait.anchorMin=portrait.anchorMax=new Vector2(.5f,0);portrait.pivot=new Vector2(.5f,0);portrait.anchoredPosition=new Vector2(-330,0);portrait.sizeDelta=new Vector2(430,530);}
            bool save=App.HasSave;
            var card=Panel("Choose your pace",screen,new Vector2(.5f,.5f),new Vector2(150,-10),new Vector2(560,save?318:292));card.pivot=new Vector2(.5f,.5f);
            Label(card,"Kiki’s Delivery Service",new Vector2(32,-28),new Vector2(500,48),36,true);
            Label(card,"A bakery, a broom, and a town to get to know.",new Vector2(34,-84),new Vector2(500,30),20);
            Button first;
            if(save)
            {
                var s=App.Rules.State;
                first=Button(card,"Continue your deliveries",new Vector2(32,-134),new Vector2(496,52),()=>App.Continue(),true,Cue.Accept);
                var detail=Label(card,$"Day {App.Rules.Day} · {s.money} coins · {s.delivered} delivered · {(s.difficulty==Difficulty.Cozy?"Cozy":"Challenging")} pace",new Vector2(34,-194),new Vector2(494,26),16);detail.alignment=TextAnchor.MiddleCenter;
                Button(card,"Start a new game…",new Vector2(32,-236),new Vector2(496,42),ShowNewGame,true,Cue.Page);
            }
            else
            {
                first=Button(card,"Cozy",new Vector2(32,-134),new Vector2(240,50),()=>App.Begin(Difficulty.Cozy),true,Cue.Accept);
                Button(card,"Challenging",new Vector2(288,-134),new Vector2(240,50),()=>App.Begin(Difficulty.Challenging),true,Cue.Accept);
                Label(card,"Longer delivery windows and gentler crows.",new Vector2(34,-190),new Vector2(236,44),15).alignment=TextAnchor.UpperCenter;
                Label(card,"Tighter windows, bolder crows and late fees.",new Vector2(290,-190),new Vector2(236,44),15).alignment=TextAnchor.UpperCenter;
            }
            var footer=Label(card,"Keyboard + mouse or controller  ·  Esc / Start for settings in town",new Vector2(32,save?-284:-252),new Vector2(496,24),15);footer.alignment=TextAnchor.MiddleCenter;footer.color=new Color(.33f,.36f,.34f);
            Focus(first);
        }
        void ShowNewGame()
        {
            ClosePanel();page=Page.NewGame;
            panel=Panel("Start a new game",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(560,300));
            Label(panel,"Start a new game?",new Vector2(30,-24),new Vector2(500,40),28,true);
            var s=App.Rules.State;
            Label(panel,$"Your current town — Day {App.Rules.Day}, {s.money} coins, {s.delivered} delivered — is copied to a backup save before Kiki starts again.",new Vector2(30,-74),new Vector2(500,70),17).verticalOverflow=VerticalWrapMode.Overflow;
            Button(panel,"New game · Cozy",new Vector2(30,-160),new Vector2(242,46),()=>App.NewGame(Difficulty.Cozy),true,Cue.Accept);
            Button(panel,"New game · Challenging",new Vector2(288,-160),new Vector2(242,46),()=>App.NewGame(Difficulty.Challenging),true,Cue.Accept);
            Focus(Button(panel,"Keep my town",new Vector2(30,-226),new Vector2(500,46),ShowTitle,true,Cue.Back));
        }

        public void ShowBakery(){tab=0;BuildBakery();App.Sound?.Play(Cue.Page);}
        public void RefreshPanel(){if(panel&&rows&&page==Page.Bakery)BuildBakery();}
        void BuildBakery()
        {
            ClosePanel();page=Page.Bakery;panel=Panel("Bakery board",root,new Vector2(0,1),new Vector2(18,-151),new Vector2(604,530));
            // Reserve the clock card above and the vitality card below at every window aspect ratio.
            panel.anchorMin=Vector2.zero;panel.anchorMax=new Vector2(0,1);
            panel.offsetMin=new Vector2(18,118);panel.offsetMax=new Vector2(622,-114);
            Icon(panel,10,new Vector2(16,-10),40);
            Label(panel,"Osono’s bakery",new Vector2(62,-13),new Vector2(330,37),27,true);
            Button(panel,"Go outside",new Vector2(469,-14),new Vector2(113,34),ClosePanel,true,Cue.Back);
            string[] tabs={"Deliveries","Pantry","Kitchen","Broom"};
            for(int i=0;i<4;i++){int index=i;var button=Button(panel,tabs[i],new Vector2(18+i*145,-62),new Vector2(136,38),()=>{tab=index;BuildBakery();},true,Cue.Page);if(i==tab)button.image.color=new Color(.84f,.72f,.52f);}
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
                    string kind=p.kind==DeliveryKind.Parcel?"":p.kind+" · ";
                    string detail=$"{kind}{Catalog.FindDestination(p.destination).Name} · {p.weight:0.#} kg · {p.reward} coins\n"+(reason??$"{Mathf.RoundToInt((float)p.duration)} seconds after pickup");
                    var b=Row(p.kind==DeliveryKind.Fragile?14:0,p.title,detail,p.accepted?"On broom":"Collect",()=>{if(rules.Accept(p.id)){ClosePanel();App.Save();App.Sound?.Play(Cue.Accept);}else RefreshPanel();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            else if(tab==1)
            {
                for(int i=0;i<6;i++)
                {
                    var ingredient=(Ingredient)i;string reason=rules.IngredientReason(ingredient);
                    var b=Row(Catalog.IngredientIcons[i],Catalog.IngredientNames[i],$"In pantry: {rules.State.pantry[i]}\n"+(reason??"For something warm when you get home."),Catalog.IngredientCosts[i]+" coins",()=>{if(rules.Buy(ingredient))App.Sound?.Play(Cue.Coin);BuildBakery();App.Save();},reason==null);
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
                    var b=Row(u.Id==Upgrade.Lantern?2:1,u.Name+$"  {rules.Level(u.Id)}/{u.Max}",u.Description+"\n"+(reason??"Ready to fit to your broom."),rules.Cost(u.Id)+" coins",()=>{if(rules.Purchase(u.Id))App.Sound?.Play(Cue.Coin);BuildBakery();App.Save();},reason==null);
                    if(first==null&&b.interactable)first=b;
                }
            }
            var restIcon=Icon(panel,3,new Vector2(18,-481),32).rectTransform;
            restIcon.anchorMin=restIcon.anchorMax=Vector2.zero;restIcon.anchoredPosition=new Vector2(18,49);
            var rest=Button(panel,"Sleep · 8 hours",new Vector2(62,-479),new Vector2(179,35),App.Sleep,!rules.Cooking,Cue.Page);
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
            if(PanelOpen){App.Sound?.Play(Cue.Back);ClosePanel();return;}
            page=Page.Settings;App.Sound?.Play(Cue.Page);
            panel=Panel("Flight settings",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,524));
            Label(panel,"Settings",new Vector2(25,-20),new Vector2(300,40),30,true);
            var pace=Label(panel,$"Day {App.Rules.Day}  ·  {(App.Rules.State.difficulty==Difficulty.Cozy?"Cozy":"Challenging")} pace",new Vector2(300,-28),new Vector2(275,30),17);pace.alignment=TextAnchor.MiddleRight;
            var input=App.Input;
            string bindings=input.Controller
                ?$"Left stick: walk / fly · Right stick: look\n{input.South} or {input.Rise}: take off / rise · {input.Descend}: descend\n{input.BoostButton}: boost · {input.BrakeButton}: brake / hover\nL3: cruise · R3: recenter camera\n{input.West}: land / deliver · {input.North}: menu"
                :"WASD: walk / fly · Mouse: look\nSpace: take off / rise · Ctrl or C: descend\nShift: boost · Q: brake / hover\nF: cruise · R: recenter camera\nE: land / deliver · Tab / Escape: menu";
            Label(panel,bindings,new Vector2(25,-72),new Vector2(550,148),19);
            Button(panel,"Controls & camera",new Vector2(25,-232),new Vector2(270,43),()=>ShowControls(),true,Cue.Page);
            Button(panel,"Sound & display",new Vector2(305,-232),new Vector2(270,43),()=>ShowSound(),true,Cue.Page);
            var cozy=Button(panel,"Cozy",new Vector2(25,-290),new Vector2(270,43),()=>{App.Rules.SetDifficulty(Difficulty.Cozy);ClosePanel();});
            var hard=Button(panel,"Challenging",new Vector2(305,-290),new Vector2(270,43),()=>{App.Rules.SetDifficulty(Difficulty.Challenging);ClosePanel();});
            (App.Rules.State.difficulty==Difficulty.Cozy?cozy:hard).image.color=new Color(.84f,.72f,.52f);
            Focus(Button(panel,"Back outside",new Vector2(25,-360),new Vector2(550,44),ClosePanel,true,Cue.Back));
            Button(panel,"Save & quit",new Vector2(25,-420),new Vector2(550,44),()=>{App.Save();Application.Quit();},true,Cue.Back);
            var note=Label(panel,"The town keeps its own time while menus are open.",new Vector2(25,-480),new Vector2(550,28),15);note.alignment=TextAnchor.MiddleCenter;note.color=new Color(.35f,.37f,.35f);
        }
        public void ShowControls(string selected="Mouse sensitivity")
        {
            ClosePanel();page=Page.Controls;panel=Panel("Controls and camera",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,625));
            Icon(panel,1,new Vector2(25,-21),40);Label(panel,"Controls & camera",new Vector2(82,-23),new Vector2(525,40),29,true);
            Label(panel,"Walk, mount and land with the same controls.\nLanding assist works on clear streets as well as delivery courts.",new Vector2(25,-83),new Vector2(590,60),18);
            var input=App.Input;
            Action<string,string,int,Action> option=(id,value,row,change)=>
            {
                var button=Button(panel,id+"  ·  "+value,new Vector2(25,-157-row*57),new Vector2(590,44),()=>{change();input.SavePreferences();ShowControls(id);},true,Cue.Tick);
                button.name=id;if(selected==id)Focus(button);
            };
            option("Mouse sensitivity",$"{input.MouseSensitivity*100:0}%",0,()=>input.MouseSensitivity=input.MouseSensitivity>=1.5f?.75f:input.MouseSensitivity+.25f);
            option("Stick sensitivity",$"{input.ControllerSensitivity*100:0}%",1,()=>input.ControllerSensitivity=input.ControllerSensitivity>=1.5f?.75f:input.ControllerSensitivity+.25f);
            option("Stick dead zone",$"{input.Deadzone*100:0}%",2,()=>input.Deadzone=input.Deadzone>=.20f?.10f:input.Deadzone+.05f);
            option("Invert vertical look",input.InvertLookY?"On":"Off",3,()=>input.InvertLookY=!input.InvertLookY);
            option("Mouse look",input.AlwaysMouseLook?"Free look":"Hold right button",4,()=>input.AlwaysMouseLook=!input.AlwaysMouseLook);
            option("Camera follow",input.FollowSpeed<.1f?"Off":input.FollowSpeed>1?"Quick":"Gentle",5,()=>input.FollowSpeed=input.FollowSpeed>1?0:input.FollowSpeed+.8f);
            Button(panel,"Back to settings",new Vector2(25,-550),new Vector2(590,44),()=>{ClosePanel();ToggleOptions();},true,Cue.Back);
        }
        void ShowSound(string selected="Master volume")
        {
            ClosePanel();page=Page.Sound;panel=Panel("Sound and display",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,530));
            Icon(panel,2,new Vector2(25,-21),40);Label(panel,"Sound & display",new Vector2(82,-23),new Vector2(525,40),29,true);
            var sound=App.Sound;
            Action<string,string,int,Action> option=(id,value,row,change)=>
            {
                var button=Button(panel,id+"  ·  "+value,new Vector2(25,-90-row*57),new Vector2(590,44),()=>{change();ShowSound(id);},true,Cue.Tick);
                button.name=id;if(selected==id)Focus(button);
            };
            string Percent(float v)=>v<=.001f?"Off":$"{v*100:0}%";
            float Next(float v)=>v>=.99f?0:Mathf.Min(1,Mathf.Round(v*4+1)/4);
            if(sound)
            {
                option("Master volume",Percent(sound.Master),0,()=>{sound.Master=Next(sound.Master);sound.SavePreferences();});
                option("Music",Percent(sound.Music),1,()=>{sound.Music=Next(sound.Music);sound.SavePreferences();});
                option("Town and effects",Percent(sound.Effects),2,()=>{sound.Effects=Next(sound.Effects);sound.SavePreferences();});
            }
            bool full=Screen.fullScreenMode!=FullScreenMode.Windowed;
            option("Display",full?"Full screen":"Window",3,()=>{Screen.fullScreenMode=full?FullScreenMode.Windowed:FullScreenMode.FullScreenWindow;});
            option("Control hints",HintMode==0?"Show when useful":HintMode==1?"Always":"Hidden",4,()=>{HintMode=(HintMode+1)%3;if(!DevelopmentFlightCheck.Requested){PlayerPrefs.SetInt(Preferences+"hints",HintMode);PlayerPrefs.Save();}hintShown=Time.unscaledTime;});
            var note=Label(panel,"Every sound in town is synthesized by the game; no recordings or film music are used.",new Vector2(25,-382),new Vector2(590,40),15);note.alignment=TextAnchor.MiddleCenter;note.color=new Color(.35f,.37f,.35f);
            Button(panel,"Back to settings",new Vector2(25,-450),new Vector2(590,44),()=>{ClosePanel();ToggleOptions();},true,Cue.Back);
        }
        /// <summary>A stamped receipt after a handover, with the pay and timely tip.</summary>
        public void ShowDelivery(string destination,int pay,int tip)
        {
            stampTitle.text="Delivered to "+destination;
            stampDetail.text=$"+{pay} coins"+(tip>0?$"  ·  +{tip} timely tip":"")+"\nJiji approves.";
            stampClock=0;stampCard.gameObject.SetActive(true);
            // The stamp carries the same news as the rules notice; show it once.
            noticeId=App.Rules.NoticeRevision;noticeUntil=0;
        }
        void Tip(string key,string text,bool once=true)
        {
            if(once&&tipsSeen.Contains(key))return;
            if(once){tipsSeen.Add(key);if(!DevelopmentFlightCheck.Requested){PlayerPrefs.SetString(Preferences+"tips",string.Join(",",tipsSeen));PlayerPrefs.Save();}}
            ShowNotice(text,true);
        }
        void ShowNotice(string text,bool tip)
        {
            notice.text=text;noticeTip=tip;noticeUntil=Time.unscaledTime+Mathf.Clamp(3.2f+text.Length/20f,4,8.5f);
            noticeFace.gameObject.SetActive(tip&&jiji);
            notice.rectTransform.anchoredPosition=new Vector2(tip&&jiji?56:12,-4);notice.rectTransform.sizeDelta=new Vector2(tip&&jiji?412:456,46);
        }
        string Key(string keyboard,string pad)=>App.Input.Controller?pad:keyboard;
        void Update()
        {
            if(App==null)return;
            float now=Time.unscaledTime,dt=Time.unscaledDeltaTime;
            flightHud.SetActive(App.InGame);
            if(fade)
            {
                fade.color=new Color(.10f,.14f,.20f,Mathf.Clamp01(App.SleepFade));fade.transform.SetAsLastSibling();
                caption.text=App.FadeCaption;captionGroup.alpha=Mathf.Clamp01((App.SleepFade-.35f)*2.2f);
            }
            if(!App.InGame)return;
            sessionClock+=dt;
            var r=App.Rules;var motor=App.Motor;var input=App.Input;
            int hour=(int)r.Hour;int minute=(int)((r.Hour-hour)*60);
            clock.text=$"Day {r.Day} · {hour:00}:{minute:00}";money.text=r.State.money+" coins";
            dayIcon.sprite=r.IsNight?moon:sun;
            energy.rectTransform.localScale=new Vector3((float)r.State.energy/100,1,1);food.rectTransform.localScale=new Vector3((float)r.State.fullness/100,1,1);
            energyText.text=$"{r.State.energy:0}%";foodText.text=$"{r.State.fullness:0}%";
            float pulse=.5f+.5f*Mathf.Sin(now*6);
            energy.color=r.State.energy<20?Color.Lerp(new Color(.38f,.56f,.43f),urgent,pulse):new Color(.38f,.56f,.43f);
            food.color=r.State.fullness<15?Color.Lerp(new Color(.77f,.53f,.30f),urgent,pulse):new Color(.77f,.53f,.30f);
            var p=r.Active;var target=p!=null?Catalog.FindDestination(p.destination):null;
            parcelCard.gameObject.SetActive(p!=null&&!PanelOpen);
            if(p!=null)
            {
                double left=p.deadline-r.State.elapsed;
                jobTitle.text=target.Name;
                string kind=p.kind==DeliveryKind.Fragile?$"  ·  fragile {p.integrity:0}%":p.kind==DeliveryKind.Rush?"  ·  rush":"";
                jobDetail.text=$"{Mathf.CeilToInt((float)left)} s left  ·  {r.State.position.HorizontalDistance(target.Landing):0} m{kind}";
                bool hurry=left<p.duration*.25||left<15;
                jobDetail.color=hurry?Color.Lerp(ink,urgent,.55f+.45f*pulse):ink;
            }
            noticeCard.anchoredPosition=new Vector2(0,p==null?-18:-100);
            prompt.text=PanelOpen?"":r.CanDeliver?Key("E",input.West)+"  ·  Deliver parcel":r.AtHome?Key("E",input.West)+"  ·  Visit Osono":motor.Approaching?"Settling into the court…":"";
            promptCard.gameObject.SetActive(prompt.text.Length>0&&stampClock>2.6f);
            // Notices: rule messages first, then Jiji's one-time tips when the screen is quiet.
            if(noticeId!=r.NoticeRevision){noticeId=r.NoticeRevision;ShowNotice(r.Notice,false);}
            if(!motor.Approaching&&(notice.text.StartsWith("Easing into")||notice.text.StartsWith("Coming down"))&&noticeUntil>now+.6f)noticeUntil=now+.6f;
            if(!PanelOpen&&now>noticeUntil&&sessionClock>1.5f&&stampClock>5)Tips(r,motor,target);
            noticeGroup.alpha=Mathf.MoveTowards(noticeGroup.alpha,!PanelOpen&&now<noticeUntil&&!string.IsNullOrEmpty(notice.text)?1:0,dt*4);
            noticeCard.gameObject.SetActive(noticeGroup.alpha>.01f);
            Hints(r,motor,input,now,dt);
            Marker(r,motor,target);
            if(stampCard.gameObject.activeSelf)
            {
                stampClock+=dt;float s=stampClock<.2f?Mathf.Lerp(1.45f,1,Mathf.SmoothStep(0,1,stampClock/.2f)):1;
                stampMark.localScale=Vector3.one*s;stampMark.localRotation=Quaternion.Euler(0,0,-12);
                stampGroup.alpha=Mathf.Clamp01(stampClock/.12f)*(1-Mathf.Clamp01((stampClock-3.4f)/.5f));
                if(stampClock>3.9f)stampCard.gameObject.SetActive(false);
            }
            if(bakeryStatus)bakeryStatus.text=r.Cooking?$"Cooking… {Mathf.CeilToInt((float)r.State.cookingRemaining)}s":"The clock keeps moving while you plan.";
            if(day!=r.Day||night!=r.IsNight||recipe!=r.State.recipeId){day=r.Day;night=r.IsNight;recipe=r.State.recipeId;RefreshPanel();}
        }
        void Tips(Rules r,FlightMotor motor,Destination target)
        {
            string fly=Key("Space","A"),land=Key("E","X");
            if(r.State.energy<22&&!lowEnergy){lowEnergy=true;Tip("tired","Jiji: You’re worn out, Kiki. Eat or sleep at the bakery before you collapse.",false);return;}
            if(r.State.energy>35)lowEnergy=false;
            if(r.State.fullness<18&&!lowFood){lowFood=true;Tip("hungry","Jiji: That’s your tummy rumbling. Cook something warm in Osono’s kitchen.",false);return;}
            if(r.State.fullness>30)lowFood=false;
            if(target!=null&&motor.OnFoot&&!r.CanDeliver){Tip("takeoff",$"Jiji: Follow the paper tag to the ribbon circle. {fly} takes off.");return;}
            if(target!=null&&motor.Mounted&&r.State.position.HorizontalDistance(target.Landing)<70){Tip("land",$"Jiji: Slow down over the ribbon circle, then {land} lands us.");return;}
            if(target==null&&r.State.delivered>0&&!r.AtHome){Tip("home","Jiji: Nicely done! The ring outside the bakery shows the way home.");return;}
            if(r.IsNight&&r.Level(Upgrade.Lantern)==0){Tip("night","Jiji: The lamps are lit. Night deliveries need the moonlight lantern.");return;}
        }
        void Hints(Rules r,FlightMotor motor,FlightInput input,float now,float dt)
        {
            string text=motor.OnFoot
                ?(input.Controller?$"Left stick walk  ·  {input.South} / {input.Rise} take off  ·  {input.West} deliver / visit  ·  Right stick look  ·  {input.North} menu":"WASD walk  ·  Space take off  ·  E deliver / visit  ·  Mouse look  ·  Tab menu")
                :(input.Controller?$"LS fly  ·  {input.Rise}/{input.Descend} rise & descend  ·  {input.BoostButton} boost  ·  {input.BrakeButton} brake  ·  L3 cruise{(motor.Cruising?" ON":"")}  ·  R3 recenter  ·  {input.West} land":$"WASD fly  ·  Space/C rise & descend  ·  Shift boost  ·  Q brake  ·  F cruise{(motor.Cruising?" ON":"")}  ·  R recenter  ·  E land");
            if(text!=lastHint){lastHint=text;hintShown=now;}
            hints.text=text;
            // Stay clear of the vitality card on the left and mirror that margin on the right.
            float width=Mathf.Min(860,root.rect.width-2*(18+270+14));
            hintCard.sizeDelta=new Vector2(width,38);hints.rectTransform.sizeDelta=new Vector2(width-24,32);
            bool active=input.Move.sqrMagnitude>.01f||input.Look.sqrMagnitude>.01f||Mathf.Abs(input.Lift)>.1f;
            idleClock=active?0:idleClock+dt;
            // Show when useful: at the start of a visit, when the controls change, or after a long pause.
            bool show=HintMode==1||HintMode==0&&(sessionClock<45||now-hintShown<9||idleClock>14);
            if(PanelOpen)show=false;
            hintGroup.alpha=Mathf.MoveTowards(hintGroup.alpha,show?1:0,dt*(show?3:.8f));
            hintCard.gameObject.SetActive(hintGroup.alpha>.01f);
        }
        void Marker(Rules r,FlightMotor motor,Destination target)
        {
            if(target==null&&!r.AtHome&&r.State.position.HorizontalDistance(Catalog.Home.Landing)>25)target=Catalog.Home;
            var camera=Camera.main;
            if(target==null||PanelOpen||!camera){marker.gameObject.SetActive(false);return;}
            float distance=(float)r.State.position.HorizontalDistance(target.Landing);
            if(distance<9){marker.gameObject.SetActive(false);return;}
            marker.gameObject.SetActive(true);
            markerIcon.sprite=target==Catalog.Home?icons[10]:icons[0];markerLabel.text=$"{distance:0} m";
            var world=new Vector3((float)target.Landing.x,(float)target.Landing.y+3.2f,(float)target.Landing.z);
            var screen=camera.WorldToScreenPoint(world);
            bool behind=screen.z<0;if(behind){screen.x=Screen.width-screen.x;screen.y=Screen.height-screen.y;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var local);
            var area=root.rect;
            float left=area.xMin+70,right=area.xMax-70,bottom=area.yMin+118,top=area.yMax-120;
            bool inside=!behind&&local.x>left&&local.x<right&&local.y>bottom&&local.y<top;
            Vector2 center=new Vector2((left+right)*.5f,(bottom+top)*.5f);
            if(!inside)
            {
                var d=local-center;if(behind&&d.sqrMagnitude<1)d=Vector2.down;
                float scale=Mathf.Min(Mathf.Abs((d.x>0?right-center.x:center.x-left)/Mathf.Max(.001f,Mathf.Abs(d.x))),Mathf.Abs((d.y>0?top-center.y:center.y-bottom)/Mathf.Max(.001f,Mathf.Abs(d.y))));
                local=center+d*scale;
                markerArrow.gameObject.SetActive(true);
                var dir=d.normalized;markerArrow.anchoredPosition=dir*48;markerArrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg-90);
            }
            else markerArrow.gameObject.SetActive(false);
            marker.anchoredPosition=local;
        }
        void LateUpdate()
        {
            if(!EventSystem.current)return;
            var selected=EventSystem.current.currentSelectedGameObject;
            if(selected==lastSelection)return;
            bool moved=lastSelection!=null&&selected!=null;
            lastSelection=selected;
            if(moved&&App&&App.Sound)App.Sound.Play(Cue.Tick,.8f);
            if(!activeScroll||!selected||!rows||!selected.transform.IsChildOf(rows))return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(activeScroll.viewport,(RectTransform)selected.transform);
            var viewport=activeScroll.viewport.rect;
            float correction=bounds.max.y>viewport.yMax?viewport.yMax-bounds.max.y:bounds.min.y<viewport.yMin?viewport.yMin-bounds.min.y:0;
            var position=rows.anchoredPosition;position.y+=correction;
            position.y=Mathf.Clamp(position.y,0,Mathf.Max(0,rows.rect.height-viewport.height));
            rows.anchoredPosition=position;activeScroll.StopMovement();
        }

        // Small illustrations drawn by the game: coin, sun, moon, receipt stamp and tag arrow.
        static Sprite Painted(int size,Func<float,float,float,Color> paint)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,name="Drawn interface mark"};
            var pixels=new Color[size*size];float px=2f/size;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=paint((x+.5f)*px-1,(y+.5f)*px-1,px);
            texture.SetPixels(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
        }
        static float Edge(float distance,float px)=>Mathf.Clamp01(.5f-distance/px);
        static Color DrawCoin(float x,float y,float px)
        {
            float r=Mathf.Sqrt(x*x+y*y);float disc=Edge(r-.86f,px);
            var gold=new Color(.90f,.70f,.28f);var dark=new Color(.62f,.42f,.14f);var light=new Color(.99f,.88f,.52f);
            var c=Color.Lerp(gold,light,Mathf.Clamp01((-x+y)*.6f+.1f));
            c=Color.Lerp(c,dark,Edge(Mathf.Abs(r-.80f)-.05f,px)*.9f);
            c=Color.Lerp(c,dark,Edge(Mathf.Abs(r-.50f)-.035f,px)*.55f);
            c.a=disc;return c;
        }
        static Color DrawSun(float x,float y,float px)
        {
            float r=Mathf.Sqrt(x*x+y*y),a=Mathf.Atan2(y,x);
            float ray=Edge(r-(.62f+.30f*Mathf.Pow(Mathf.Abs(Mathf.Cos(a*4)),6)),px);
            float disc=Edge(r-.46f,px);
            var c=Color.Lerp(new Color(.95f,.62f,.24f),new Color(.99f,.80f,.36f),disc);c.a=Mathf.Max(ray,disc);return c;
        }
        static Color DrawMoon(float x,float y,float px)
        {
            float outer=Mathf.Sqrt(x*x+y*y)-.72f,inner=Mathf.Sqrt((x-.34f)*(x-.34f)+(y-.22f)*(y-.22f))-.60f;
            float shape=Edge(Mathf.Max(outer,-inner),px);
            var c=new Color(.95f,.88f,.62f);c.a=shape;return c;
        }
        static Color DrawStamp(float x,float y,float px)
        {
            float r=Mathf.Sqrt(x*x+y*y),a=Mathf.Atan2(y,x);
            // An inked rubber stamp: two rings with a slightly uneven press.
            float press=.72f+.28f*Mathf.Clamp01(Mathf.Sin(a*3+1.3f)*.5f+Mathf.Sin(a*7.1f)*.3f+.6f);
            float ring=Mathf.Max(Edge(Mathf.Abs(r-.86f)-.07f,px),Edge(Mathf.Abs(r-.66f)-.025f,px));
            var c=new Color(.72f,.16f,.14f);c.a=ring*press*.9f;return c;
        }
        static Color DrawArrow(float x,float y,float px)
        {
            // A soft triangle pointing up.
            float inside=Edge(Mathf.Max(-y-.55f,y*.52f+Mathf.Abs(x)*.95f-.45f),px);
            var c=Color.white;c.a=inside;return c;
        }
    }
}
