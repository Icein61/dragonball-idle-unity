using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  bool storyMode=true,saiyanPool=true;
  readonly Dictionary<string,Texture2D> portraits=new Dictionary<string,Texture2D>();
  static string PortraitKey(int index){return "V051/"+Data.Cards[index].Id+"-portrait";}
  Texture2D PortraitTexture(int index){return LoadCharacterTexture(PortraitKey(index));}
  Texture2D FighterTexture(int index){return LoadCharacterTexture("V050/"+Data.Cards[index].Id+"-atlas");}
  Texture2D LoadCharacterTexture(string key){
   if(!portraits.ContainsKey(key))portraits[key]=Resources.Load<Texture2D>("Art/"+key);
   return portraits[key];
  }
  void DrawChapterSelection(){
   int chosen=storyMode?profile.StorySelected:profile.SelectedStage;
   Label(224,112,900,44,storyMode?"镜像修炼":"试炼挑战",30,InkText);
   Label(226,162,1280,45,storyMode?Data.StoryBriefs[chosen]:"试炼独立解锁；可反复刷取装备、技能残页与珠子。",17,InkText);
   if(InkGuiButton(1160,115,190,36,"剧情章节",enabled:!running,selected:storyMode)&&!storyMode){storyMode=true;battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
   if(InkGuiButton(1365,115,190,36,"素材试炼",enabled:!running,selected:!storyMode)&&storyMode){storyMode=false;battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
   for(int i=0;i<4;i++){
    int stage=i;bool unlocked=i<=(storyMode?profile.StoryUnlocked:profile.Unlocked);
    string title=(i+1).ToString("00")+"  "+(storyMode?Data.StoryStages[i]:Data.Stages[i])+(unlocked?(storyMode&&profile.StoryCleared[i]?" · 已通关":""):" · 未解锁");
    if(InkGuiButton(224+i*337,214,321,42,title,enabled:!running&&unlocked&&saveError=="",selected:chosen==i)){
     if(Change(()=>{if(storyMode)profile.StorySelected=stage;else profile.SelectedStage=stage;})){battle=null;last=null;settled=false;autoRepeat=false;log.Clear();}
    }
   }
   Label(226,259,1335,21,storyMode?Data.StoryLoot[chosen]:Data.LootDescriptions[chosen],13,InkText);
  }

 }
}
