using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 // The focus view uses the same right-facing ground coordinates as the approved preview.
 [RequireComponent(typeof(CanvasRenderer))]
 internal sealed partial class RoundMotionFx : MaskableGraphic {
  internal const float Width = 960, Height = 404, Floor = 333, Origin = 282, Scale = .60f;
  static readonly Color Warm = new Color32(210, 145, 65, 255);
  static readonly Color Pale = new Color32(255, 242, 184, 255);
  static readonly Color Cyan = new Color32(86, 222, 252, 255);
  static readonly Color Blue = new Color32(25, 134, 204, 255);
  static readonly Color PiccoloGold=new Color32(255,181,76,255);
  static readonly Color PiccoloViolet=new Color32(207,89,237,255);
  RoundMotionArt art;
  RoundMotionArt.Clip clip;
  float time, poseScale, beamEnd;
  Vector2 travelPerUnit;
  Vector2? beamTarget,visibleRightFingertips;
  internal struct VolleyOrb {
   internal Vector2 Start,End,Ground;
   internal float Elapsed,Flight;
   internal bool Miss,Scatter,Raditz,TwinRay,Enhanced,Acid;
   internal int Index;
  }
  IReadOnlyList<VolleyOrb> volley;
  internal struct DiscCut {internal Vector2 Center;internal float Age;}
  IReadOnlyList<DiscCut> discCuts;
  internal void PresentDiscCuts(IReadOnlyList<DiscCut> cuts){
   discCuts=cuts;
   SetVerticesDirty();
  }
  internal int VisibleVolleyOrbs {get;private set;}
  internal int VisibleVolleyBursts {get;private set;}
  internal void PresentVolley(IReadOnlyList<VolleyOrb> orbs){
   volley=orbs;
   SetVerticesDirty();
  }
  internal struct GuardPulse {internal Vector2 Center;internal float Age;internal bool Barrier,PiccoloSurge;}
  IReadOnlyList<GuardPulse> guards;
  internal float CounterWidth {get;private set;}
  internal void PresentGuard(IReadOnlyList<GuardPulse> pulses){
   guards=pulses;
   SetVerticesDirty();
  }

  internal struct ChargePulse {internal Vector2 Center;internal float Phase;}
  IReadOnlyList<ChargePulse> heldCharges;
  internal int VisibleHeldCharges {get;private set;}
  internal void PresentHeldCharges(IReadOnlyList<ChargePulse> pulses){
   heldCharges=pulses;
   SetVerticesDirty();
  }

  Vector2 effectPivot;
  float effectScale;
  Vector2 stageOrigin;
  bool mirror, dodged, behind;
  internal int GeometryCount { get; private set; }
  internal float BeamOpacity { get; private set; }
  internal float BeamReach { get; private set; }
  internal float BeamRadius { get; private set; }
  internal float BeamChargeRadius { get; private set; }
  internal float BeamTipOpacity { get; private set; }
  internal float BeamTailRadius { get; private set; }
  internal Vector2 BeamPalm { get; private set; }
  internal Vector2 BeamTip { get; private set; }
  internal float BeamRootDepth { get; private set; }

  internal void Present(RoundMotionArt library, RoundMotionArt.Clip track, float at, bool flip, bool missed, bool rear = false, Vector2? origin = null, float end = Width,
   Vector2? travel = null, float scale = Scale, Vector2? target = null, Vector2? rightFingertips = null) {
   art = library;
   clip = track;
   time = at;
   mirror = flip;
   dodged = missed;
   behind = rear;
   stageOrigin = origin ?? new Vector2(Origin, Floor);
   travelPerUnit = travel ?? Vector2.right;
   poseScale = scale;
   beamEnd = end;
   beamTarget = target;
   visibleRightFingertips=rightFingertips;
   volley=null;
   discCuts=null;
   guards=null;
   heldCharges=null;
   SetVerticesDirty();
  }

  protected override void OnPopulateMesh(VertexHelper mesh) {
   mesh.Clear();
   BeamOpacity=BeamReach=BeamRadius=BeamTipOpacity=BeamChargeRadius=BeamRootDepth=BeamTailRadius=0;
   effectScale=1;
   CounterWidth=0;
   RaditzSphereRadius=0;
   RaditzBeamBranches=0;
   BeamDissolveParticles=0;
   KrillinDiscRadius=0;
   KrillinDiscTrailParticles=0;
   if(clip!=null){
    if(behind){
     ScaleLocalEffect(Ground(time));
     if(clip.id=="tienCannon")DrawTienCharge(mesh);
     else if(clip.id=="yamchaRise"){
      if(clip.fxStyle=="Yamcha")DrawYamchaFeignGlow(mesh);
      else DrawYamchaRise(mesh);
     }
     else if(clip.id=="gohanRally")DrawGohanRally(mesh);
     else if(clip.id=="gohanHeavy")DrawGohanHeavyCharge(mesh);
     else if(clip.fx=="raditzAura"&&clip.fxStyle=="Raditz")DrawRaditzStance(mesh);
     else{DrawHalo(mesh);if(clip.fx=="krillinMuster")DrawKrillinOpening(mesh); }
    }else{
     var beam=clip.beam;
     if(clip.id=="piccoloCharge")
      BeamChargeRadius=clip.fxStyle=="Piccolo"?
       DrawPiccoloBrowEnergy(mesh,PiccoloFingertips(time),Ease((time-42)/402),time):
       DrawPiccoloCharge(mesh,Palm(time,false),.86f*Mathf.InverseLerp(50,560,time),time);
     if(clip.id=="piccoloCounter"){
      if(clip.fxStyle=="Piccolo")DrawPiccoloCounterSurge(mesh);
      else DrawPiccoloCounter(mesh);
     }
     if(clip.id=="krillinDisc"){
      if(clip.fxStyle=="Krillin")DrawKrillinDiscRevised(mesh);
      else DrawKrillinDisc(mesh);
     }
     if(clip.id=="tienCannon")DrawTienCannon(mesh);
     if(clip.id=="tienFinger")DrawTienFinger(mesh);
     if(clip.fx=="saibaDetonate")DrawSaibamanDetonation(mesh);
     if(clip.fx=="raditzKi"&&time>=110)DrawRaditzCharge(mesh);
     if(beam!=null&&beam.gather>0&&time<beam.extinguish){
      if(clip.id=="specialBeam"){
       if(clip.fxStyle=="Piccolo")DrawPiccoloCannon(mesh,beam);
       else DrawPiccoloBeam(mesh,beam);
      }
      else if(time>=beam.gather){
       if(clip.id=="ultimate"&&clip.fxStyle=="Goku")DrawGokuKamehameha(mesh,beam);
       else DrawBeam(mesh,beam);
      }
     }
     if(!string.IsNullOrEmpty(clip.fxStyle)&&clip.id!="piccoloCharge"&&clip.id!="specialBeam")
      DrawReworkedRelease(mesh);
     foreach(var cue in clip.impacts){
      var pose=art.Atlas[cue.drawing];
      var point=RoundMotionArt.Anchor(pose,pose.hit,Ground(cue.at),poseScale);
      ScaleLocalEffect(point);
      float age=time-cue.at;
      if(age<0&&age>=-74&&string.IsNullOrEmpty(clip.fxStyle))DrawWindup(mesh,point,cue.kind,1+age/74);
      if(clip.id=="yamchaWolf"&&clip.fxStyle=="Yamcha"){
       if(age>=-42&&age<325)DrawYamchaClaw(mesh,point,age,cue.at==clip.impactAt,cue.drawing%2);
       continue;
      }
      if(!dodged&&age>=0&&age<355){
       if(clip.id=="raditzKick"&&clip.fxStyle=="Raditz")DrawRaditzKick(mesh,point,age);
       else if(string.IsNullOrEmpty(clip.fxStyle))DrawImpact(mesh,point,cue.kind,age);
       else DrawReworkedStrike(mesh,point,cue.kind,age);
       if(clip.id=="gohanFlash")DrawGohanFlash(mesh,point,age);
       if(cue.kind=="wolf")DrawYamchaWolf(mesh,point,age);
      }
     }
    }
   }
   if(guards!=null){
    effectScale=1;
    foreach(var guard in guards){
     if(guard.Barrier)DrawGohanBarrier(mesh,guard.Center,guard.Age);
     else if(guard.PiccoloSurge)DrawPiccoloCounterShield(mesh,guard.Center,guard.Age);
     else DrawPiccoloGuard(mesh,guard.Center,guard.Age);
    }
   }
   VisibleHeldCharges=heldCharges==null?0:heldCharges.Count;
   if(heldCharges!=null)
    foreach(var charge in heldCharges)DrawPiccoloCharge(mesh,charge.Center,.86f,charge.Phase);
   VisibleVolleyOrbs=volley==null?0:volley.Count;
   VisibleVolleyBursts=0;
   if(volley!=null){
    effectScale=1;
    foreach(var orb in volley)DrawVolleyOrb(mesh,orb);
   }
   if(discCuts!=null){
    effectScale=1;
    foreach(var cut in discCuts)DrawKrillinDiscCut(mesh,cut);
   }
   GeometryCount=mesh.currentVertCount;
  }

  void DrawVolleyOrb(VertexHelper mesh,VolleyOrb orb){
   if(orb.Acid){DrawSaibamanAcid(mesh,orb);return;}
   if(orb.Scatter){DrawKrillinScatter(mesh,orb);return;}
   if(orb.TwinRay){DrawRaditzTwinBeam(mesh,orb);return;}
   if(orb.Raditz){DrawRaditzVolatileOrb(mesh,orb);return;}
   var warm=new Color32(255,199,70,255);
   var flare=new Color32(255,249,197,255);
   float age=orb.Elapsed-orb.Flight;
   if(age>=0){
    float opacity=1-Mathf.Clamp01(age/185);
    float radius=(orb.Enhanced?16:8)+age*.11f;
    Glow(mesh,orb.End,radius,radius*.78f,Alpha(warm,orb.Miss?.14f*opacity:.68f*opacity),Alpha(warm,0));
    if(!orb.Miss){
     Glow(mesh,orb.End,radius*.54f,radius*.48f,Alpha(flare,opacity),Alpha(warm,0));
     DrawVolleyImpact(mesh,orb,warm,flare,age);
    }
    return;
   }
   float phase=Mathf.Clamp01(orb.Elapsed/orb.Flight);
   var direction=(orb.End-orb.Start).normalized;
   var at=Vector2.Lerp(orb.Start,orb.End,phase)+Vector2.up*(-Mathf.Sin(phase*Mathf.PI)*13);
   float strength=orb.Enhanced?1.65f:1;
   float size=strength*1.20f;
   Glow(mesh,at-direction*4,20*size,15*size,Alpha(warm,.78f),Alpha(warm,0));
   Glow(mesh,at,9*size,8*size,flare,Alpha(warm,.46f));
   Line(mesh,at-direction*(8+phase*22)*strength,at-direction*3,3.5f*strength,Alpha(warm,.70f));
  }

  void DrawSaibamanAcid(VertexHelper mesh,VolleyOrb orb){
   var rich=new Color32(91,143,24,255);
   var green=new Color32(172,211,53,255);
   var yellow=new Color32(242,253,124,255);
   float age=orb.Elapsed-orb.Flight;
   if(age<0){
    float head=Mathf.Clamp01(orb.Elapsed/orb.Flight);
    float trail=Mathf.Max(0,head-.39f);
    Vector2 At(float fraction){
     var path=Vector2.Lerp(orb.Start,orb.End,fraction);
     return path+new Vector2(0,-Mathf.Sin(Mathf.PI*fraction)*(28+orb.Index*7));
    }
    for(int i=0;i<7;i++){
     float p=i/7f;
     var a=At(Mathf.Lerp(trail,head,p));
     var b=At(Mathf.Lerp(trail,head,(i+1)/7f));
     float width=Mathf.Lerp(3,24,p)*Mathf.Lerp(.6f,1,head);
     Line(mesh,a,b,width,Alpha(rich,.93f));
     Line(mesh,a+new Vector2(0,-1),b+new Vector2(0,-1),width*.74f,Alpha(green,.94f));
     Line(mesh,a+new Vector2(0,-2),b+new Vector2(0,-2),width*.24f,Alpha(yellow,.93f));
    }
    var tip=At(head);
    Glow(mesh,tip,18,12,Alpha(yellow,.94f),Alpha(green,0));
    for(int i=0;i<4;i++){
     var tear=At(Mathf.Lerp(trail,head,.30f+i*.15f))+new Vector2(-i*4,(i%2==0?-1:1)*(8+i*4));
     Glow(mesh,tear,5.5f,4,Alpha(green,.88f),Alpha(yellow,0));
    }
    return;
   }
   if(orb.Miss||age>=670)return;
   float fade=1-Ease(Mathf.InverseLerp(265,670,age));
   float splash=1-Ease(age/245);
   var point=orb.End;
   var feet=orb.Ground;
   if(splash>0){
    Glow(mesh,point,44+age*.052f,35+age*.045f,Alpha(yellow,splash*.87f),Alpha(green,0));
    for(int i=0;i<9;i++){
     float angle=i*.73f;
     var ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
     var from=point+ray*(10+age*.018f);
     var to=from+ray*(21+age*.076f);
     Line(mesh,from,to,2.5f+i%3,Alpha(i%3==0?yellow:green,splash));
    }
   }
   Glow(mesh,point+new Vector2(0,9),24,29,Alpha(green,.58f*fade),Alpha(rich,0));
   Arc(mesh,feet,34+age*.019f,9,0,Mathf.PI*2,.04f,Alpha(green,.72f*fade),3.4f);
   for(int i=0;i<5;i++){
    float fall=Mathf.Clamp01((age-i*35)/335);
    var drip=point+new Vector2((i-2)*8,-13+fall*(26+(i%2)*10));
    Line(mesh,drip-new Vector2(1,8+4*fall),drip,4.1f,Alpha(rich,.75f*fade));
    Glow(mesh,drip,3.6f,5,Alpha(yellow,.84f*fade),Alpha(green,0));
    var vapor=point+new Vector2((i-2)*9+Mathf.Sin(i+age*.012f)*7,-14-age*.065f);
    Glow(mesh,vapor,6+i%2*2,10,Alpha(new Color32(234,249,203,255),.23f*fade),Alpha(yellow,0));
   }
  }

  void DrawSaibamanDetonation(VertexHelper mesh){
   var orange=new Color32(255,157,43,255);
   var white=new Color32(255,251,232,255);
   float age=time-clip.impactAt;
   effectScale=1;
   if(age<0){
    float thrust=time-RoundMotionArt.SaibamanPushOff;
    float touchdown=time-RoundMotionArt.SaibamanLanding;
    if(thrust>=0&&thrust<238)DrawSaibaDust(mesh,stageOrigin,thrust,orange);
    if(touchdown>=0&&touchdown<215)DrawSaibaDust(mesh,Ground(RoundMotionArt.SaibamanLanding),touchdown,orange);
    if(time>=RoundMotionArt.SaibamanClasp+40){
     var center=(beamTarget??Ground(time))+new Vector2(0,-7);
     float pulse=Mathf.InverseLerp(RoundMotionArt.SaibamanClasp+40,clip.impactAt,time);
     Glow(mesh,center,18+14*pulse,16+11*pulse,Alpha(white,pulse*.68f),Alpha(orange,0));
     Arc(mesh,center,20,16,time*.019f,time*.019f+3.5f,0,Alpha(orange,pulse*.86f),2.8f);
    }
    return;
   }
   if(age>=460)return;
   var origin=(beamTarget??Ground(time))+new Vector2(0,-21);
   float spread=1-Mathf.Exp(-age/95);
   float opacity=1-Ease(age/460);
   Glow(mesh,origin,104+68*spread,81+35*spread,Alpha(white,.98f*opacity),Alpha(orange,0));
   Arc(mesh,origin,35+174*spread,20+77*spread,0,Mathf.PI*2,0,Alpha(white,opacity*.93f),6.5f);
   for(int ray=0;ray<14;ray++){
    var direction=new Vector2(Mathf.Cos(ray*.449f),Mathf.Sin(ray*.449f));
    Line(mesh,origin+direction*(21+39*spread),origin+direction*(54+96*spread),2+ray%3,Alpha(orange,opacity));
   }
  }

  void DrawSaibaDust(VertexHelper mesh,Vector2 feet,float age,Color tint){
   float fade=1-Ease(age/238);
   for(int i=0;i<4;i++){
    var puff=feet+new Vector2((i-1.9f)*(4+age*.091f),-5-age*.037f);
    Glow(mesh,puff,10+age*.027f,5+age*.021f,Alpha(tint,.48f*fade),Alpha(tint,0));
   }
  }

  static readonly Color DiscGold=new Color32(255,181,29,255);
  static readonly Color DiscWhite=new Color32(255,252,217,255);

  void DrawKrillinOpening(VertexHelper mesh){
   float fade=Ease((time-240)/130)*(1-Ease((time-610)/280));
   if(fade<=0)return;
   var center=Ground(time)+new Vector2(0,-41);
   for(int i=0;i<3;i++){
    float grow=Mathf.InverseLerp(255+i*52,660+i*52,time);
    float a=fade*(1-grow);
    Arc(mesh,center,30+grow*103,9+grow*21,-.28f,Mathf.PI*1.7f,-.1f+i*.11f,Alpha(DiscGold,a*.78f),2.2f);
   }
   for(int i=0;i<10;i++){
    float angle=i*.628f+.14f;
    var ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    float reach=24+Mathf.Repeat(time+i*81,370)*.13f;
    Line(mesh,center+ray*reach,center+ray*(reach+13),1.4f,Alpha(DiscWhite,fade*.75f));
   }
  }

  void DrawKrillinDisc(VertexHelper mesh){
   if(time<264||time>=clip.impactAt+130)return;
   float launch=clip.launchAt;
   float charge=Ease(Mathf.InverseLerp(264,launch-60,time));
   bool flying=time>=launch;
   float flight=Mathf.InverseLerp(launch,clip.impactAt,time);
   var start=Palm(launch,false)+new Vector2(0,-10);
   var target=beamTarget??new Vector2(beamEnd,188);
   var point=flying?Vector2.Lerp(start,target,flight)+new Vector2(0,-13*Mathf.Sin(flight*Mathf.PI)):
    Palm(time,false)+new Vector2(0,-12-charge*9);
   float opacity=(flying?1:Mathf.Clamp01((time-264)/120))*(1-Ease((time-clip.impactAt)/130));
   float rx=flying?40+flight*15:13+charge*37;
   float ry=flying?5.5f+flight*2:4+charge*4;
   float turn=flying?Mathf.Lerp(-.19f,.10f,flight):-.08f;
   if(flying){
    var direction=(target-start).normalized;
    for(int i=0;i<5;i++){
     float offset=(i-2)*3.4f;
     var line=point+new Vector2(0,offset);
     Line(mesh,line-direction*(35+flight*36+i*3),line-direction*(6+i*2),i==2?3.2f:1.1f,
      Alpha(i==2?DiscWhite:DiscGold,opacity*(i==2?.82f:.46f)));
    }
   }
   Glow(mesh,point,rx*1.26f,ry*2.8f,Alpha(DiscGold,.7f*opacity),Alpha(DiscGold,0));
   Glow(mesh,point,rx*.92f,ry*.95f,Alpha(DiscWhite,.90f*opacity),Alpha(DiscGold,.64f*opacity));
   Arc(mesh,point,rx,ry,0,Mathf.PI*2,turn,Alpha(DiscGold,opacity),2.8f);
   Arc(mesh,point,rx-2,ry*.82f,0,Mathf.PI*2,turn,Alpha(DiscWhite,opacity),1.65f);
   float spin=time*.037f;
   for(int i=0;i<3;i++){
    float phase=spin+i*2.094f;
    Arc(mesh,point,rx+4+i*1.3f,ry+2,phase,phase+.78f,turn,Alpha(DiscWhite,opacity*.86f),1.45f);
   }
  }

  void DrawKrillinDiscCut(VertexHelper mesh,DiscCut cut){
   float fade=1-Ease(cut.Age/282);
   var point=cut.Center;
   float span=21+cut.Age*.16f;
   Glow(mesh,point,span,9,Alpha(DiscGold,.77f*fade),Alpha(DiscGold,0));
   Line(mesh,point+new Vector2(-span,11),point+new Vector2(span,-11),3.3f,Alpha(DiscWhite,fade));
   for(int i=0;i<7;i++){
    var ray=Oval(1,1,i*.9f-.23f,0);
    float drift=8+cut.Age*(.10f+(i%3)*.035f);
    Line(mesh,point+ray*drift,point+ray*(drift+6+i%4),1.3f,Alpha(DiscGold,fade*.85f));
   }
  }

  void DrawKrillinScatter(VertexHelper mesh,VolleyOrb orb){
   float age=orb.Elapsed-orb.Flight;
   if(age>=0){
    float fade=1-Ease(age/180);
    if(orb.Miss){
     Arc(mesh,orb.End,13+age*.04f,7+age*.03f,-1,1.8f,0,Alpha(DiscGold,fade*.42f),1.2f);
     return;
    }
    DrawVolleyImpact(mesh,orb,DiscGold,DiscWhite,age);
    Glow(mesh,orb.End,9+age*.07f,7+age*.06f,Alpha(DiscGold,.72f*fade),Alpha(DiscGold,0));
    for(int i=0;i<5;i++){
     var ray=Oval(1,1,i*1.25f+orb.Index*.7f,0);
     float reach=2+age*.11f;
     Line(mesh,orb.End+ray*reach,orb.End+ray*(reach+7),1.35f,Alpha(DiscWhite,fade));
    }
    return;
   }
   float phase=Mathf.Clamp01(orb.Elapsed/orb.Flight);
   if(clip?.fxStyle=="Krillin"&&orb.Elapsed<58){
    float flash=1-Ease(orb.Elapsed/58f);
    Glow(mesh,orb.Start,17+orb.Elapsed*.09f,11+orb.Elapsed*.06f,
     Alpha(DiscWhite,flash*.92f),Alpha(DiscGold,0));
   }
   float bend=10+orb.Index%3*7;
   var at=Vector2.Lerp(orb.Start,orb.End,phase)+new Vector2(0,-Mathf.Sin(phase*Mathf.PI)*bend);
   var direction=(orb.End-orb.Start).normalized;
   float grow=Ease(phase/.3f);
   Glow(mesh,at,15,11,Alpha(DiscGold,.9f),Alpha(DiscGold,0));
   Glow(mesh,at+direction*2,6.5f,5,DiscWhite,Alpha(DiscGold,.46f));
   Line(mesh,at-direction*(10+grow*22),at-direction*3,1.85f,Alpha(DiscGold,.72f));
  }

  void DrawVolleyImpact(VertexHelper mesh,VolleyOrb orb,Color rim,Color core,float age){
   VisibleVolleyBursts++;
   float blast=1-Ease(age/115);
   float lift=Ease(age/185);
   float radius=8+age*.105f;
   Glow(mesh,orb.End,radius*1.38f,radius,Alpha(core,blast*.83f),Alpha(rim,0));
   for(int i=0;i<6;i++){
    var ray=Oval(1,1,i*Mathf.PI/3+orb.Index*.43f,0);
    Line(mesh,orb.End+ray*radius,orb.End+ray*(radius+4+9*blast),1.7f,Alpha(rim,blast*.88f));
   }
   var dust=new Color32(213,188,146,255);
   for(int i=0;i<5;i++){
    float spread=(i-2)*(5+9*lift);
    var center=orb.Ground+new Vector2(spread,-2-lift*(5+((i+orb.Index)%3)*3));
    Glow(mesh,center,7+8*lift,3+4*lift,Alpha(dust,.46f*(1-lift)),Alpha(dust,0));
   }
  }

  Vector2 Ground(float at) => stageOrigin + clip.PlanarShift(at,travelPerUnit);
  static float Ease(float n) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(n));
  static Color Alpha(Color c, float a) { c.a = a; return c; }

  void ScaleLocalEffect(Vector2 pivot) {
   effectPivot=pivot;
   effectScale=Mathf.Clamp(poseScale/Scale,.52f,1);
  }

  void Vertex(VertexHelper mesh, Vector2 p, Color c) {
   p=effectPivot+(p-effectPivot)*effectScale;
   mesh.AddVert(new Vector3(mirror ? Width - p.x : p.x, -p.y, 0), c, Vector2.zero);
  }

  void Line(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color tint) {
   var delta = end - start;
   if (delta.sqrMagnitude < .001f) return;
   var normal = new Vector2(-delta.y, delta.x).normalized * (width * .5f);
   int i = mesh.currentVertCount;
   Vertex(mesh, start - normal, tint);
   Vertex(mesh, start + normal, tint);
   Vertex(mesh, end + normal, tint);
   Vertex(mesh, end - normal, tint);
   mesh.AddTriangle(i, i+1, i+2);
   mesh.AddTriangle(i+2, i+3, i);
  }

  void Glow(VertexHelper mesh, Vector2 p, float radius, float ry, Color inside, Color edge) {
   const int sides = 24;
   int i = mesh.currentVertCount;
   Vertex(mesh, p, inside);
   for (int n = 0; n <= sides; n++) {
    float a = n * (2 * Mathf.PI / sides);
    Vertex(mesh, p + new Vector2(radius * Mathf.Cos(a), ry * Mathf.Sin(a)), edge);
    if (n > 0) mesh.AddTriangle(i, i+n, i+n+1);
   }
  }

  void Arc(VertexHelper mesh, Vector2 p, float rx, float ry, float begin, float end, float turn, Color color, float width) {
   const int segments = 20;
   Vector2 last = p + Oval(rx, ry, begin, turn);
   for (int n = 1; n <= segments; n++) {
    var next = p + Oval(rx, ry, Mathf.Lerp(begin, end, n / (float)segments), turn);
    Line(mesh, last, next, width, color);
    last = next;
   }
  }

  static Vector2 Oval(float rx, float ry, float angle, float turn) {
   float x = rx * Mathf.Cos(angle), y = ry * Mathf.Sin(angle);
   return new Vector2(x * Mathf.Cos(turn) - y * Mathf.Sin(turn), x * Mathf.Sin(turn) + y * Mathf.Cos(turn));
  }

  void DrawHalo(VertexHelper mesh) {
   if (clip.haloTo == 0 || time < clip.haloFrom || time >= clip.haloTo) return;
   float age = time - clip.haloFrom;
   float a = Ease(age / 240) * Ease((clip.haloTo - time) / 340);
   var center = Ground(time);
   Arc(mesh, center, 69 + age * .038f, 8 + age * .009f, 0, 2 * Mathf.PI, 0, Alpha(Warm, a * .65f), 2);
   for (int n = 0; n < 7; n++) {
    float lift = Mathf.Repeat(age + n * 90, 710) / 710;
    var p = center + new Vector2(n * 27 % 142 - 71 + Mathf.Sin(lift * 5 + n) * 6, -lift * 91);
    Glow(mesh, p, 2, 2, Alpha(Pale, a), Alpha(Warm, 0));
   }
  }

  void DrawTienCharge(VertexHelper mesh){
   float rise=Ease(Mathf.InverseLerp(200,clip.launchAt,time));
   if(rise<=0||time>=clip.impactAt)return;
   var point=Palm(time,false);
   for(int i=0;i<3;i++){
    float angle=time*.013f+i*2*Mathf.PI/3;
    Arc(mesh,point,42+18*rise,26+15*rise,angle,angle+1.56f,-.24f,Alpha(Pale,rise*.75f),2.7f);
   }
   Glow(mesh,point,26+20*rise,22+12*rise,Alpha(Warm,rise*.32f),Alpha(Warm,0));
  }

  void DrawTienCannon(VertexHelper mesh){
   float age=time-clip.impactAt;
   if(time<clip.launchAt||age>=440)return;
   var source=Palm(clip.impactAt,false);
   var end=beamTarget??new Vector2(beamEnd,Floor);
   var point=end-new Vector2(96,3);
   // The triangular release widens into a ground burst along the target row.
   float fire=1-Ease(Mathf.Max(0,age)/300);
   float flight=Ease(Mathf.InverseLerp(clip.launchAt,clip.impactAt,time));
   var leading=Vector2.Lerp(source,point,flight);
   if(fire>0){
    Glow(mesh,source,20,16,Alpha(Color.white,fire),Alpha(Warm,0));
    for(int i=-1;i<=1;i++){
     var edge=leading+new Vector2(i*65*flight,Mathf.Abs(i)*13*flight);
     Line(mesh,source,edge,(i==0?22:6)*Mathf.Max(.12f,fire),Alpha(i==0?Pale:Warm,fire*.88f));
    }
   }
   if(age<0||dodged)return;
   float wave=1-Ease(age/440);
   Glow(mesh,point,87+age*.26f,15+age*.035f,Alpha(Pale,wave*.74f),Alpha(Warm,0));
   Arc(mesh,point,68+age*.24f,11+age*.035f,0,Mathf.PI*2,0,Alpha(Warm,wave*.84f),3.5f);
   for(int i=-3;i<=3;i++){
    var from=point+new Vector2(i*35,0);
    Line(mesh,from+new Vector2(0,-22-age*.08f),from,2.5f,Alpha(Pale,wave*.61f));
   }
  }

  void DrawTienFinger(VertexHelper mesh){
   float age=time-clip.impactAt;
   if(time<clip.launchAt||age>=185)return;
   var source=Palm(clip.launchAt,false);
   var end=beamTarget??new Vector2(beamEnd,Floor-114);
   if(age<0){
    float progress=Mathf.InverseLerp(clip.launchAt,clip.impactAt,time);
    var head=Vector2.Lerp(source,end,progress);
    var trail=Vector2.Lerp(source,end,Mathf.Max(0,progress-.36f));
    Line(mesh,trail,head,10,Alpha(Warm,.71f));
    Line(mesh,trail,head,3.6f,Pale);
    Glow(mesh,head,12,8,Color.white,Alpha(Warm,0));
   }else if(!dodged){
    float strength=1-Ease(age/185);
    Glow(mesh,end,12+age*.10f,11+age*.08f,Alpha(Pale,strength),Alpha(Warm,0));
   }
  }

  void DrawYamchaRise(VertexHelper mesh){
   float strength=Ease(Mathf.InverseLerp(190,460,time))*(1-Ease(Mathf.InverseLerp(610,985,time)));
   if(strength<=0)return;
   var ground=Ground(time);
   Color green=new Color32(123,213,136,255),cream=new Color32(240,254,196,255);
   float spread=22+26*Ease(Mathf.InverseLerp(260,870,time));
   Glow(mesh,ground+new Vector2(0,-19),spread,32,Alpha(green,strength*.25f),Alpha(green,0));
   Arc(mesh,ground,spread,7,0,Mathf.PI*2,0,Alpha(cream,strength*.87f),2.4f);
   for(int i=0;i<12;i++){
    float drift=Mathf.Repeat(time/410+i*.279f,1);
    var p=ground+new Vector2(Mathf.Sin(i*2.38f)*spread*(.58f+.30f*drift),-drift*95);
    Line(mesh,p,p+new Vector2(0,-6-12*drift),2.1f,Alpha(i%3==0?cream:green,strength*(1-drift)));
   }
  }

  void DrawYamchaWolf(VertexHelper mesh,Vector2 p,float age){
   float force=1-Ease(age/220);
   if(force<=0)return;
   Color blue=new Color32(109,213,248,255),white=new Color32(237,250,255,255);
   for(int i=-1;i<=1;i++){
    var start=p+new Vector2(-37-age*.11f,i*16-7);
    var tip=p+new Vector2(19+age*.07f,i*9-13);
    Line(mesh,start,tip,3.3f,Alpha(blue,force));
    Line(mesh,start+new Vector2(12,-1),tip,1.3f,Alpha(white,force));
   }
   var head=p+new Vector2(age*.06f,-27);
   Line(mesh,head+new Vector2(-37,4),head+new Vector2(-29,-20),2.5f,Alpha(blue,force));
   Line(mesh,head+new Vector2(-29,-20),head+new Vector2(-16,-9),2.5f,Alpha(blue,force));
   Line(mesh,head+new Vector2(-16,-9),head+new Vector2(11,-6),2.5f,Alpha(blue,force));
   Line(mesh,head+new Vector2(11,-6),head+new Vector2(28,3),2.5f,Alpha(white,force));
   Line(mesh,head+new Vector2(28,3),head+new Vector2(8,10),2.5f,Alpha(blue,force));
   Glow(mesh,p,22+age*.09f,17+age*.06f,Alpha(white,force*.55f),Alpha(blue,0));
  }

  Color ReworkedAccent(){
   switch(clip.fxStyle){
    case "Goku":return new Color32(88,204,254,255);
    case "Piccolo":return new Color32(188,225,113,255);
    case "Krillin":return new Color32(255,232,107,255);
    case "Raditz":return new Color32(248,109,201,255);
    case "Yamcha":return new Color32(243,178,91,255);
    case "Tien":return new Color32(255,207,89,255);
    default:return new Color32(215,234,120,255);
   }
  }

  void DrawReworkedStrike(VertexHelper mesh,Vector2 point,string kind,float age){
   float phase=Ease(age/255);
   if(phase>=1)return;
   bool rising=kind=="lift"||kind=="uppercut";
   bool sweep=kind=="sweep";
   bool down=kind=="hammer";
   var color=ReworkedAccent();
   Vector2 direction=rising?new Vector2(.42f,-.91f):down?Vector2.up:Vector2.right;
   Vector2 normal=new Vector2(-direction.y,direction.x);
   float strength=1-phase;
   for(int i=0;i<4;i++){
    var start=point-direction*(23+7*i+phase*11)+normal*(i-1.5f)*5;
    var end=point+direction*(17+phase*29+i*4)+normal*(i-1.5f)*10;
    Line(mesh,start,end,i==2?3.4f:1.7f,Alpha(i==2?Color.white:color,strength*.89f));
   }
   float radius=18+phase*36;
   Arc(mesh,point,radius,radius*(sweep||down?.38f:.70f),-.25f,Mathf.PI*1.62f,
    rising?-.7f:0,Alpha(color,strength*.89f),3.6f);
   Glow(mesh,point,26+phase*21,20+phase*13,Alpha(Color.white,strength*.88f),Alpha(color,0));
  }

  void DrawReworkedRelease(VertexHelper mesh){
   float fire=clip.beam!=null&&clip.beam.fire>0?clip.beam.fire:clip.launchAt;
   bool aura=fire<=0&&clip.impacts.Length==0;
   if(fire<=0&&!aura)return;
   float local=aura?Mathf.InverseLerp(95,Mathf.Min(630,clip.duration*.75f),time):
    Mathf.InverseLerp(fire-180,fire+180,time);
   if(local<=0||local>=1)return;
   var source=aura?Ground(time)+new Vector2(0,-88):Palm(time,false);
   if(clip.fx=="saibaDetonate")source=Ground(time)+new Vector2(0,-48);
   ScaleLocalEffect(source);
   var color=ReworkedAccent();
   float visible=Mathf.Sin(local*Mathf.PI);
   var spread=17+38*local;
   Arc(mesh,source,spread,spread*.54f,time*.018f,time*.018f+4.2f,-.5f,Alpha(color,visible*.84f),2.5f);
   for(int i=0;i<6;i++){
    var angle=i*Mathf.PI/3+time*.004f;
    var offset=Oval(spread*1.2f,spread*.77f,angle,0);
    Line(mesh,source+offset*.63f,source+offset,1.7f,Alpha(i%2==0?color:Color.white,visible*.82f));
   }
  }

  void DrawWindup(VertexHelper mesh, Vector2 p, string kind, float a) {
   for (int n = 0; n < 5; n++) {
    float offset = (n - 2) * 4.5f;
    float length = 17 + n * 5 + a * 21;
    var tail = (kind == "lift" || kind == "uppercut") ? new Vector2(-length * .82f + offset, length) : new Vector2(-length, offset);
    Line(mesh, p + tail, p + new Vector2(-9, offset + 1), 1.5f, Alpha(Warm, a * .73f));
   }
  }

  void DrawImpact(VertexHelper mesh, Vector2 p, string kind, float age) {
   if(kind=="hammer"){
    float force=1-Ease(age/245);
    if(force<=0)return;
    var gold=Alpha(Warm,force);
    for(int i=-2;i<=2;i++)
     Line(mesh,p+new Vector2(i*7,-42-Mathf.Abs(i)*6),p+new Vector2(i*13,19),i==0?5:2,gold);
    Glow(mesh,p,40+age*.08f,23+age*.03f,Alpha(Pale,force),Alpha(Warm,0));
    return;
   }
   float a = 1 - Ease(age / 355);
   bool body = kind == "body", low = kind == "sweep", lift = kind == "lift" || kind == "uppercut";
   float extent = 9 + (body ? 21 : lift ? 17 : 12) * Ease(age / 59) + age * .094f;
   if (low) {
    var center = p + new Vector2(-41 + age * .1f, 8);
    Arc(mesh, center, 69 + age * .06f, 12 + age * .03f, .07f, Mathf.PI * .88f, -.16f, Alpha(Warm, a), 6);
    Arc(mesh, center, 69 + age * .06f, 9 + age * .03f, .12f, Mathf.PI * .75f, -.16f, Alpha(Pale, a), 2.5f);
   }
   if (lift) Arc(mesh, p + new Vector2(-28, 27), 12 + age * .06f, 51 + age * .1f,
    Mathf.PI, Mathf.PI * 1.93f, .8f, Alpha(Warm, a), 4);
   for (int n = 0; n < (body ? 12 : 8); n++) {
    float angle = n * Mathf.PI / 4.7f + (lift ? -.54f : .11f);
    float ray = extent * (.75f + n % 5 * .19f);
    var offset = Oval(ray, ray * (low ? .47f : .90f), angle, 0);
    Line(mesh, p + offset * .38f, p + offset, n % 3 == 0 ? 3.6f : 1.7f, Alpha(n % 3 == 0 ? Pale : Warm, a));
   }
   Glow(mesh, p, 23 + age * .055f, 21 + age * .045f, Alpha(Pale, a), Alpha(Warm, 0));
   float core = Mathf.Max(0, (body ? 12 : 9) - age * .055f);
   if (core > 0) Glow(mesh, p, core, core * .8f, Alpha(Color.white, a), Alpha(Pale, a));
  }

  Vector2 Palm(float at,bool blend) {
   int key=clip.KeyAt(at);
   var pose=art.Atlas[clip.poses[key].drawing];
   var ground=Ground(at);
   if(pose.beam==null||pose.beam.Length!=2)
    throw new System.InvalidOperationException("Missing beam anchor at "+clip.poses[key].at);
   var anchor=RoundMotionArt.Anchor(pose,pose.beam,ground,poseScale);
   if(!blend||key+1>=clip.poses.Length)return anchor;
   var next=clip.poses[key+1];
   var future=art.Atlas[next.drawing];
   if(future.beam==null||future.beam.Length!=2)return anchor;
   var to=RoundMotionArt.Anchor(future,future.beam,ground,poseScale);
   float interval=Mathf.Min(110,next.at-clip.poses[key].at);
   if((to-anchor).sqrMagnitude>22*22)interval=Mathf.Min(interval,32);
   return Vector2.Lerp(anchor,to,Ease((at-next.at+interval)/interval));
  }

  float DrawPiccoloCharge(VertexHelper mesh,Vector2 wrist,float strength,float phase){
   if(strength<=0)return 0;
   effectScale=1;
   float size=Mathf.Lerp(4,11,strength)+Mathf.Sin(phase*.014f)*.5f;
   Glow(mesh,wrist,size*1.42f,size*1.42f,Alpha(PiccoloViolet,strength*.82f),Alpha(PiccoloViolet,0));
   Glow(mesh,wrist,size*.55f,size*.55f,Alpha(Color.white,strength),Alpha(PiccoloGold,0));
   return size;
  }

  Vector2 PiccoloFingertips(float at){
   if(visibleRightFingertips.HasValue)return visibleRightFingertips.Value;
   var pose=art.At(clip,at);
   return pose.HasRightFingertips?RoundMotionArt.Anchor(pose,pose.RightFingertipCenter,Ground(at),poseScale):Palm(at,false);
  }

  float DrawPiccoloBrowEnergy(VertexHelper mesh,Vector2 point,float gain,float phase){
   if(gain<=0)return 0;
   BeamPalm=point;
   effectScale=1;
   float radius=3+5*gain+.55f*Mathf.Sin(phase*.023f);
   Glow(mesh,point,radius*2.2f,radius*1.9f,Alpha(PiccoloViolet,.74f*gain),Alpha(PiccoloViolet,0));
   Glow(mesh,point,radius,radius*.84f,Alpha(Color.white,gain),Alpha(PiccoloGold,0));
   for(int i=0;i<6;i++){
    float angle=i*Mathf.PI/3+phase*.003f;
    float pull=1-Mathf.Repeat(phase*.0028f+i*.19f,1);
    var direction=Oval(1,1,angle,0);
    var end=point+direction*(radius*(.9f+pull*1.05f));
    Line(mesh,end+direction*(2+pull*6),end,1.2f,Alpha(i%2==0?PiccoloGold:PiccoloViolet,gain*(1-pull*.65f)));
   }
   return radius;
  }

  void DrawPiccoloCannon(VertexHelper mesh,RoundMotionArt.Beam beam){
   effectScale=1;
   if(time<beam.fire){
    var hand=PiccoloFingertips(time);
    float wind=Ease((time-beam.compress)/(beam.fire-beam.compress));
    BeamChargeRadius=DrawPiccoloBrowEnergy(mesh,hand,1-.22f*wind,time);
    float whip=Mathf.InverseLerp(beam.push-48,beam.fire,time);
    if(whip>0){
     float length=12+whip*41;
     for(int i=0;i<3;i++){
      Vector2 offset=new Vector2(-4,i*5-4);
      Line(mesh,hand+offset+new Vector2(-length,12+i*2),hand+offset,1.5f+i*.4f,
       Alpha(i==1?Pale:PiccoloViolet,whip*.85f));
     }
    }
    return;
   }

   var palm=PiccoloFingertips(beam.fire);
   float age=time-beam.fire;
   float reach=Mathf.Clamp01(age/beam.flight);
   float through=Ease((age-beam.flight)/93);
   float life=1-Ease(Mathf.InverseLerp(beam.cut,beam.extinguish,time));
   float hitX=Mathf.Clamp(beamTarget?.x??beamEnd,palm.x+14,Width);
   float forward=Mathf.Max(Width+190,hitX+250);
   float headX=Mathf.Lerp(Mathf.Lerp(palm.x,hitX,reach),forward,through);
   var tip=new Vector2(headX,palm.y);
   float recoil=1-Ease(age/205);
   float radius=(17+17*recoil)*Mathf.Lerp(.5f,1,life);
   BeamPalm=palm;BeamTip=tip;BeamOpacity=life;BeamReach=reach;
   BeamRadius=radius;BeamTipOpacity=life;BeamRootDepth=radius;
   Glow(mesh,palm,19+radius*1.65f,12+radius*.96f,Alpha(PiccoloGold,life*(.64f+recoil*.28f)),Alpha(PiccoloViolet,0));
   if(headX-palm.x>1){
    var orchid=new Color32(157,53,234,255);
    var cream=new Color32(255,248,204,255);
    DrawWaveLayer(mesh,palm,tip,Vector2.up,radius*1.9f,life,orchid,.77f);
    DrawWaveLayer(mesh,palm,tip,Vector2.up,radius*1.12f,life,PiccoloViolet,.94f);
    DrawWaveLayer(mesh,palm,tip,Vector2.up,radius*.62f,life,PiccoloGold,.98f);
    DrawWaveLayer(mesh,palm,tip,Vector2.up,radius*.25f,life,cream,1);
    Line(mesh,palm,tip,3.5f*life,Alpha(Color.white,.94f*life));
    var back=palm;
    var front=palm;
    float span=headX-palm.x;
    for(int i=1;i<=82;i++){
     float x=span*i/82f;
     float turn=x*.080f-age*.040f;
     float roll=radius*(.63f+.24f*Mathf.Sin(x*.004f))*Mathf.Sin(turn);
     var line=palm+new Vector2(x,roll);
     var other=palm+new Vector2(x,-roll);
     Line(mesh,back,line,2.1f+recoil*.9f,Alpha(PiccoloGold,life*(.60f+.34f*Mathf.Cos(turn))));
     Line(mesh,front,other,1.1f+recoil,Alpha(cream,life*(.54f-.33f*Mathf.Cos(turn))));
     back=line;
     front=other;
    }
    if(through<.98f)Glow(mesh,tip,24+19*recoil,radius,Alpha(cream,life),Alpha(PiccoloViolet,0));
   }

   float pulse=1-Ease(age/235);
   if(pulse>0){
    float burst=Ease(age/105);
    Arc(mesh,palm+Vector2.right*(5+burst*49),7+22*burst,17+41*burst,-1.41f,1.41f,0,Alpha(Pale,pulse),4.7f);
    for(int i=0;i<10;i++){
     float slope=(i-4.5f)*.23f;
     var tail=palm+new Vector2(4+i%3*6,slope*17);
     var end=tail+new Vector2((43+i%4*18)*(1+burst),slope*(38+burst*46));
     Line(mesh,tail,end,i%3==0?3.7f:1.6f,Alpha(i%2==0?Color.white:PiccoloGold,pulse*.83f));
    }
   }
   float contactAge=age-beam.flight;
   if(!dodged&&contactAge>=0&&contactAge<220){
    float burst=Ease(contactAge/220);
    var contact=new Vector2(hitX,palm.y);
    Glow(mesh,contact,33+burst*51,26+burst*34,Alpha(Pale,1-burst),Alpha(PiccoloViolet,0));
    Arc(mesh,contact,10+36*burst,25+42*burst,-1.29f,1.29f,0,Alpha(PiccoloGold,(1-burst)*.94f),3.3f);
   }
  }

  void DrawPiccoloBeam(VertexHelper mesh,RoundMotionArt.Beam beam){
   var palm=Palm(time<beam.fire?time:beam.fire,false);
   if(time<beam.fire){
    BeamChargeRadius=DrawPiccoloCharge(mesh,palm,Mathf.Lerp(.86f,1,Mathf.InverseLerp(0,beam.fire,time)),time);
    return;
   }
   var target=new Vector2(beamTarget?.x??beamEnd,palm.y);
   float reach=Mathf.Clamp01((time-beam.fire)/beam.flight);
   float life=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(beam.cut,beam.extinguish,time));
   var direction=(target-palm).normalized;
   var across=new Vector2(-direction.y,direction.x);
   var tip=Vector2.Lerp(palm,target,reach);
   BeamPalm=palm;BeamTip=tip;BeamReach=reach;BeamOpacity=life;BeamRadius=5;
   effectScale=1;
   Glow(mesh,palm,15,13,Alpha(PiccoloGold,life),Alpha(PiccoloViolet,0));
   Line(mesh,palm,tip,17,Alpha(PiccoloViolet,.22f*life));
   Line(mesh,palm,tip,8,Alpha(PiccoloViolet,.88f*life));
   Line(mesh,palm,tip,3,Alpha(Color.white,.95f*life));
   var last=palm;
   for(int i=1;i<=56;i++){
    float u=i/56f;
    var next=Vector2.Lerp(palm,tip,u)+across*(Mathf.Sin(u*reach*28-time*.048f)*Mathf.Sin(u*Mathf.PI)*12*life);
    Line(mesh,last,next,3.5f,Alpha(PiccoloGold,life));
    last=next;
   }
   Glow(mesh,tip,dodged?8:14,dodged?8:12,Alpha(Pale,life),Alpha(PiccoloGold,0));
  }

  void DrawGohanHeavyCharge(VertexHelper mesh){
   if(time<clip.haloFrom||time>=clip.haloTo)return;
   float power=Ease(Mathf.InverseLerp(clip.haloFrom,clip.launchAt,time))*
    (1-Ease(Mathf.InverseLerp(clip.launchAt,clip.haloTo,time)));
   float spread=Mathf.Lerp(17,39,power);
   var gold=new Color32(246,181,80,255);
   var warm=new Color32(255,239,173,255);
   var fist=Palm(Mathf.Min(time,clip.launchAt-1),false);
   Glow(mesh,fist,spread,spread*.80f,Alpha(gold,.48f*power),Alpha(gold,0));
   Arc(mesh,fist,spread*.82f,spread*.63f,-2.82f,.66f,-.30f,Alpha(warm,.94f*power),3);
   var feet=Ground(time);
   for(int i=0;i<4;i++){
    float angle=-2.9f+i*.86f;
    var edge=feet+Oval(45,12,angle,0);
    Line(mesh,edge,edge+new Vector2(-12+i*8,-8-i),2.3f,Alpha(gold,.73f*power));
   }
  }

  void DrawGohanRally(VertexHelper mesh){
   float strength=Ease((time-130)/270)*(1-Ease((time-510)/280));
   if(strength<=0)return;
   var feet=Ground(time);
   var mint=new Color32(158,255,220,255);
   var rim=new Color32(229,255,240,255);
   Glow(mesh,feet+new Vector2(0,-115*poseScale),66,126,Alpha(mint,.39f*strength),Alpha(mint,0));
   for(int i=0;i<3;i++){
    float u=Mathf.Repeat((time-130+i*139)/420,1);
    var ring=feet+new Vector2(0,-u*192*poseScale);
    float opacity=Mathf.Sin(u*Mathf.PI)*strength;
    Arc(mesh,ring,56-i*4,15,-2.76f,.22f,.10f,Alpha(rim,opacity),3);
   }
  }

  void DrawGohanFlash(VertexHelper mesh,Vector2 hand,float age){
   float opacity=(1-Ease(age/230));
   if(opacity<=0)return;
   var gold=new Color32(255,212,90,255);
   var white=new Color32(255,246,190,255);
   var center=hand+new Vector2(age*.065f,0);
   Arc(mesh,center,18+age*.055f,49+age*.086f,-1.40f,1.46f,.13f,Alpha(gold,opacity),6);
   Arc(mesh,center,15+age*.055f,47+age*.086f,-1.09f,1.25f,.13f,Alpha(white,opacity),2);
  }

  void DrawGohanBarrier(VertexHelper mesh,Vector2 center,float age){
   float opacity=(.65f+.35f*Ease(age/85))*(1-Ease((age-230)/245));
   var blue=new Color32(80,198,253,255);
   var rim=new Color32(197,244,255,255);
   float radius=25+5*Ease(age/140);
   Glow(mesh,center,radius+6,radius+8,Alpha(blue,.20f*opacity),Alpha(blue,0));
   Arc(mesh,center,radius,radius+3,-Mathf.PI,Mathf.PI,0,Alpha(blue,.88f*opacity),2);
   Arc(mesh,center,radius-2,radius+1,-2.58f,-.34f,0,Alpha(rim,opacity),3);
  }

  void DrawPiccoloGuard(VertexHelper mesh,Vector2 center,float age){
   float power=(.4f+.6f*Ease(age/95))*(1-Ease((age-145)/180));
   float size=13+age*.066f;
   var flash=new Color32(255,237,148,255);
   Glow(mesh,center,size,size*.92f,Alpha(flash,.75f*power),Alpha(PiccoloGold,0));
   Arc(mesh,center,size*.83f,size*1.25f,-2.20f,1.13f,-.42f,Alpha(flash,power),2.5f);
   Arc(mesh,center,size*.54f,size*1.44f,.55f,3.45f,.28f,Alpha(PiccoloGold,.82f*power),2);
  }

  void DrawPiccoloCounterShield(VertexHelper mesh,Vector2 center,float age){
   effectScale=1;
   float spread=Ease(age/185);
   float fade=1-Ease(age/312);
   var purple=new Color32(194,112,255,255);
   var mint=new Color32(180,255,144,255);
   float radius=15+spread*29;
   Glow(mesh,center,radius,radius*.84f,Alpha(purple,fade*.62f),Alpha(purple,0));
   Arc(mesh,center,radius*.61f,radius,-1.37f,1.37f,.09f,Alpha(purple,fade),3.5f);
   Arc(mesh,center,radius*.44f,radius*.88f,-1.24f,1.24f,.09f,Alpha(mint,fade*.91f),2.1f);
   for(int i=0;i<5;i++){
    float angle=i*Mathf.PI*.40f+age*.008f;
    var ray=Oval(1,1,angle,0);
    Line(mesh,center+ray*(8+spread*8),center+ray*(19+spread*17),i==2?2.2f:1.4f,
     Alpha(i%2==0?mint:purple,fade*.84f));
   }
  }

  void DrawPiccoloCounterSurge(VertexHelper mesh){
   if(!beamTarget.HasValue)return;
   effectScale=1;
   var purple=new Color32(175,95,247,255);
   var green=new Color32(136,239,65,255);
   var white=new Color32(247,255,206,255);
   if(time<clip.launchAt){
    var palm=Palm(time,false);
    float squeeze=Ease(time/clip.launchAt);
    float radius=Mathf.Lerp(21,9,squeeze);
    Glow(mesh,palm,radius*2.1f,radius*1.8f,Alpha(green,.53f+squeeze*.37f),Alpha(purple,0));
    Glow(mesh,palm,radius*.63f,radius*.63f,Alpha(white,.92f),Alpha(green,.2f));
    for(int i=0;i<3;i++){
     float angle=i*Mathf.PI*2/3+time*.012f;
     Arc(mesh,palm,radius*1.34f,radius*.62f,angle,angle+1.24f,-.53f,Alpha(purple,.87f),2.45f);
    }
    return;
   }

   var hand=Palm(clip.launchAt,false);
   var target=beamTarget.Value;
   var direction=(target-hand).normalized;
   var side=new Vector2(-direction.y,direction.x);
   float angleOfAttack=Mathf.Atan2(direction.y,direction.x);
   float age=Mathf.Max(0,time-clip.impactAt);
   float power=1-Ease(age/268);
   if(power<=0)return;

   float travel=Mathf.InverseLerp(clip.launchAt,clip.impactAt,time);
   var nose=Vector2.Lerp(hand,target,Mathf.Pow(travel,.79f))+direction*(age*.068f);
   var root=Vector2.Lerp(hand,nose,Ease(age/230)*.76f);
   float wing=10+44*Ease(travel)+age*.055f;
   CounterWidth=wing*2;
   int strip=mesh.currentVertCount;
   for(int k=0;k<=16;k++){
    float u=k/16f;
    var flow=Vector2.Lerp(root,nose,u);
    float span=Mathf.Lerp(3,wing,u*u)*(1+.075f*Mathf.Sin(u*23-time*.037f));
    Vertex(mesh,flow-side*span,Alpha(purple,0));
    Vertex(mesh,flow-side*span*.72f,Alpha(purple,power*.68f));
    Vertex(mesh,flow-side*span*.38f,Alpha(green,power*.92f));
    Vertex(mesh,flow,Alpha(white,power*.86f));
    Vertex(mesh,flow+side*span*.38f,Alpha(green,power*.92f));
    Vertex(mesh,flow+side*span*.72f,Alpha(purple,power*.68f));
    Vertex(mesh,flow+side*span,Alpha(purple,0));
    if(k==0)continue;
    int start=strip+(k-1)*7;
    for(int band=0;band<6;band++){
     int row=start+band;
     mesh.AddTriangle(row,row+1,row+7);
     mesh.AddTriangle(row+1,row+8,row+7);
    }
   }

   float flash=1-Ease((time-clip.launchAt)/170);
   if(flash>0){
    Glow(mesh,hand,28,27,Alpha(white,flash),Alpha(green,0));
    Arc(mesh,hand+direction*(10+14*(1-flash)),7+9*(1-flash),19+18*(1-flash),-1.38f,1.38f,
     angleOfAttack,Alpha(white,flash),3.4f);
   }
   for(int i=0;i<3;i++){
    float level=1-i*.24f;
    var curl=Vector2.Lerp(root,nose,level);
    float swell=wing*(.55f+.45f*level);
    Arc(mesh,curl,9+travel*15,swell,-1.34f,1.34f,angleOfAttack,Alpha(i==0?white:green,power*level),i==0?5:3.1f);
    Arc(mesh,curl-direction*6,8+travel*11,swell*1.05f,-1.40f,1.40f,angleOfAttack,Alpha(purple,power*.84f*level),2.5f);
   }

   if(!dodged&&time>=clip.impactAt){
    var contact=target+direction*(2+age*.11f);
    Glow(mesh,contact,24+age*.19f,33+age*.13f,Alpha(white,power),Alpha(green,0));
    for(int i=0;i<8;i++){
     float spread=(i-3.5f)*.235f;
     var ray=contact+side*(spread*wing*.64f);
     Line(mesh,ray,ray+direction*(21+i%3*11+age*.17f)+side*(spread*(50+age*.27f)),i%3==0?3.8f:1.55f,
      Alpha(i%2==0?white:purple,power*.90f));
    }
   }
  }

  void DrawPiccoloCounter(VertexHelper mesh){
   if(!beamTarget.HasValue)return;
   if(time<clip.launchAt){
    DrawPiccoloGuard(mesh,Palm(time,false),time);
    return;
   }
   var hand=Palm(clip.launchAt,false);
   float progress=Mathf.InverseLerp(clip.launchAt,clip.impactAt,time);
   float release=Mathf.Max(0,time-clip.impactAt);
   float opacity=1-Ease(release/205);
   if(opacity<=0)return;
   effectScale=1;
   var aim=beamTarget.Value;
   var direction=(aim-hand).normalized;
   var across=new Vector2(-direction.y,direction.x);
   float angle=Mathf.Atan2(direction.y,direction.x);
   var front=Vector2.Lerp(hand,aim,progress)+direction*(release*.12f);
   float half=9+42*Ease(progress)+release*.10f;
   CounterWidth=half*2;
   var flash=new Color32(255,244,174,255);
   var bright=Alpha(flash,opacity);
   var glow=Alpha(PiccoloGold,.8f*opacity);
   var amber=new Color32(255,124,42,255);
   var shell=Alpha(amber,.88f*opacity);
   var edgeTint=Alpha(PiccoloGold,0);
   // The cross section grows continuously toward the leading edge.
   int strip=mesh.currentVertCount;
   for(int step=0;step<=12;step++){
    float u=step/12f;
    float belly=Mathf.Lerp(3,half,Mathf.Pow(u,1.25f));
    var center=Vector2.Lerp(hand,front,u);
    Vertex(mesh,center-across*belly,edgeTint);
    Vertex(mesh,center-across*belly*.66f,shell);
    Vertex(mesh,center,Alpha(flash,opacity*Mathf.Lerp(.87f,.6f,u)));
    Vertex(mesh,center+across*belly*.66f,shell);
    Vertex(mesh,center+across*belly,edgeTint);
    if(step==0)continue;
    int current=strip+step*5;
    for(int band=0;band<4;band++){
     int i=current+band;
     mesh.AddTriangle(i-5,i-4,i);
     mesh.AddTriangle(i,i-4,i+1);
    }
   }
   Glow(mesh,hand,17,21,bright,Alpha(PiccoloGold,0));
   for(int band=-2;band<=2;band++){
    var edge=front+across*(half*band*.4f);
    Line(mesh,hand+across*band*2,edge,band==0?6:2.5f,band==0?bright:glow);
    Line(mesh,edge,edge+direction*(9+progress*25)-across*(band*5),1.5f,glow);
   }
   for(int wave=0;wave<3;wave++){
    float at=progress-wave*.18f;
    if(at<0)continue;
    var crest=Vector2.Lerp(hand,front,1-wave*.24f);
    float spread=half*(1-wave*.13f);
    Arc(mesh,crest,11+progress*14,spread,-1.44f,1.44f,angle,
     Alpha(wave==0?flash:PiccoloGold,opacity*(1-wave*.22f)),wave==0?5:3);
   }
   Glow(mesh,front,half*.65f,half*.98f,Alpha(flash,.72f*opacity),Alpha(PiccoloGold,0));
   for(int i=0;i<8;i++){
    float sign=i%2==0?1:-1;
    float fan=(.46f+i*.06f)*sign;
    var ray=front-direction*(i%3)*5+across*(half*fan);
    Line(mesh,ray,ray+direction*(9+release*.21f)+across*(sign*(9+release*.07f)),
     1.5f,Alpha(flash,.82f*opacity));
   }
  }

  void DrawBeam(VertexHelper mesh, RoundMotionArt.Beam beam) {
   var p=time<beam.fire?Palm(time,true):Palm(beam.fire,false);
   if (time < beam.fire) {
    float gain = Ease((time - beam.gather) / (beam.compress - beam.gather));
    float squeeze = Ease((time - beam.compress) / (beam.push - beam.compress));
    float release = Ease((time - beam.push) / (beam.fire - beam.push));
    float r = 5 + 22 * gain - 9 * squeeze + 17 * release + Mathf.Sin(time * .042f) * 2 * gain;
    BeamChargeRadius=r;
    float opacity = Ease((time - beam.gather) / 135);
    Glow(mesh, p, r * 2.6f, r * 2.55f, Alpha(Cyan, opacity * .8f), Alpha(Blue, 0));
    Glow(mesh, p, r, r * .91f, Alpha(Color.white, opacity), Alpha(Cyan, opacity * .8f));
    for (int n = 0; n < 3; n++)
     Arc(mesh, p, r * (1.35f + n * .17f), r * (.63f + n * .18f), -.9f, 1.32f,
      time * .0024f + n * Mathf.PI / 3, Alpha(Cyan, opacity), 1.3f);
    for (int n = 0; n < 12; n++) {
     float inwards = 1 - Mathf.Repeat(time - beam.gather + n * 47, 480) / 480;
     float ringRadius = r + (20 + gain * 27) * inwards, angle = n * Mathf.PI / 6 + time * .003f;
     Line(mesh, p + Oval(ringRadius, ringRadius, angle, 0), p + Oval(ringRadius + 8, ringRadius + 8, angle, 0), 1.5f, Alpha(Cyan, opacity));
    }
    return;
   }

   float age=time-beam.fire;
   float life=1-Ease((time-beam.cut)/(beam.extinguish-beam.cut));
   float reach=Mathf.Clamp01(age/beam.flight);
   float distance=Mathf.Max(0,(beamTarget?.x??beamEnd)-p.x);
   var direction=Vector2.right;
   var normal=Vector2.up;
   var tip=p+direction*(distance*reach);
   BeamPalm=p;
   BeamTip=tip;
   float radius=Mathf.Lerp(21,23,Mathf.InverseLerp(.18f,Scale,poseScale))*
    Mathf.Lerp(.55f,1,Ease(age/75))*Mathf.Lerp(.49f,1,life);

   BeamOpacity=life;
   BeamReach=reach;
   BeamRadius=radius;
   BeamTipOpacity=WaveOpacity(.86f,life);
   Glow(mesh,p,radius*2.05f,radius*1.86f,Alpha(Cyan,.46f*life),Alpha(Blue,0));
   if(distance*reach<.5f)return;
   BeamRootDepth=radius*1.53f*.68f;
   BeamTailRadius=radius*WaveWidth(.86f);

   DrawWaveLayer(mesh,p,tip,normal,radius*1.53f,life,new Color32(54,148,249,255),.42f);
   DrawWaveLayer(mesh,p,tip,normal,radius*1.04f,life,new Color32(43,143,255,255),.76f);
   DrawWaveLayer(mesh,p,tip,normal,radius*.66f,life,new Color32(118,225,255,255),.87f);
   DrawWaveLayer(mesh,p,tip,normal,radius*.28f,life,new Color32(230,253,255,255),.96f);
   float pulse=reach<1?.68f:dodged?0:1-Ease((age-beam.flight)/185);
   var head=tip-direction*Mathf.Min(radius*.42f,distance*reach*.18f);
   if(pulse>0)
    Glow(mesh,head,radius*1.54f,radius,Alpha(Cyan,pulse*.71f*life),Alpha(Blue,0));
   DrawWaveStreaks(mesh,p,tip,direction,normal,radius,life);
  }

  static float WaveOpacity(float u,float life){
   float feather=1-Ease(Mathf.InverseLerp(.66f,1,u));
   // Dissipation reaches the far end first and then runs back toward the caster.
   return feather*life*Mathf.Lerp(1,life*life,Ease(u));
  }

  float WaveWidth(float u){
   float bulge=.68f+.37f*Mathf.Sin(u*Mathf.PI);
   float taper=Mathf.Lerp(bulge,.070f,Ease(Mathf.InverseLerp(.684f,1,u)));
   float ripple=1+.105f*Mathf.Sin(u*22-time*.041f)+.047f*Mathf.Sin(u*56-time*.058f);
   return taper*ripple;
  }

  void DrawWaveLayer(VertexHelper mesh,Vector2 start,Vector2 tip,Vector2 normal,float radius,
   float life,Color color,float strength){
   const int steps=34, capSegments=12;
   // Use the same center and alpha gradient for the cap and the first body slice.
   int center=mesh.currentVertCount;
   float capHeight=radius*WaveWidth(0);
   Vertex(mesh,start,Alpha(color,WaveOpacity(0,life)*strength));
   for(int i=0;i<=capSegments;i++){
    float angle=Mathf.Lerp(-Mathf.PI*.5f,Mathf.PI*.5f,i/(float)capSegments);
    var edge=start+normal*(Mathf.Sin(angle)*capHeight)-Vector2.right*(Mathf.Cos(angle)*radius*.68f);
    Vertex(mesh,edge,Alpha(color,0));
    if(i>0)mesh.AddTriangle(center,center+i,center+i+1);
   }
   int first=mesh.currentVertCount;
   for(int i=0;i<=steps;i++){
    float u=i/(float)steps;
    float tilt=radius*.085f*Mathf.Sin(u*16-time*.033f)*Mathf.Sin(u*Mathf.PI);
    Vector2 axle=Vector2.LerpUnclamped(start,tip,u)+normal*tilt;
    Vector2 edge=normal*(radius*WaveWidth(u));
    Vertex(mesh,axle-edge,Alpha(color,0));
    Vertex(mesh,axle,Alpha(color,WaveOpacity(u,life)*strength));
    Vertex(mesh,axle+edge,Alpha(color,0));
    if(i==0)continue;
    int n=first+(i-1)*3;
    mesh.AddTriangle(n,n+1,n+3);
    mesh.AddTriangle(n+3,n+1,n+4);
    mesh.AddTriangle(n+1,n+2,n+4);
    mesh.AddTriangle(n+4,n+2,n+5);
   }
  }

  void DrawWaveStreaks(VertexHelper mesh,Vector2 start,Vector2 tip,Vector2 direction,Vector2 normal,float radius,float life){
   float span=(tip-start).magnitude;
   var tint=new Color32(230,251,255,255);
   for(int i=0;i<13;i++){
    float u=Mathf.Repeat(time*(4+i%3)+i*167,1480)/1480;
    float tail=Mathf.Min(1-u,(radius*(.65f+i%4*.42f))/span);
    float thickness=WaveWidth(u)*radius;
    float offset=thickness*(i%7-3)*.22f;
    var at=start+direction*(u*span)+normal*offset;
    Line(mesh,at,at+direction*(span*tail)+normal*(offset*-.21f),i%3==0?1.2f:.76f,
     Alpha(tint,WaveOpacity(u,life)*.6f));
   }
  }
 }
}
