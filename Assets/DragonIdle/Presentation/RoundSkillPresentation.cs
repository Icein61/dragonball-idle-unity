using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  const float SkillBadgeWidth=110,SkillBadgeHeight=25,MaximumRoundZoom=.075f;
  RectTransform roundFocusWorld,roundSkillBadge;
  Image roundCutShade;
  Text roundSkillText;
  RawImage roundSkillArt;

  Image CreateRoundCutShade(Transform world) {
   var shade=UPanel(world,"UltimateShade",0,0,RoundMotionFx.Width,RoundMotionFx.Height,Color.clear).GetComponent<Image>();
   shade.raycastTarget=false;
   return shade;
  }

  RectTransform CreateRoundSkillBadge(Transform viewport,RoundMotionPlayback run) {
   var badge=UPanel(viewport,"BattleSkillBadge",0,0,SkillBadgeWidth,SkillBadgeHeight,RoundMeterPaper);
   RoundMeterCorners(badge,8);
   roundSkillArt=RoundPoseImage(badge,"SkillArtwork");
   roundSkillArt.rectTransform.anchoredPosition=new Vector2(3,-2.5f);
   roundSkillArt.rectTransform.sizeDelta=new Vector2(20,20);
   var icon=run?.Opening?.icon??run?.Action?.Ability?.icon;
   if(run?.IsUltimate==true||run?.IsCharge==true)icon=RoundRules().For(run.Actor.CardId).ultimateIcon;
   else if(run?.Clip?.id=="tienVolley"||run?.Clip?.id=="yamchaBasic"||run?.Clip?.id=="gohanRally"||run?.Clip?.id=="krillinBasic"||run?.IsRaditzEnergy==true&&!run.IsUltimate)icon=RoundRules().For(run.Actor.CardId).basicIcon;
   else if(run?.Clip?.id=="tienSet"||run?.Clip?.id=="yamchaSweep"||run?.Clip?.id=="gohanFlash"||run?.IsScatter==true)icon=RoundRules().For(run.Actor.CardId).pursuitIcon;
   BindRoundSkillIcon(roundSkillArt,icon);
   bool hasIcon=roundSkillArt.texture!=null;
   roundSkillArt.gameObject.SetActive(hasIcon);
   float left=hasIcon?27:4;
   roundSkillText=UText(badge,"SkillName",run?.SkillName??"",left,0,SkillBadgeWidth-left-4,SkillBadgeHeight,
    13,InkText,hasIcon?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
   roundSkillText.resizeTextForBestFit=true;
   roundSkillText.resizeTextMaxSize=13;
   roundSkillText.resizeTextMinSize=10;
   badge.gameObject.SetActive(false);
   return badge;
  }

  static void BindRoundSkillIcon(RawImage image,RoundSkillIcon icon) {
   if(string.IsNullOrEmpty(icon?.resource))return;
   var texture=Resources.Load<Texture2D>(icon.resource);
   if(texture==null)throw new System.InvalidOperationException("Missing round skill icon: "+icon.resource);
   image.texture=texture;
   var region=icon.region;
   bool hasRegion=region!=null&&region.Length>0;
   if(hasRegion&&(region.Length!=4||region[0]<0||region[1]<0||region[2]<=0||region[3]<=0||
      region[2]>texture.width-region[0]||region[3]>texture.height-region[1]))
    throw new System.InvalidOperationException("Bad round skill icon crop: "+icon.resource);
   int sizeX=hasRegion?region[2]:texture.width;
   int sizeY=hasRegion?region[3]:texture.height;
   float side=Mathf.Min(sizeX,sizeY);
   float left=(hasRegion?region[0]:0)+(sizeX-side)*.5f;
   float top=(hasRegion?region[1]:0)+(sizeY-side)*.5f;
   image.uvRect=new Rect(left/texture.width,1-(top+side)/texture.height,side/texture.width,side/texture.height);
  }

  void PaintRoundSkillPresentation(RectTransform world,RawImage actor,RoundMotionPlayback run,Vector2 anchor,Vector2? guardAnchor=null) {
   bool opening=run?.Opening!=null;
   float flash=opening&&run.Time<RoundMotionPlayback.OpeningFlash?
    run.Time/RoundMotionPlayback.OpeningFlash:run?.FlashTime??-1;
   PaintRoundSheen(actor,flash,opening);

   float guard=run?.CounterGuardWeight??0;
   float shade=Mathf.Max(run?.ReleaseShade??0,guard*.45f);
   roundCutShade.color=new Color(.07f,.12f,.18f,shade*.44f);
   roundCutShade.gameObject.SetActive(shade>0);
   float zoom=Mathf.Max(run?.CameraWeight??0,guard)*MaximumRoundZoom;
   var pivot=Vector2.Lerp(anchor,guardAnchor??anchor,guard)+new Vector2(0,roundDemo?-54:-125);
   world.localScale=new Vector3(1+zoom,1+zoom,1);
   world.anchoredPosition=new Vector2(-pivot.x*zoom,pivot.y*zoom);

   bool visible=run?.SkillName!=null&&(opening||run.IsCharge||run.Action?.Ability!=null||run.Clip?.id=="gohanRally"||run.Time>=run.ReleaseAt-120&&run.Time<run.ImpactAt+430);
   roundSkillBadge.gameObject.SetActive(visible);
   if(!visible)return;
   // The badge lives in the viewport; sprite poses and the world camera use separate transforms.
   float gap=roundDemo?49:111;
   float outward=run.Actor.Side==0?anchor.x-SkillBadgeWidth-gap:anchor.x+gap;
   roundSkillBadge.anchoredPosition=new Vector2(
    Mathf.Clamp(outward,8,RoundMotionFx.Width-SkillBadgeWidth-8),
    -Mathf.Clamp(anchor.y-(roundDemo?47:88),8,RoundMotionFx.Height-SkillBadgeHeight-8));
  }

  void DrawRoundAtlasActor(RawImage image,RoundFighter fighter,Vector2 at,int frame,float scale) {
   int card=Data.Index(fighter.CardId);
   var region=Regions(card).frames[frame];
   image.texture=FighterTexture(card);
   image.uvRect=ArtUV(card,frame,fighter.Side==1);
   image.rectTransform.sizeDelta=new Vector2(region.w*scale,region.h*scale);
   image.rectTransform.anchoredPosition=new Vector2(at.x-region.w*scale*.5f,-at.y+region.h*scale);
  }
 }
}
