using System;
using System.IO;
using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    [DefaultExecutionOrder(-30)]
    public sealed class GameApp : MonoBehaviour
    {
        public FlightMotor Motor;
        public FlightInput Input;
        public GameHud Hud;
        public Daylight Lighting;
        public Soundscape Sound;
        public TownLife Life;
        public Rules Rules { get; private set; }
        public RiderPerformance Rider { get; private set; }
        public bool HasSave { get; private set; }
        public bool InGame { get; private set; }
        public bool Checking => DevelopmentFlightCheck.Requested;
        public float SleepFade { get; private set; }
        public string FadeCaption { get; private set; } = "";
        public string SavePath => Path.Combine(Application.persistentDataPath,"koriko-desktop-v1.json");
        public string BackupPath => Path.Combine(Application.persistentDataPath,"koriko-desktop-v1.before-new-game.json");
        float autosave;
        int homeReturns;

        void Awake()
        {
            Rules=new Rules();
            try
            {
                if(!Checking&&File.Exists(SavePath)&&new FileInfo(SavePath).Length<100000)
                    HasSave=Rules.Restore(JsonUtility.FromJson<State>(File.ReadAllText(SavePath)));
            }
            catch(Exception e){Debug.LogWarning("Save could not be read: "+e.Message);}
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            Motor.Warp(HasSave?Rules.State.position:Catalog.Home.Landing);
            Rider=Motor.Visual.GetComponentInChildren<RiderPerformance>();
            if(Hud)Hud.Initialize(this);
        }

        public void Begin(Difficulty difficulty)
        {
            Rules.Start(difficulty);InGame=true;HasSave=true;
            Hud.ClosePanel();if(Rules.AtHome)Hud.ShowBakery();Save();
        }
        /// <summary>Start again from day one. The previous save is copied aside first.</summary>
        public void NewGame(Difficulty difficulty)
        {
            if(!Checking&&File.Exists(SavePath))
            {
                try{File.Copy(SavePath,BackupPath,true);}
                catch(Exception e){Debug.LogWarning("Previous save could not be copied: "+e.Message);Hud.ClosePanel();Rules.Say("Your previous save could not be backed up, so it was kept.");return;}
            }
            Rules=new Rules();homeReturns=0;autosave=0;
            Motor.Warp(Catalog.Home.Landing);
            Begin(difficulty);
        }
        public void Continue()
        {
            if(!HasSave)return;
            InGame=true;Hud.ClosePanel();
            if(Rules.AtHome)Hud.ShowBakery();
        }
        void Update()
        {
            float elapsed=Time.deltaTime;
            float dt=Mathf.Min(elapsed,.1f);
            Input.Read(Hud.PanelOpen||!InGame);
            if(Lighting)Lighting.Refresh(Rules,InGame);
            SleepFade=Mathf.MoveTowards(SleepFade,0,Time.unscaledDeltaTime*.7f);
            if(!InGame){if(Input.Back)Hud.Back();return;}
            HandleHomeReturn();
            if(Input.Back)Hud.Back();
            else if(Input.Menu)
            {
                if(Hud.PanelOpen)Hud.ToggleOptions();
                else if(Rules.AtHome)Hud.ShowBakery();
                else Hud.ToggleOptions();
            }
            if(Input.Interact&&!Hud.PanelOpen&&SleepFade<.05f)Interact();
            Motor.Simulate(dt,Input,Rules,Hud.PanelOpen||Rules.Cooking||SleepFade>.05f);
            Rules.Advance(elapsed,Motor.Frame);
            HandleHomeReturn();
            autosave+=elapsed;
            if(autosave>=8){autosave=0;Save();}
        }
        void HandleHomeReturn()
        {
            if(Rules.HomeReturns==homeReturns)return;
            homeReturns=Rules.HomeReturns;Motor.Warp(Catalog.Home.Landing);Motor.Face(85);
            Rules.Observe(Motor.Frame);Hud.ClosePanel();SleepFade=1.9f;FadeCaption="Osono brought you home to rest.";Save();
        }
        string Key(string keyboard,string pad)=>Input.Controller?pad:keyboard;
        public void Interact()
        {
            if(Motor.Approaching){Motor.CancelApproach();Rules.Say("Landing cancelled. Hovering.");return;}
            Rules.Observe(Motor.Frame);
            var active=Rules.Active;
            if(Rules.CanDeliver)
            {
                var destination=Catalog.FindDestination(active.destination);
                if(Rules.Deliver()){Hud.ShowDelivery(destination.Name,Rules.LastPay,Rules.LastTip);Rider?.Deliver();}
                Hud.RefreshPanel();Save();return;
            }
            if(Rules.AtHome){Hud.ShowBakery();return;}
            Destination nearest=Catalog.Home;
            foreach(var destination in Catalog.Destinations)
                if(Rules.State.position.HorizontalDistance(destination.Landing)<Rules.State.position.HorizontalDistance(nearest.Landing))nearest=destination;
            if(Motor.BeginApproach(nearest)){Rules.Say("Easing into "+nearest.Name+". Steer or climb to cancel.");return;}
            if(Motor.LandHere()){Rules.Say("Coming down to the street. Steer or brake to cancel.");return;}
            var target=active!=null?Catalog.FindDestination(active.destination):null;
            string fly=Key("Space","A"),land=Key("E","X");
            if(Motor.OnFoot)
            {
                if(target!=null&&Rules.Near(target))Rules.Say("Stand still inside the ribbon circle to hand the parcel over.");
                else if(target!=null)Rules.Say($"{target.Name} is {Rules.State.position.HorizontalDistance(target.Landing):0} m away. {fly} takes off.");
                else Rules.Say(Rules.State.position.HorizontalDistance(Catalog.Home.Landing)<30?"Walk into the bakery courtyard to visit Osono.":$"No parcel on your broom. {fly} takes off; Osono’s bakery has the next delivery.");
            }
            else Rules.Say(target!=null?$"Fly lower over the ribbon circle at {target.Name}, then press {land} to land.":$"Fly lower over a clear street or court, then press {land} to land.");
        }
        public void Sleep()
        {
            if(Rules.Sleep()){SleepFade=2.1f;FadeCaption="Eight hours later…";Sound?.Play(Cue.Lullaby);Save();Hud.RefreshPanel();}
        }
        public void Save()
        {
            if(Checking||!InGame||!Koriko.Core.Rules.Validate(Rules.State))return;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var temporary=SavePath+".tmp";
                File.WriteAllText(temporary,JsonUtility.ToJson(Rules.State));
                if(File.Exists(SavePath))File.Replace(temporary,SavePath,SavePath+".bak");
                else File.Move(temporary,SavePath);
            }
            catch(Exception e){Debug.LogWarning("Save could not be written: "+e.Message);}
        }
        void OnApplicationFocus(bool focused){if(!focused)Save();}
        void OnApplicationQuit(){Save();}
    }
}
