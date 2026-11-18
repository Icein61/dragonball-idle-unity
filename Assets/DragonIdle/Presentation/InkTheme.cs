using System;
using UnityEngine;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  static readonly Color InkPaper=new Color32(222,217,200,255);
  static readonly Color InkLight=new Color32(234,229,214,255);
  static readonly Color InkMist=new Color32(172,181,168,255);
  static readonly Color InkDark=new Color32(38,56,55,255);
  static readonly Color InkText=new Color32(35,49,54,255);
  static readonly Color InkIvory=new Color32(242,236,221,255);
  static readonly Color InkMuted=new Color32(78,90,84,255);
  static readonly Color InkOnDark=new Color32(201,208,203,255);
  static readonly Color InkGold=new Color32(185,149,97,255);
  static readonly Color InkWarmText=new Color32(37,29,20,255);
  static readonly Color InkDisabled=new Color32(197,194,180,255);
  static readonly Color InkBorder=new Color32(140,138,116,255);
  static readonly Color InkHealth=new Color32(83,128,96,255);
  static readonly Color InkLowHealth=new Color32(158,70,57,255);
  static readonly Color InkCharge=new Color32(128,90,48,255);
  const float InkRadius=7;
  Sprite inkRing;
  GUIStyle inkGuiButton;

  RectTransform InkPanel(Transform parent,string name,float x,float y,float w,float h,Color color,bool square=false) {
   var panel=UPanel(parent,name,x,y,w,h,color);
   var image=panel.GetComponent<UI.Image>();
   if(square)image.sprite=null;
   else {
    image.sprite=RoundedSurface(false);
    image.type=UI.Image.Type.Sliced;
    image.pixelsPerUnitMultiplier=2;
   }
   return panel;
  }

  UI.Button InkButton(Transform parent,string name,string label,float x,float y,float w,float h,Action click,bool primary=false,bool enabled=true) {
   var control=UButton(parent,name,label,x,y,w,h,click,primary,enabled);
   var image=(UI.Image)control.targetGraphic;
   image.pixelsPerUnitMultiplier=2;
   image.color=!enabled?InkDisabled:primary?InkGold:InkLight;
   control.GetComponentInChildren<UI.Text>().color=primary&&enabled?InkWarmText:InkText;
   control.GetComponent<UI.Outline>().effectColor=InkCharge;
   var colors=control.colors;
   colors.highlightedColor=new Color(.96f,.96f,.96f);
   colors.selectedColor=new Color(.95f,.95f,.95f);
   colors.disabledColor=Color.white;
   control.colors=colors;
   return control;
  }

  Sprite InkRingSprite() {
   if(inkRing!=null)return inkRing;
   const int size=96;
   var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
    float radius=new Vector2(x+.5f-size/2,y+.5f-size/2).magnitude;
    byte opacity=(byte)Mathf.RoundToInt(255*Mathf.Clamp01(Mathf.Min(45-radius,radius-38)));
    pixels[y*size+x]=new Color32(255,255,255,opacity);
   }
   var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="InkProgressRing",wrapMode=TextureWrapMode.Clamp};
   texture.SetPixels32(pixels);
   texture.Apply(false,true);
   inkRing=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
   return inkRing;
  }

  void InkHomeRing(Transform parent,float x,float y,float size,int cleared,int total) {
   foreach(string name in new[]{"HomeProgressTrack","HomeProgressFill"}) {
    var graphic=UNode(parent,name,x,y,size,size).gameObject.AddComponent<UI.Image>();
    graphic.sprite=InkRingSprite();
    graphic.raycastTarget=false;
    graphic.color=name=="HomeProgressTrack"?InkBorder:InkCharge;
    if(name=="HomeProgressFill") {
     graphic.type=UI.Image.Type.Filled;
     graphic.fillMethod=UI.Image.FillMethod.Radial360;
     graphic.fillOrigin=(int)UI.Image.Origin360.Top;
     graphic.fillAmount=total>0?(float)cleared/total:0;
    }
   }
   UText(parent,"HomeProgressCount",cleared+" / "+total,x,y,size,size,28,InkText,TextAnchor.MiddleCenter);
  }

  void InkGuiSurface(float x,float y,float w,float h,Color fill) {
   GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,fill,0,InkRadius);
  }

  bool InkGuiButton(float x,float y,float w,float h,string caption,bool primary=false,bool enabled=true,bool selected=false) {
   var bounds=new Rect(x,y,w,h);
   bool acceptsInput=enabled&&GUI.enabled;
   var fill=!acceptsInput?InkDisabled:primary?InkGold:selected?InkDark:InkLight;
   var foreground=acceptsInput&&selected&&!primary?InkIvory:primary&&acceptsInput?InkWarmText:InkText;
   if(Event.current.type==EventType.Repaint) {
    InkGuiSurface(x,y,w,h,InkBorder);
    InkGuiSurface(x+1,y+1,w-2,h-2,fill);
    if(acceptsInput&&bounds.Contains(Event.current.mousePosition))
     InkGuiSurface(x+1,y+1,w-2,h-2,new Color(0,0,0,.08f));
   }
   if(inkGuiButton==null)inkGuiButton=new GUIStyle(GUIStyle.none){font=font,fontSize=17,alignment=TextAnchor.MiddleCenter};
   inkGuiButton.normal.textColor=inkGuiButton.hover.textColor=inkGuiButton.active.textColor=inkGuiButton.focused.textColor=foreground;
   bool prior=GUI.enabled;
   GUI.enabled=acceptsInput;
   bool clicked=GUI.Button(bounds,caption,inkGuiButton);
   GUI.enabled=prior;
   if(clicked)PlayUISound(0);
   return clicked;
  }

  void InkGuiMeter(float x,float y,float width,float height,double fraction,Color color,Color track) {
   Box(x,y,width,height,track);
   if(fraction>0)Box(x,y,width*(float)Math.Min(1,fraction),height,color);
  }
 }
}
