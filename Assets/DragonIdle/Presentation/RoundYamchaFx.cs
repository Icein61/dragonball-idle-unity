using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  RawImage demoYamchaWolf;
  Texture2D yamchaWolfTexture;

  void PaintYamchaWolfFinisher(RawImage image,RoundMotionPlayback run,Vector2 origin,float scale,bool mirror){
   image.enabled=false;
   if(run?.IsYamchaWolf!=true)return;
   float age=run.Time-run.ImpactAt;
   if(age< -68||age>=345)return;
   if(yamchaWolfTexture==null)yamchaWolfTexture=Resources.Load<Texture2D>(RoundMotionArt.YamchaFolder+"wolf-finisher");
   if(yamchaWolfTexture==null)return;
   var hit=run.Clip.impacts[run.Clip.impacts.Length-1];
   var pose=run.Library.Atlas[hit.drawing];
   var claw=RoundMotionArt.Anchor(pose,pose.hit,origin+run.ShiftAt(run.Time),scale);
   float gather=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-68,0,age));
   float drift=Mathf.SmoothStep(0,1,Mathf.InverseLerp(108,345,age));
   float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(83,345,age));
   bool miss=run.Action.Dodged;
   Vector2 center=FacingSpace(claw+new Vector2(-26+32*gather+drift*(miss?65:27),-21-(miss?22:6)*drift),mirror);
   float width=Mathf.Lerp(64,163,gather)+drift*38;
   float height=width*yamchaWolfTexture.height/yamchaWolfTexture.width;
   var rect=image.rectTransform;
   rect.sizeDelta=new Vector2(width,height);
   rect.anchoredPosition=new Vector2(center.x-width*.5f,-center.y+height*.5f);
   image.texture=yamchaWolfTexture;
   image.uvRect=mirror?new Rect(1,0,-1,1):new Rect(0,0,1,1);
   image.color=new Color(1,1,1,gather*fade);
   image.enabled=true;
  }
 }

 internal sealed partial class RoundMotionFx {
  static readonly Color YamchaIndigo=new Color32(53,75,240,255);
  static readonly Color YamchaSky=new Color32(119,211,255,255);
  static readonly Color YamchaWhite=new Color32(239,253,255,255);

  void DrawYamchaClaw(VertexHelper mesh,Vector2 point,float age,bool final,int variant){
   effectScale=1;
   float life=final?325:167;
   float appear=Ease((age+42)/42);
   float fade=appear*(1-Ease(Mathf.Max(0,age)/life));
   float opening=Ease(Mathf.Max(0,age)/life);
   if(fade<=.002f)return;
   float turn=variant==0?-.32f:.49f;
   Vector2 axis=new Vector2(Mathf.Cos(turn),Mathf.Sin(turn));
   Vector2 side=new Vector2(-axis.y,axis.x);
   for(int i=-1;i<=1;i++){
    float spread=i*(final?16:11);
    Vector2 start=point-axis*((final?70:36)+opening*18)+side*(spread-9);
    Vector2 middle=point-axis*12+side*(spread*.76f+2);
    Vector2 tip=point+axis*((final?38:21)+opening*12)+side*(spread*.51f);
    float thick=(final?9:5.4f)*(1-.56f*opening);
    Line(mesh,start,middle,thick*1.38f,Alpha(YamchaIndigo,.70f*fade));
    Line(mesh,middle,tip,thick*1.2f,Alpha(YamchaIndigo,.83f*fade));
    Line(mesh,Vector2.Lerp(start,middle,.26f),middle,thick*.48f,Alpha(YamchaSky,fade));
    Line(mesh,middle,tip,thick*.46f,Alpha(YamchaSky,fade));
    Line(mesh,middle,Vector2.Lerp(middle,tip,.83f),thick*.17f,Alpha(YamchaWhite,fade));
   }
   if(age<0||dodged)return;
   if(!final){
    Glow(mesh,point,15+opening*13,10+opening*7,Alpha(YamchaWhite,fade*.46f),Alpha(YamchaIndigo,0));
    return;
   }
   float burst=1-Ease(age/235);
   Glow(mesh,point,27+opening*28,17+opening*23,Alpha(YamchaWhite,.73f*burst),Alpha(YamchaSky,0));
   Arc(mesh,point,19+opening*53,32+opening*30,-1.47f,1.52f,-.3f,Alpha(YamchaSky,.90f*burst),2.8f);
   for(int i=0;i<14;i++){
    float angle=.59f*i+.19f*(i%3);
    var dir=Oval(1,1,angle,0);
    var from=point+dir*(11+opening*(28+i%4*4));
    Line(mesh,from,from+dir*(7+i%5*3),1.2f+(i%3)*.37f,Alpha(i%3==0?YamchaWhite:YamchaSky,burst));
   }
  }

  void DrawYamchaFeignGlow(VertexHelper mesh){
   effectScale=1;
   var feet=Ground(time);
   var green=new Color32(120,209,140,255);
   var ivory=new Color32(237,255,206,255);
   float peek=Ease((time-497)/100)*(1-Ease((time-722)/98));
   if(peek>0)Glow(mesh,feet+new Vector2(-12,-9),18,7,Alpha(ivory,.23f*peek),Alpha(green,0));
   float rise=Ease((time-920)/370)*(1-Ease((time-1390)/304));
   if(rise<=0)return;
   float spread=31+28*Ease((time-1040)/624);
   Glow(mesh,feet+new Vector2(0,-19),spread,30,Alpha(green,.25f*rise),Alpha(green,0));
   Arc(mesh,feet,spread,6+11*rise,0,Mathf.PI*2,0,Alpha(ivory,.76f*rise),2.2f);
   for(int i=0;i<10;i++){
    float t=Mathf.Repeat((time-860)/560+i*.327f,1);
    var point=feet+new Vector2(Mathf.Sin(i*2.39f)*spread*(.62f+.22f*t),-t*61);
    Line(mesh,point,point+new Vector2(.6f,-4-9*t),1.7f,Alpha(i%3==0?ivory:green,rise*(1-t)));
   }
  }
 }
}
