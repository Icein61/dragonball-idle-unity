using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 internal sealed partial class RoundMotionFx {
  static readonly Color GokuBeamBlue=new Color32(32,112,250,255);
  static readonly Color GokuBeamCyan=new Color32(75,224,255,255);
  static readonly Color GokuBeamIce=new Color32(215,251,255,255);
  static readonly float[] GokuBeamBands={-1.46f,-1.04f,-.76f,-.39f,0,.39f,.76f,1.04f,1.46f};
  static readonly float[] GokuBeamAlpha={0,.70f,.96f,.99f,1,.99f,.96f,.70f,0};
  internal int BeamDissolveParticles {get;private set;}

  void DrawGokuKamehameha(VertexHelper mesh,RoundMotionArt.Beam beam){
   if(time<beam.fire){
    DrawGokuBeamCharge(mesh,beam);
    return;
   }

   var palm=Palm(beam.fire,false);
   float age=time-beam.fire;
   float reach=Mathf.Clamp01(age/beam.flight);
   float impactX=Mathf.Clamp(beamTarget?.x??beamEnd,palm.x+16,Width);
   float thrust=1-(1-reach)*(1-reach);
   float x=Mathf.Lerp(palm.x,Width+125,thrust);
   var tip=new Vector2(x,palm.y);
   float dissolve=Ease((time-beam.cut)/252);
   float density=1-dissolve;
   float radius=Mathf.Lerp(17,43,Ease(age/26));
   BeamPalm=palm;
   BeamTip=tip;
   BeamReach=reach;
   BeamRadius=radius;
   BeamOpacity=BeamTipOpacity=density;
   BeamRootDepth=radius*.46f;
   BeamTailRadius=radius;

   if(density>0){
    Glow(mesh,palm+new Vector2(10,0),radius*1.22f,radius*1.09f,
     Alpha(GokuBeamCyan,.56f*density),Alpha(GokuBeamBlue,0));
    if(x>palm.x+1){
     DrawGokuBeamBody(mesh,palm,tip,radius,density,dissolve);
     DrawGokuBeamFilaments(mesh,palm,tip,radius,density);
    }
   }

   float burst=1-Ease(age/125);
   if(burst>0){
    DrawGokuBeamShock(mesh,palm,age,burst);
    float transition=1-Ease(age/32);
    if(transition>0)Glow(mesh,palm,13,12,Alpha(Color.white,transition),Alpha(GokuBeamCyan,transition*.58f));
   }
   float contact=age-beam.flight;
   float hitFlash=1-Ease(contact/162);
   if(!dodged&&contact>=0&&hitFlash>0){
    var hit=new Vector2(impactX,palm.y);
    Glow(mesh,hit,68+contact*.31f,49+contact*.19f,Alpha(GokuBeamIce,hitFlash*.69f),Alpha(GokuBeamBlue,0));
    Arc(mesh,hit,12+contact*.36f,18+contact*.29f,-1.39f,1.39f,0,Alpha(GokuBeamIce,hitFlash*.91f),2.2f);
   }
   if(time>=beam.cut)DrawGokuBeamDissolve(mesh,palm,tip,radius,time-beam.cut,beam.extinguish-beam.cut);
  }

  void DrawGokuBeamCharge(VertexHelper mesh,RoundMotionArt.Beam beam){
   // Sample the displayed drawing so the charge never leads either hand.
   var hands=Palm(time,false);
   float build=Ease((time-beam.gather)/(beam.compress-beam.gather));
   float squeeze=Ease((time-beam.compress)/(beam.push-beam.compress));
   float flare=Ease((time-beam.push)/(beam.fire-beam.push));
   float light=Ease((time-beam.gather)/162);
   float sceneSize=Mathf.Lerp(1,2.15f,Mathf.InverseLerp(.19f,Scale,poseScale));
   float radius=(Mathf.Lerp(Mathf.Lerp(3,11.4f,build),8.2f,squeeze)+3.7f*flare)*sceneSize;
   BeamPalm=hands;
   BeamChargeRadius=radius;

   Glow(mesh,hands,radius*2.32f+2,radius*2.17f+2,Alpha(GokuBeamBlue,light*.73f),Alpha(GokuBeamBlue,0));
   Glow(mesh,hands,radius,radius*.96f,Alpha(new Color32(115,223,255,255),light),Alpha(GokuBeamBlue,light*.85f));
   Glow(mesh,hands+new Vector2(-radius*.16f,-radius*.1f),radius*.68f,radius*.63f,
    Alpha(Color.white,light),Alpha(GokuBeamCyan,.68f*light));
   for(int i=0;i<3;i++){
    float arc=i*.83f+time*.0055f;
    Arc(mesh,hands,radius*(1.09f+i*.17f)+3,radius*(.64f+i*.17f)+3,arc,arc+1.11f,i*.49f,
     Alpha(i%2==0?GokuBeamIce:GokuBeamCyan,light*(.81f-.115f*i)),1.1f+1.3f*flare);
   }
   for(int i=0;i<14;i++){
    float angle=i*(Mathf.PI*2/14)+time*.0017f;
    float stage=Mathf.Repeat((time-beam.gather)*.0038f+i*.381966f,1);
    var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    float space=radius+3+(36-12*squeeze)*(1-stage)*(1-stage);
    float length=(2+i%5*1.3f)*(1+build*.45f+flare*.45f);
    var point=hands+direction*space;
    Line(mesh,point,point+direction*length,1.15f+1.15f*squeeze,
     Alpha(i%3==0?Color.white:GokuBeamCyan,light*Mathf.Sin(stage*Mathf.PI)*.88f));
   }
   for(int i=0;i<6;i++){
    float a=i*(Mathf.PI*2/6)+.2f;
    var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
    DrawGokuRay(mesh,hands,direction,radius*(1.07f+.65f*flare)+(i%3)*2.4f*flare,
     radius*.17f,Alpha(GokuBeamIce,light*(.36f+.62f*flare)));
   }
  }

  void DrawGokuBeamBody(VertexHelper mesh,Vector2 palm,Vector2 tip,float radius,float density,float dissolve){
   const int sections=52;
   int stride=GokuBeamBands.Length;
   int first=mesh.currentVertCount;
   float span=tip.x-palm.x;
   for(int i=0;i<=sections;i++){
    float u=i/(float)sections;
    float x=palm.x+span*u;
    float mouth=Mathf.Lerp(.32f,1,Ease((x-palm.x)/34));
    float ripples=1+.027f*Mathf.Sin(x*.071f-time*.032f)+.018f*Mathf.Sin(x*.171f+time*.048f);
    float head=Mathf.Sqrt(1-.92f*Ease((u-.97f)/.03f));
    float uneven=.5f+.5f*Mathf.Sin(u*87+time*.012f);
    float width=radius*mouth*ripples*head*(1-dissolve*Mathf.Lerp(.36f,.85f,uneven));
    float center=palm.y+Mathf.Sin(x*.031f-time*.029f)*width*.028f*Ease(u/.12f);
    float opacity=density*(1-Ease((u-.971f)/.029f))*Mathf.Lerp(1,1-.84f*uneven,dissolve);
    for(int strip=0;strip<stride;strip++){
     int band=Mathf.Abs(strip-4);
     Color tint=band>=3?GokuBeamBlue:band==2?GokuBeamCyan:band==1?GokuBeamIce:Color.white;
     Vertex(mesh,new Vector2(x,center+GokuBeamBands[strip]*width),Alpha(tint,opacity*GokuBeamAlpha[strip]));
     if(i==0||strip==0)continue;
     int prev=first+(i-1)*stride+strip-1;
     mesh.AddTriangle(prev,prev+1,prev+stride);
     mesh.AddTriangle(prev+1,prev+stride+1,prev+stride);
    }
   }
  }

  void DrawGokuBeamFilaments(VertexHelper mesh,Vector2 palm,Vector2 tip,float radius,float density){
   float span=tip.x-palm.x;
   for(int i=0;i<33;i++){
    float u=Mathf.Repeat((i+1)*.618034f+time*(.0019f+(i%4)*.00033f),1);
    float side=(i%2==0?1:-1)*(.27f+i%7*.113f);
    float distance=Mathf.Min(tip.x,palm.x+span*u+28+11*(i%6));
    float rim=Mathf.Lerp(.4f,1,Ease(span*u/34));
    var start=new Vector2(palm.x+span*u,palm.y+side*radius*rim);
    var end=new Vector2(distance,palm.y+side*radius*rim*.91f);
    float opacity=density*(1-Ease((u-.87f)/.13f))*(i%3==0?.69f:.36f);
    Line(mesh,start,end,i%4==0?2.1f:.9f,Alpha(i%3==0?GokuBeamBlue:Color.white,opacity));
   }
  }

  void DrawGokuBeamShock(VertexHelper mesh,Vector2 palm,float age,float opacity){
   Glow(mesh,palm+new Vector2(16,0),80+age*.24f,58+age*.17f,
    Alpha(Color.white,opacity*.95f),Alpha(GokuBeamCyan,0));
   var ring=palm+new Vector2(16+age*.92f,0);
   Arc(mesh,ring,11+age*.18f,21+age*.48f,0,Mathf.PI*2,0,Alpha(GokuBeamIce,opacity*.8f),2.65f);
   for(int i=0;i<14;i++){
    float angle=i*(Mathf.PI*2/14)+.17f;
    var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    DrawGokuRay(mesh,palm+direction*(13+age*.17f),direction,21+i%5*11+age*.29f,
     2+i%4*.82f,Alpha(i%3==0?GokuBeamCyan:GokuBeamIce,opacity*.90f));
   }
  }

  void DrawGokuRay(VertexHelper mesh,Vector2 origin,Vector2 direction,float length,float half,Color tint){
   var side=new Vector2(-direction.y,direction.x);
   int root=mesh.currentVertCount;
   Vertex(mesh,origin+side*half,tint);
   Vertex(mesh,origin-side*half,tint);
   Vertex(mesh,origin+direction*length,Alpha(tint,tint.a*.18f));
   mesh.AddTriangle(root,root+1,root+2);
  }

  void DrawGokuBeamDissolve(VertexHelper mesh,Vector2 palm,Vector2 tip,float radius,float elapsed,float duration){
   for(int i=0;i<116;i++){
    float u=Mathf.Repeat((i+1)*.618034f,1);
    float seed=Mathf.Repeat((i+1)*.414214f,1);
    float age=elapsed-(1-u)*142-seed*46;
    if(age<0)continue;
    float life=duration*(.53f+seed*.28f);
    if(age>=life)continue;
    float fade=(1-Ease(age/life))*Ease(age/49);
    float side=i%2==0?1:-1;
    float spread=radius*(.13f+seed*.69f)*Mathf.Lerp(.54f,1,Ease(u/.17f));
    var spot=new Vector2(Mathf.Lerp(palm.x,tip.x,u*.94f)+age*(.032f+seed*.14f),
     palm.y+side*(spread+age*(.011f+seed*.086f))-age*.015f);
    float size=(1.3f+seed*4.2f)*(1-Ease(age/life)*.64f);
    var direction=new Vector2(1,side*(seed-.2f)).normalized;
    Color tint=i%4==0?GokuBeamBlue:i%3==0?Color.white:GokuBeamCyan;
    DrawGokuRay(mesh,spot,direction,size*(2.2f+i%5*.47f),size*.48f,Alpha(tint,fade*.94f));
    if(i%4==1)Glow(mesh,spot,size*1.48f,size*1.37f,Alpha(GokuBeamIce,fade*.74f),Alpha(GokuBeamCyan,0));
    BeamDissolveParticles++;
   }
  }
 }
}
