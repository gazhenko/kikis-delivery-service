using System;
using System.IO;
using UnityEngine;

namespace Koriko
{
    /// <summary>Development only: measures the listener's final mix and can record it to WAV.
    /// Added at runtime by the tour check; never present in normal play.</summary>
    public sealed class AudioProbe : MonoBehaviour
    {
        public int SampleRate {get;private set;}
        double sum;long count;float peak;bool invalid;
        float[] recording;int written;volatile bool record;
        void Awake(){SampleRate=AudioSettings.outputSampleRate;}
        public void Reset(){sum=0;count=0;peak=0;invalid=false;}
        public (float rms,float peak,bool valid) Measure()=>(count>0?(float)Math.Sqrt(sum/count):0,peak,!invalid);
        public void Record(float seconds){recording=new float[(int)(seconds*SampleRate)];written=0;record=true;}
        public bool Recorded=>recording!=null&&written>=recording.Length;
        void OnAudioFilterRead(float[] data,int channels)
        {
            for(int i=0;i<data.Length;i+=channels)
            {
                float mono=0;for(int c=0;c<channels;c++){float v=data[i+c];if(float.IsNaN(v)||float.IsInfinity(v))invalid=true;else mono+=v;}
                mono/=channels;sum+=mono*mono;count++;peak=Math.Max(peak,Math.Abs(mono));
                if(record&&recording!=null&&written<recording.Length)recording[written++]=mono;
            }
        }
        public void SaveRecording(string path){if(recording!=null)WriteWav(path,recording,Math.Min(written,recording.Length),SampleRate);}
        public static void WriteWav(string path,float[] samples,int length,int rate)
        {
            using var stream=new BinaryWriter(File.Create(path));
            stream.Write(new[]{'R','I','F','F'});stream.Write(36+length*2);stream.Write(new[]{'W','A','V','E','f','m','t',' '});
            stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);
            stream.Write(new[]{'d','a','t','a'});stream.Write(length*2);
            for(int i=0;i<length;i++)stream.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i],-1,1)*32767));
        }
    }
}
