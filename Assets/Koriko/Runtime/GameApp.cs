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
        public Rules Rules { get; private set; }
        public bool HasSave { get; private set; }
        public bool InGame { get; private set; }
        public bool Checking => DevelopmentFlightCheck.Requested;
        public float SleepFade { get; private set; }
        public string SavePath => Path.Combine(Application.persistentDataPath,"koriko-desktop-v1.json");
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
            if(Hud)Hud.Initialize(this);
        }

        public void Begin(Difficulty difficulty)
        {
            Rules.Start(difficulty);InGame=true;HasSave=true;
            Hud.ClosePanel();if(Rules.AtHome)Hud.ShowBakery();Save();
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
            Input.Read();
            if(Lighting)Lighting.Refresh(Rules,InGame);
            SleepFade=Mathf.MoveTowards(SleepFade,0,Time.unscaledDeltaTime*.7f);
            if(!InGame)return;
            HandleHomeReturn();
            if(Input.Back)Hud.ToggleOptions();
            else if(Input.Menu)
            {
                if(Hud.PanelOpen)Hud.ClosePanel();
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
            homeReturns=Rules.HomeReturns;Motor.Warp(Catalog.Home.Landing);
            Rules.Observe(Motor.Frame);Hud.ClosePanel();SleepFade=1;Save();
        }
        public void Interact()
        {
            Rules.Observe(Motor.Frame);
            if(Rules.CanDeliver){Rules.Deliver();Hud.RefreshPanel();Save();return;}
            if(Rules.AtHome){Hud.ShowBakery();return;}
            Destination nearest=Catalog.Home;
            foreach(var destination in Catalog.Destinations)
                if(Rules.State.position.HorizontalDistance(destination.Landing)<Rules.State.position.HorizontalDistance(nearest.Landing))nearest=destination;
            if(Motor.BeginApproach(nearest))Rules.Say("Easing into "+nearest.Name+". Steer or climb to cancel.");
            else Rules.Say("Approach the delivery court, then press E or X to land.");
        }
        public void Sleep()
        {
            if(Rules.Sleep()){SleepFade=1;Save();Hud.RefreshPanel();}
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
