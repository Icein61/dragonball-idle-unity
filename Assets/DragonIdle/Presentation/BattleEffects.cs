using System;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  Color EffectColor(BattleVisualKind kind){
   switch(kind){
    case BattleVisualKind.PurpleBeam:case BattleVisualKind.Psychic:case BattleVisualKind.DualBlast:return new Color(.78f,.47f,1);
    case BattleVisualKind.GreenBolt:case BattleVisualKind.Acid:return new Color(.59f,1,.36f);
    case BattleVisualKind.KaiokenRush:return new Color(1,.25f,.3f);
    case BattleVisualKind.BlueBeam:case BattleVisualKind.SpiritBomb:return new Color(.37f,.83f,1);
    default:return new Color(1,.79f,.28f);
   }
  }
  void Ring(Vector2 center,float rx,float ry,Color c,float width=2){
   Vector2 previous=center+new Vector2(rx,0);
   for(int i=1;i<=32;i++){float a=i*Mathf.PI/16;Vector2 next=center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);Line(previous,next,width,c);previous=next;}
  }
  void Orb(Vector2 p,float radius,Color c){
   Circle(p.x-radius*1.4f,p.y-radius*1.4f,radius*2.8f,new Color(c.r,c.g,c.b,c.a*.12f));
   Circle(p.x-radius,p.y-radius,radius*2,c);Circle(p.x-radius*.53f,p.y-radius*.53f,radius*1.06f,new Color(1,1,1,c.a*.9f));
  }
  void Burst(Vector2 center,float amount,Color color){
   float radius=12+amount*40;Ring(center,radius,radius*.7f,new Color(color.r,color.g,color.b,(1-amount)*.8f*color.a),2);
   for(int i=0;i<10;i++){float a=i*Mathf.PI/5+.2f;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(center+d*(radius*.45f),center+d*(radius+8),3*(1-amount)+1,new Color(color.r,color.g,color.b,(1-amount)*color.a));}
  }
  void EnergyBeam(Vector2 from,Vector2 to,float width,Color c,bool spiral){
   Line(from,to,width*2.3f,new Color(c.r,c.g,c.b,.12f*c.a));Line(from,to,width,c);Line(from,to,Mathf.Max(2,width*.34f),new Color(1,1,1,.95f*c.a));
   if(spiral){Vector2 d=to-from,n=new Vector2(-d.y,d.x).normalized;Vector2 previous=from;
    for(int i=1;i<=36;i++){float t=i/36f;Vector2 p=Vector2.Lerp(from,to,t)+n*Mathf.Sin(t*Mathf.PI*10-(float)action.Elapsed*12)*9;Line(previous,p,2,new Color(.85f,.55f,1));previous=p;}
   }
  }
  void DrawBattleEffects(){
   if(action==null||action.Complete||last==null||last.Move==Move.Stunned)return;
   float p=(float)action.Progress;var kind=action.Kind;var actor=last.Actor;float sign=actor.Side==0?1:-1;
   var feet=AnimatedFeet(actor);var from=feet+new Vector2(sign*26,-FighterHeight(Data.Index(actor.CardId))*(actor.Slot<3?.53f:.42f));
   if(kind==BattleVisualKind.SpiritBomb)from=feet+new Vector2(0,-112);
   if(kind==BattleVisualKind.Disc||kind==BattleVisualKind.Masenko)from=feet+new Vector2(sign*8,-88);
   Color color=EffectColor(kind);float intensity=ReducedBattleMotion?.55f:1;
   if(p<.43f&&!action.Melee){
    float r=(kind==BattleVisualKind.SpiritBomb?31:12)*(p/.43f+.25f);
    Orb(from,r,new Color(color.r,color.g,color.b,.7f*intensity));Ring(from,r+10,r+6,new Color(color.r,color.g,color.b,.5f*intensity));
    if(!ReducedBattleMotion)for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f+p*8;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(from+d*(r+24),from+d*(r+12),2,new Color(color.r,color.g,color.b,.5f));}
   }
   if(kind==BattleVisualKind.KaiokenRush&&p>.12f&&p<.76f){Ring(feet+new Vector2(0,-43),40,62,new Color(1,.2f,.25f,.5f));if(!ReducedBattleMotion)Line(feet+new Vector2(-sign*90,-35),feet+new Vector2(-sign*20,-35),8,new Color(1,.2f,.25f,.25f));}
   for(int i=0;i<last.Hits.Count;i++){
    var hit=last.Hits[i];var to=FighterFeet(hit.Target)+new Vector2(0,-42);float impact=(float)action.ImpactAt(i),t=(float)action.Elapsed;
    float travel=Mathf.Clamp01((t-impact+.28f)/.28f);bool release=t>=impact-.28f&&t<=impact+.25f;
    if(release){
     Vector2 tip=Vector2.Lerp(from,to,travel);Color c=new Color(color.r,color.g,color.b,intensity);
     switch(kind){
      case BattleVisualKind.BlueBeam:case BattleVisualKind.PurpleBeam:case BattleVisualKind.SpiralBeam:case BattleVisualKind.FingerBeam:case BattleVisualKind.Masenko:
       EnergyBeam(from,tip,kind==BattleVisualKind.FingerBeam?4:kind==BattleVisualKind.SpiralBeam?7:15,c,kind==BattleVisualKind.SpiralBeam);break;
      case BattleVisualKind.Disc:
       Ring(tip,27,7,c,4);Line(tip-new Vector2(23,0),tip+new Vector2(23,0),3,Color.white);Line(tip-new Vector2(sign*58,0),tip-new Vector2(sign*22,0),3,new Color(c.r,c.g,c.b,.35f));break;
      case BattleVisualKind.SolarFlare:
       for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(from+d*14,from+d*(35+travel*48),3,new Color(1,.95f,.6f,.8f*intensity));}Ring(to,30,36,c,2);break;
      case BattleVisualKind.TriBeam:
       Vector2 triA=from+new Vector2(0,-12),triB=from+new Vector2(-12,9),triC=from+new Vector2(12,9);Line(triA,triB,3,c);Line(triB,triC,3,c);Line(triC,triA,3,c);
       EnergyBeam(from,tip,20,new Color(1,.85f,.4f,.5f*intensity),false);Ring(to,22+travel*18,22+travel*18,c);break;
      case BattleVisualKind.SpiritBall:
       tip.y-=Mathf.Sin(travel*Mathf.PI)*68;Orb(tip,14,c);for(int j=1;j<4;j++){float q=Mathf.Max(0,travel-j*.05f);var trail=Vector2.Lerp(from,to,q);trail.y-=Mathf.Sin(q*Mathf.PI)*68;Orb(trail,5-j,new Color(c.r,c.g,c.b,.25f));}break;
      case BattleVisualKind.SpiritBomb:Orb(tip,30,c);Ring(tip,38,38,new Color(c.r,c.g,c.b,.45f));break;
      case BattleVisualKind.Psychic:
       Ring(to,25+travel*18,44,c);Ring(to,42,12,c);Line(from,tip,2,new Color(c.r,c.g,c.b,.3f));break;
      case BattleVisualKind.Slash:
       for(int j=0;j<3;j++){Vector2 prev=to+new Vector2(-34,22-j*7);for(int n=1;n<=12;n++){float q=n/12f;Vector2 point=to+new Vector2(-34+q*68,22-j*7-40*Mathf.Sin(q*Mathf.PI*.7f));Line(prev,point,3-j*.6f,new Color(1,1,1,(1-j*.2f)*intensity));prev=point;}}break;
      case BattleVisualKind.Acid:
       for(int j=0;j<4;j++){var drop=Vector2.Lerp(from,to,Mathf.Clamp01(travel-j*.06f));drop.y-=Mathf.Sin(travel*Mathf.PI)*28;Orb(drop,5-j*.7f,c);}break;
      case BattleVisualKind.DualBlast:
       Orb(tip,14,c);Line(from,tip,5,new Color(c.r,c.g,c.b,.3f));break;
      case BattleVisualKind.Explosion:
       if(travel<1)Orb(tip,15,c);else{Orb(to,27,new Color(1,.62f,.15f,.55f*intensity));Burst(to,Mathf.Clamp01((t-impact)/.35f),c);}break;
      case BattleVisualKind.GreenBolt:Orb(tip,11,c);Line(tip-new Vector2(sign*30,0),tip,5,new Color(c.r,c.g,c.b,.3f));break;
     }
    }
    if(action.HitVisible(i)){
     float age=t-impact;if(age<.38f&&!hit.Dodged)Burst(to,Mathf.Clamp01(age/.38f),new Color(color.r,color.g,color.b,intensity));
     if(age<.65f){string value=hit.Dodged?"闪避":(hit.Critical?"暴击 ":"")+hit.Damage.ToString("0");
      Label(to.x-48,to.y-54-(ReducedBattleMotion?0:age*28),180,35,value,hit.Critical?24:20,hit.Dodged?muted:hit.Critical?gold:Color.white);
      if(hit.ShieldDamage>0)Label(to.x-46,to.y-24,160,24,"护盾 -"+hit.ShieldDamage.ToString("0"),12,new Color(.4f,.85f,1));
     }
    }
   }
   if(last.Move==Move.Ultimate&&p<.30f){
    int index=Data.Index(actor.CardId);Box(680,304,150,188,new Color(.025f,.045f,.07f,.90f));Box(680,304,150,2,color);
    PaintPortrait(index,new Rect(694,311,122,140));Label(690,457,130,33,actor.UltimateName,15,gold);
   }
  }
 }
}
