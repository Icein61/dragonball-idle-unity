using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  bool storyMode=true,saiyanPool=true;
  readonly Dictionary<string,Texture2D> portraits=new Dictionary<string,Texture2D>();
  Texture2D PortraitTexture(int index){
   string key=index==0?"goku-raditz-c-v030":Data.Cards[index].Id=="piccolo-raditz-v030"?"piccolo-raditz-c-v030":Data.Cards[index].Id=="raditz-arrival-v030"?"raditz-c-v030":null;
   if(key==null)return null;
   if(!portraits.ContainsKey(key))portraits[key]=Resources.Load<Texture2D>("Art/"+key);
   return portraits[key];
  }
  void DrawChapterSelection(){
   int chosen=storyMode?profile.StorySelected:profile.SelectedStage;
   Heading(storyMode?"拉蒂兹来袭":"试炼挑战",storyMode?Data.StoryBriefs[chosen]:"试炼独立解锁；可反复刷取装备、技能残页与珠子。");
   if(Button(1160,115,190,36,"剧情章节",storyMode,!running)&&!storyMode){storyMode=true;battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
   if(Button(1365,115,190,36,"素材试炼",!storyMode,!running)&&storyMode){storyMode=false;battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
   for(int i=0;i<4;i++){
    int stage=i;bool unlocked=i<=(storyMode?profile.StoryUnlocked:profile.Unlocked);
    string title=(i+1).ToString("00")+"  "+(storyMode?Data.StoryStages[i]:Data.Stages[i])+(unlocked?(storyMode&&profile.StoryCleared[i]?" · 已通关":""):" · 未解锁");
    if(Button(224+i*337,214,321,42,title,chosen==i,!running&&unlocked)){
     if(Change(()=>{if(storyMode)profile.StorySelected=stage;else profile.SelectedStage=stage;})){battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
    }
   }
   Label(226,259,1335,21,storyMode?Data.StoryLoot[chosen]:Data.LootDescriptions[chosen],13,muted);
  }
  void DrawActionPortrait(){
   if(last==null||last.Move==Move.Basic||last.Move==Move.Stunned||fx<=.12f)return;
   int i=Data.Index(last.Actor.CardId);if(i<0)return;var art=PortraitTexture(i);if(art==null)return;
   float offset=(1-fx)*9;
   Box(650,334,215,332,new Color(.035f,.055f,.09f,.94f));
   Box(650,334,215,3,last.Move==Move.Ultimate?gold:CardColor(i));
   GUI.color=new Color(1,1,1,Math.Min(1,fx*3));
   GUI.DrawTexture(new Rect(660+offset,346,190,263),art,ScaleMode.ScaleToFit,true);GUI.color=Color.white;
   Label(665,610,185,50,last.Move==Move.Ultimate?last.Actor.UltimateName:last.Actor.SpecialName,19,gold);
  }
 }
}
