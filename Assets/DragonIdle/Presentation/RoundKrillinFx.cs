using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 internal sealed partial class RoundMotionFx {
  internal Vector2 KrillinDiscCenter {get;private set;}
  internal float KrillinDiscRadius {get;private set;}
  internal int KrillinDiscTrailParticles {get;private set;}

  void DrawKrillinDiscRevised(VertexHelper mesh){
   const float gather=256,orbitStart=450,orbitPeriod=440,fadeLength=320;
   float age=time-clip.impactAt;
   if(time<gather||age>=fadeLength)return;
   float launch=clip.launchAt;
   bool flying=time>=launch;
   float orbit=Mathf.Clamp(time-orbitStart,0,orbitPeriod*2)*Mathf.PI*2/orbitPeriod;
   float build=Ease((time-gather)/350);
   float travel=Mathf.InverseLerp(launch,clip.impactAt,time);
   float dissolve=Ease(Mathf.Max(age,0)/fadeLength);
   float opacity=Ease((time-gather)/100)*(1-dissolve);
   var offset=new Vector2(0,-7);
   var origin=Palm(launch,false)+offset;
   var target=beamTarget??new Vector2(beamEnd,188);
   var direction=(target-origin).normalized;
   var normal=new Vector2(-direction.y,direction.x);
   var center=flying
    ? Vector2.Lerp(origin,target,travel)+new Vector2(0,-13*Mathf.Sin(travel*Mathf.PI))
       +direction*(76*Mathf.InverseLerp(0,fadeLength,age))
    : Palm(time,false)+offset+new Vector2(Mathf.Sin(orbit)*2.4f,Mathf.Cos(orbit)*1.55f);
   float radius=flying?Mathf.Lerp(39,47,travel)*(1-.50f*dissolve):Mathf.Lerp(7,39,build);
   float flatten=flying?Mathf.Lerp(.14f,.21f,travel):.15f+.022f*Mathf.Sin(orbit);
   float tilt=flying?Mathf.Lerp(-.10f,.07f,travel):-.035f+.105f*Mathf.Sin(orbit);
   KrillinDiscCenter=center;
   KrillinDiscRadius=radius;

   if(flying){
    for(int i=0;i<4;i++){
     var edge=center+normal*((i-1.5f)*3.8f);
     Line(mesh,edge-direction*(23+travel*32+i*6),edge-direction*(8+i*2),
      i==1?2.2f:1.1f,Alpha(i==1?DiscWhite:DiscGold,opacity*(i==1?.76f:.54f)));
    }
   }

   AddDiscSurface(mesh,center,radius,flatten,tilt,opacity);
   for(int i=0;i<3;i++){
    float angle=time*.031f+i*Mathf.PI*2/3;
    Arc(mesh,center,radius*(1.06f+i*.045f),radius*flatten+1.2f+i*.8f,
     angle,angle+.56f,tilt,Alpha(DiscGold,opacity*.77f),1.15f);
   }

   if(age<=0)return;
   for(int i=0;i<16;i++){
    float drift=age-i*9;
    if(drift<=0)continue;
    float phase=Mathf.Clamp01(drift/(fadeLength-i*9));
    float side=(i%2==0?1:-1)*(2+i%5*2.8f);
    var mote=center-direction*(6+i*2.6f+drift*(.025f+i%4*.009f))
     +normal*(side*(.25f+phase*1.35f));
    var color=Alpha(i%3==0?DiscWhite:DiscGold,(1-Ease(phase))*.88f);
    Line(mesh,mote-direction*(1.8f+i%3),mote,i%3==0?1.55f:.92f,color);
    KrillinDiscTrailParticles++;
   }
  }

  void AddDiscSurface(VertexHelper mesh,Vector2 center,float radius,float flatten,float tilt,float opacity){
   const int slices=36,stride=5;
   int first=mesh.currentVertCount;
   for(int i=0;i<=slices;i++){
    float angle=i*(Mathf.PI*2/slices);
    var rim=Oval(radius,radius*flatten,angle,tilt);
    Vertex(mesh,center,Alpha(DiscWhite,opacity));
    Vertex(mesh,center+rim*.81f,Alpha(DiscWhite,opacity));
    Vertex(mesh,center+rim*.92f,Alpha(DiscGold,opacity));
    Vertex(mesh,center+rim,Alpha(DiscGold,opacity*.95f));
    Vertex(mesh,center+rim*1.22f,Alpha(DiscGold,0));
    if(i==0)continue;
    int a=first+(i-1)*stride;
    for(int edge=0;edge<stride-1;edge++){
     int start=a+edge;
     mesh.AddTriangle(start,start+1,start+stride);
     mesh.AddTriangle(start+1,start+stride+1,start+stride);
    }
   }
  }
 }
}
