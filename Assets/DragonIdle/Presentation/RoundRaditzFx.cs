using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 internal sealed partial class RoundMotionFx {
  static readonly Color RaditzPink=new Color32(253,40,184,255);
  static readonly Color RaditzPurple=new Color32(108,24,200,255);
  static readonly Color RaditzIndigo=new Color32(37,12,83,255);
  static readonly Color RaditzWhite=new Color32(254,246,255,255);
  internal float RaditzSphereRadius {get;private set;}
  internal int RaditzBeamBranches {get;private set;}

  const float RaditzNormalRadius=33,RaditzEnhancedRadius=44;
  void PaintRaditzSphere(VertexHelper mesh,Vector2 at,float radius,float phase,float alpha){
   if(alpha<=0||radius<=0)return;
   effectScale=1;
   RaditzSphereRadius=Mathf.Max(radius,RaditzSphereRadius);
   Glow(mesh,at,radius*1.28f,radius*1.28f,Alpha(RaditzPink,.74f*alpha),Alpha(RaditzPink,0));
   Glow(mesh,at,radius,radius,Alpha(RaditzIndigo,alpha),Alpha(RaditzPurple,.97f*alpha));
   Arc(mesh,at,radius*.98f,radius*.98f,0,Mathf.PI*2,0,Alpha(RaditzPink,.94f*alpha),3.6f);
   Arc(mesh,at,radius*1.065f,radius*1.065f,phase*.011f,phase*.011f+3.7f,0,
    Alpha(new Color32(227,117,255,255),.68f*alpha),1.8f);
   Glow(mesh,at,radius*.44f,radius*.44f,Alpha(new Color32(114,65,255,255),.91f*alpha),Alpha(RaditzIndigo,0));
   Glow(mesh,at,radius*.16f,radius*.16f,Alpha(RaditzWhite,alpha),Alpha(RaditzPurple,0));
   for(int i=0;i<7;i++){
    float angle=i*Mathf.PI*2/7+phase*.012f;
    Vector2 p=at+Oval(radius*.24f,radius*.24f,angle,0);
    for(int bend=0;bend<4;bend++){
     float r=radius*(.37f+bend*.14f);
     float zig=(1-2*(bend%2))*(.21f+i%3*.055f)+.055f*Mathf.Sin(phase*.04f+i);
     var q=at+Oval(r,r,angle+zig,0);
     Line(mesh,p,q,bend%2==0?1.9f:1.1f,Alpha(bend==1?RaditzWhite:RaditzPurple,.82f*alpha));
     p=q;
    }
   }
  }

  void DrawRaditzVolatileOrb(VertexHelper mesh,VolleyOrb orb){
   float age=orb.Elapsed-orb.Flight;
   float size=orb.Enhanced?RaditzEnhancedRadius:RaditzNormalRadius;
   float travel=Ease(orb.Elapsed/orb.Flight);
   var direction=(orb.End-orb.Start).normalized;
   var start=orb.Start+Vector2.right*(size*.78f);
   var end=orb.End+(orb.Miss?new Vector2(24,-30):Vector2.zero);
   var point=Vector2.Lerp(start,end,travel)-Vector2.up*(11*Mathf.Sin(Mathf.PI*travel));
   if(age<0){
    PaintRaditzSphere(mesh,point,size,orb.Elapsed+490,1);
    for(int i=0;i<5;i++){
     Vector2 edge=point+new Vector2(0,(i-2)*size*.25f);
     Line(mesh,edge-direction*(size+28*travel),edge-direction*(size*.48f),1.2f,Alpha(RaditzPurple,travel*.67f));
    }
    return;
   }
   float fade=1-Ease(age/185);
   PaintRaditzSphere(mesh,point,size*(1+.15f*Ease(age/80)),orb.Elapsed+490,fade*.81f);
   if(orb.Miss||fade<=0)return;
   Glow(mesh,end,size*1.25f+age*.10f,size*1.18f+age*.10f,Alpha(RaditzWhite,fade*.78f),Alpha(RaditzPink,0));
   for(int i=0;i<11;i++){
    float a=i*.58f+orb.Index*.24f;
    var ray=Oval(1,1,a,0);
    Line(mesh,end+ray*(12+age*.10f),end+ray*(size+i%3*3+age*.24f),1.4f,Alpha(RaditzPink,fade));
   }
  }

  void DrawRaditzTwinBeam(VertexHelper mesh,VolleyOrb orb){
   float age=orb.Elapsed-orb.Flight;
   float fade=1-Ease((age-40)/145);
   if(fade<=0)return;
   effectScale=1;
   RaditzBeamBranches++;
   var end=orb.End+(orb.Miss?new Vector2(18,-15):Vector2.zero);
   var head=Vector2.Lerp(orb.Start,end,Ease(orb.Elapsed/orb.Flight));
   var axis=(end-orb.Start).normalized;
   var normal=new Vector2(-axis.y,axis.x);
   float width=(18+12*(1-Ease(orb.Elapsed/230)))*fade;
   DrawWaveLayer(mesh,orb.Start,head,normal,width*1.28f,fade,RaditzPurple,.56f);
   DrawWaveLayer(mesh,orb.Start,head,normal,width,fade,RaditzPink,.80f);
   DrawWaveLayer(mesh,orb.Start,head,normal,width*.46f,fade,RaditzWhite,1);
   Glow(mesh,orb.Start,24*fade,18*fade,Alpha(RaditzWhite,.94f*fade),Alpha(RaditzPink,0));
   Glow(mesh,head,19*fade,17*fade,Alpha(RaditzWhite,.86f*fade),Alpha(RaditzPink,0));
   for(int i=0;i<5;i++){
    var from=Vector2.Lerp(orb.Start,head,(i+1)/8f);
    var to=Vector2.Lerp(orb.Start,head,Mathf.Min(1,(i+3)/8f));
    var side=normal*(i%2==0?1:-1)*(width*.70f+i*1.1f);
    Line(mesh,from+side,to+side*.42f,1.4f,Alpha(RaditzPink,fade*.77f));
   }
  }

  void DrawRaditzCharge(VertexHelper mesh){
   if(time>=clip.launchAt)return;
   effectScale=1;
   var pose=art.At(clip,time);
   var center=RoundMotionArt.Anchor(pose,pose.beam,Ground(time),poseScale);
   float gain=Ease((time-115)/(clip.launchAt*.60f));
   if(clip.id=="raditzTwin"){
    var other=RoundMotionArt.Anchor(pose,pose.offhand,Ground(time),poseScale);
    foreach(var hand in new[]{center,other}){
     Glow(mesh,hand,21*gain,19*gain,Alpha(RaditzWhite,.85f*gain),Alpha(RaditzPink,0));
     Arc(mesh,hand,23*gain,20*gain,time*.018f,time*.018f+3.2f,0,Alpha(RaditzPink,gain),2.0f);
    }
    return;
   }
   float size=(clip.id=="raditzSurge"?RaditzEnhancedRadius:RaditzNormalRadius)*gain;
   PaintRaditzSphere(mesh,center+Vector2.right*(size*.78f),size,time,gain);
  }

  void DrawRaditzKick(VertexHelper mesh,Vector2 at,float age){
   if(age<0||age>=150)return;
   effectScale=1;
   float fade=1-Ease(age/150);
   var center=at-new Vector2(18,-11);
   Arc(mesh,center,24+age*.08f,33+age*.07f,-1.35f,.48f,.22f,Alpha(Pale,.78f*fade),2.0f);
   Line(mesh,at-new Vector2(11,-14),at+new Vector2(12,-14),1.8f,Alpha(RaditzWhite,fade));
  }

  void DrawRaditzStance(VertexHelper mesh){
   float amount=Ease((time-165)/125)*(1-Ease((time-460)/175));
   if(amount<=0)return;
   effectScale=1;
   var center=Ground(time)+new Vector2(0,-43);
   var warm=new Color32(226,129,72,255);
   Arc(mesh,center,29+12*amount,7+13*amount,-1.25f,1.5f,-.28f,Alpha(warm,amount*.68f),1.6f);
   for(int i=0;i<7;i++){
    var point=center+new Vector2((i-3)*8,-10-(i%3)*12);
    Line(mesh,point,point-new Vector2(3,8+3*i%4),1.2f,Alpha(warm,amount*.67f));
   }
  }
 }
}
