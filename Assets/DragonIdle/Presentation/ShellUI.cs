using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  RectTransform galleryChrome,homeRoot,loadingRoot;
  readonly Dictionary<string,UnityEngine.Object> shellAssets=new Dictionary<string,UnityEngine.Object>();
  readonly MenuLoadProgress shellLoad=new MenuLoadProgress();
  static readonly string[] ShellKeys={"loading-scene","loading-track","loading-fill","loading-goku"};
  UI.Image loadingFill;UI.Text loadingStatus,loadingPercent,homeNotice;RectTransform loadingMarker;
  string shellLoadingLabel="";
  static bool IsShellPage(string p){return p=="主页"||p=="加载";}
  UnityEngine.Object ShellAsset(string key){
   UnityEngine.Object obj;if(shellAssets.TryGetValue(key,out obj))return obj;
   obj=key.EndsWith("-scene")?(UnityEngine.Object)Resources.Load<Texture2D>("Interface/V070/"+key):Resources.Load<Sprite>("Interface/V070/"+key);
   shellAssets[key]=obj;return obj;
  }
  Sprite ShellSprite(string key){return ShellAsset(key) as Sprite;}
  RectTransform ShellPicture(Transform parent,string name,string key,float x,float y,float w,float h){
   var r=UNode(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<UI.Image>();image.sprite=ShellSprite(key);image.preserveAspect=true;image.raycastTarget=false;
   if(image.sprite==null)image.color=Color.clear;return r;
  }
  void ShellScene(Transform parent,string name,string key,float x,float y,float w,float h){
   var texture=ShellAsset(key) as Texture2D;if(texture==null)return;
   float scale=Math.Min(w/texture.width,h/texture.height),width=texture.width*scale,height=texture.height*scale;
   var r=UNode(parent,name,x+(w-width)/2,y+(h-height)/2,width,height);var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=texture;image.uvRect=new Rect(0,0,1,1);image.raycastTarget=false;
  }
  void DrawHome(){
   galleryDirty=false;
   string focus=null;var selected=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   if(selected!=null&&homeRoot!=null&&selected.transform.IsChildOf(homeRoot))focus=selected.name;
   if(homeRoot==null)homeRoot=InkPanel(galleryRoot,"Home",0,0,1600,900,InkMist,true);else ClearUI(homeRoot);
   InkPanel(homeRoot,"HomeTopbar",0,0,1600,90,InkDark,true);
   UText(homeRoot,"HomeBrand",Data.GameTitle,40,19,500,54,33,InkIvory);
   UText(homeRoot,"HomeResources","素材  "+profile.Materials+"     晶石  "+profile.Crystals+"     珠子  "+profile.Gems,
    820,21,722,46,20,InkOnDark,TextAnchor.MiddleRight);
   var progress=InkPanel(homeRoot,"HomeProgress",40,148,690,504,InkPaper);
   UText(progress,"HomeProgressHeading","修炼进度",40,32,580,55,32,InkText);
   int cleared=profile.StoryCleared.Count(done=>done);
   InkHomeRing(progress,46,122,216,cleared,Data.StoryStages.Length);
   UText(progress,"HomeStoryProgress","剧情已通关",58,348,192,33,19,InkMuted,TextAnchor.MiddleCenter);
   for(int stage=0;stage<Data.StoryStages.Length;stage++){
    bool done=profile.StoryCleared[stage], open=stage<=profile.StoryUnlocked;
    int y=119+stage*60;
    InkPanel(progress,"HomeChapterNode"+stage,302,y,347,46,done||open?InkDark:InkLight);
    UText(progress,"HomeChapterState"+stage,Data.StoryStages[stage]+" · "+(done?"已通关":open?"可挑战":"未解锁"),
     318,y+4,315,39,17,done||open?InkIvory:InkMuted,TextAnchor.MiddleLeft);
   }
   UText(progress,"HomeCollectionCount","已收藏 "+profile.Owned.Count(x=>x)+" / "+Data.Cards.Length+"   ·   上阵 "+profile.Team.Length+" 人",
    40,431,610,34,18,InkMuted);
   var next=InkPanel(homeRoot,"HomeJourney",758,148,802,249,InkDark);
   int chapter=Math.Min(profile.StoryUnlocked,Data.StoryStages.Length-1);
   UText(next,"HomeJourneyLabel","继续剧情",26,22,420,29,17,InkOnDark);
   UText(next,"HomeChapter",Data.StoryStages[chapter],26,59,740,53,32,InkIvory);
   UText(next,"HomeChapterBrief",Data.StoryBriefs[chapter],26,118,739,51,19,InkOnDark);
   InkButton(next,"HomeContinue","前往剧情",25,179,310,48,()=>{
    if(Change(()=>profile.StorySelected=chapter)){storyMode=true;battle=null;settled=false;ClearBattlePresentation();NavigateGallery("挑战");}else GalleryNotice(notice,false);
   },true,saveError=="");
   var bank=InkPanel(homeRoot,"HomeBank",758,422,802,150,InkPaper);
   UText(bank,"HomeBankTitle","离线修炼",25,21,310,32,22,InkText);
   UText(bank,"HomeBankCount","待领取素材  "+profile.OfflineBank,25,69,400,37,20,InkMuted);
   string bankLabel=profile.OfflineBank>0?"领取 "+profile.OfflineBank+" 素材":profile.Wins==0?"首胜后开启积累":"暂无待领取素材";
   InkButton(bank,"HomeClaim",bankLabel,448,57,329,51,()=>{
    if(Change(()=>{if(profile.OfflineBank<=0)throw new InvalidOperationException("暂无可领取素材");profile.Materials+=profile.OfflineBank;profile.OfflineBank=0;}))GalleryNotice("离线素材已存入背包");else GalleryNotice(notice,false);
    DrawHome();
   },false,profile.OfflineBank>0&&saveError=="");
   InkButton(homeRoot,"HomeSettings","声音与显示",758,600,310,52,ShowAudioSettings);
   var toast=InkPanel(homeRoot,"HomeNoticePanel",40,684,1520,55,InkDark);
   homeNotice=UText(toast,"HomeNotice","",18,7,1484,44,18,InkIvory);
   InkPanel(homeRoot,"HomeDock",0,766,1600,134,InkPaper,true);
   string[] names={"角色","编队","养成","装备","召唤","说明"},titles={"人物志","队伍编成","角色养成","装备工坊","时空召唤","帮助与记录"};
   string[] help={"收藏与招式","阵位与队长","修炼与策略","搭配与比较","寻找新同伴","玩法与存档"};
   for(int n=0;n<names.Length;n++){string target=names[n];float x=40+n*256;InkButton(homeRoot,"HomeNav"+target,titles[n],x,786,240,62,()=>NavigateGallery(target));UText(homeRoot,"HomeNavHint"+n,help[n],x,855,240,27,16,InkMuted,TextAnchor.MiddleCenter);}
   RefreshHomeNotice();
   if(EventSystem.current!=null){var b=homeRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(x=>x.name==focus&&x.interactable);EventSystem.current.SetSelectedGameObject(b!=null?b.gameObject:ActiveBackButton(homeRoot,"HomeNav角色"));}
  }
  void RefreshHomeNotice(){if(homeNotice!=null){homeNotice.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"从人物志了解同伴，或前往剧情开始冒险。";}}
  void DrawShellLoading(){
   if(loadingRoot==null){loadingRoot=UPanel(galleryRoot,"Loading",0,0,1600,900,new Color(.025f,.055f,.085f),true);loadingRoot.GetComponent<UI.Image>().sprite=null;}else ClearUI(loadingRoot);
   ShellScene(loadingRoot,"LoadingScene","loading-scene",0,0,1600,900);
   UText(loadingRoot,"LoadingBrand",Data.GameTitle,96,122,1000,80,52,Color.white);
   UText(loadingRoot,"LoadingCaption","新的修行，从这里开始",100,212,1000,40,24,new Color(.80f,.88f,.95f));
   UPanel(loadingRoot,"LoadingFooter",0,638,1600,262,new Color(.02f,.04f,.07f,.85f));
   ShellPicture(loadingRoot,"LoadingTrack","loading-track",184,650,1232,110);
   var fill=ShellPicture(loadingRoot,"LoadingFill","loading-fill",210,696,1180,19);loadingFill=fill.GetComponent<UI.Image>();loadingFill.preserveAspect=false;loadingFill.type=UI.Image.Type.Filled;loadingFill.fillMethod=UI.Image.FillMethod.Horizontal;loadingFill.fillOrigin=0;
   if(loadingFill.sprite==null){loadingFill.sprite=RoundedSurface(false);loadingFill.color=uiAccent;}
   loadingMarker=ShellPicture(loadingRoot,"LoadingMarker","loading-goku",190,646,104,49);
   loadingStatus=UText(loadingRoot,"LoadingStatus","",210,742,1030,36,20,Color.white);
   loadingPercent=UText(loadingRoot,"LoadingPercent","",1230,742,160,36,20,Color.white,TextAnchor.UpperRight);
   UText(loadingRoot,"LoadingTip","普通攻击与绝招推进正常行动；必杀是消耗怒气的额外行动。",210,801,1180,40,18,new Color(.79f,.85f,.92f));
   RefreshLoadingDisplay();
  }
  void RefreshLoadingDisplay(){
   if(loadingFill==null)return;loadingFill.fillAmount=(float)shellLoad.Fraction;
   loadingMarker.anchoredPosition=new Vector2(190+(float)shellLoad.Fraction*1100,-646);
   loadingStatus.text=shellLoadingLabel+"  "+shellLoad.Completed+" / "+shellLoad.Total;
   loadingPercent.text=Mathf.RoundToInt((float)shellLoad.Fraction*100)+"%";
  }
  void BeginShellLoad(string target){
   EnsureGallery();int generation=shellLoad.Begin(ShellKeys.Length);
   page="加载";CloseGalleryDialog();DrawShellLoading();RefreshModernFrame();galleryCanvas.SetActive(true);previousPage=page;
   StartCoroutine(LoadShellResources(generation,target));
  }
  IEnumerator LoadShellResources(int generation,string target){
   foreach(string key in ShellKeys){
    if(!shellLoad.IsCurrent(generation))yield break;
    shellLoadingLabel="准备界面资源";RefreshLoadingDisplay();
    ResourceRequest request=key.EndsWith("-scene")?Resources.LoadAsync<Texture2D>("Interface/V070/"+key):Resources.LoadAsync<Sprite>("Interface/V070/"+key);
    while(!request.isDone){if(!shellLoad.IsCurrent(generation))yield break;yield return null;}
    if(!shellLoad.IsCurrent(generation))yield break;
    shellAssets[key]=request.asset;shellLoad.Advance(generation,request.asset==null);RefreshLoadingDisplay();
    yield return null; // One actual completed work unit; no minimum display-time or fake percentage.
   }
   if(!shellLoad.Finish(generation))yield break;
   int missing=shellLoad.Missing;NavigateGallery(target);
   if(missing>0)GalleryNotice("部分界面素材未能载入，已使用基础控件。",false);
  }
 }
}
