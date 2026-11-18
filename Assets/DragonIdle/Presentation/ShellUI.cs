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
  static readonly string[] ShellKeys={"loading-scene","loading-track","loading-fill","loading-goku","home-scene","button-primary","button-secondary","currency-material","currency-crystal","currency-gem","settings"};
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
  UI.Button ShellButton(Transform parent,string name,string label,float x,float y,float w,float h,Action action,bool primary=false,bool enabled=true){
   var b=UButton(parent,name,label,x,y,w,h,action,primary,enabled);var sprite=ShellSprite(primary?"button-primary":"button-secondary");
   if(sprite!=null){var image=(UI.Image)b.targetGraphic;image.sprite=sprite;image.type=UI.Image.Type.Sliced;image.color=Color.white;b.GetComponentInChildren<UI.Text>().color=primary?uiPaper:Color.white;}
   return b;
  }
  void DrawHome(){
   galleryDirty=false;
   string focus=null;var selected=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   if(selected!=null&&homeRoot!=null&&selected.transform.IsChildOf(homeRoot))focus=selected.name;
   if(homeRoot==null){homeRoot=UPanel(galleryRoot,"Home",0,0,1600,900,new Color(.22f,.46f,.70f),true);homeRoot.GetComponent<UI.Image>().sprite=null;}else ClearUI(homeRoot);
   ShellScene(homeRoot,"HomeIsland","home-scene",0,80,1600,739);
   UPanel(homeRoot,"HomeTopbar",0,0,1600,90,new Color(.985f,.98f,.96f));
   UText(homeRoot,"HomeBrand",Data.GameTitle,40,21,430,42,31,uiPaper);
   UText(homeRoot,"HomeSubtitle","龟仙屋 · 修行之旅",470,34,350,28,18,uiDim);
   string[] currency={"currency-material","currency-crystal","currency-gem"};int[] values={profile.Materials,profile.Crystals,profile.Gems};string[] labels={"素材","晶石","珠子"};
   for(int n=0;n<3;n++){float x=878+n*202;ShellPicture(homeRoot,"HomeCurrencyIcon"+n,currency[n],x,25,36,36);UText(homeRoot,"HomeCurrency"+n,labels[n]+" "+values[n],x+44,28,154,32,18,uiPaper);}
   var collection=UPanel(homeRoot,"HomeCollection",40,132,380,116,new Color(1,1,1,.94f));
   UText(collection,"HomeGreeting","今天也继续修行吧",20,15,340,36,26,uiPaper);
   UText(collection,"HomeCollectionCount","已收藏 "+profile.Owned.Count(x=>x)+" / "+Data.Cards.Length+"  ·  上阵 "+profile.Team.Length+" 人",20,63,340,28,18,uiDim);
   var next=UPanel(homeRoot,"HomeJourney",1200,152,344,312,new Color(1,1,1,.96f));
   int chapter=Math.Min(profile.StoryUnlocked,Data.StoryStages.Length-1);
   UText(next,"HomeJourneyLabel","下一场冒险",22,18,300,28,18,uiAccent);
   UText(next,"HomeChapter",Data.StoryStages[chapter],22,62,300,42,26,uiPaper);
   UText(next,"HomeChapterBrief",Data.StoryBriefs[chapter],22,120,300,90,18,uiDim);
   ShellButton(next,"HomeContinue","前往剧情",18,232,308,62,()=>{
    if(Change(()=>profile.StorySelected=chapter)){storyMode=true;battle=null;settled=false;ClearBattlePresentation();NavigateGallery("挑战");}else GalleryNotice(notice,false);
   },true,saveError=="");
   var bank=UPanel(homeRoot,"HomeBank",1200,484,344,154,new Color(1,1,1,.96f));
   UText(bank,"HomeBankTitle","离线积累",20,15,300,28,21,uiPaper);
   string bankLabel=profile.OfflineBank>0?"领取 "+profile.OfflineBank+" 素材":profile.Wins==0?"首胜后开启积累":"暂无待领取素材";
   ShellButton(bank,"HomeClaim",bankLabel,18,65,308,62,()=>{
    if(Change(()=>{if(profile.OfflineBank<=0)throw new InvalidOperationException("暂无可领取素材");profile.Materials+=profile.OfflineBank;profile.OfflineBank=0;}))GalleryNotice("离线素材已存入背包");else GalleryNotice(notice,false);
    DrawHome();
   },false,profile.OfflineBank>0&&saveError=="");
   ShellButton(homeRoot,"HomeSettings","声音与显示",1200,660,344,62,ShowAudioSettings);
   ShellPicture(homeRoot,"HomeSettingsIcon","settings",1219,679,25,25);
   var toast=UPanel(homeRoot,"HomeNoticePanel",40,694,1100,52,new Color(.045f,.09f,.14f,.82f));
   homeNotice=UText(toast,"HomeNotice","",18,8,1064,40,18,Color.white);
   UPanel(homeRoot,"HomeDock",0,766,1600,134,new Color(.985f,.98f,.96f));
   string[] names={"角色","编队","养成","装备","召唤","说明"},titles={"人物志","队伍编成","角色养成","装备工坊","时空召唤","帮助与记录"};
   string[] help={"收藏与招式","阵位与队长","修炼与策略","搭配与比较","寻找新同伴","玩法与存档"};
   for(int n=0;n<names.Length;n++){string target=names[n];float x=40+n*256;ShellButton(homeRoot,"HomeNav"+target,titles[n],x,786,240,62,()=>NavigateGallery(target));UText(homeRoot,"HomeNavHint"+n,help[n],x,855,240,27,16,uiDim,TextAnchor.MiddleCenter);}
   RefreshHomeNotice();
   if(EventSystem.current!=null){var b=homeRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(x=>x.name==focus&&x.interactable);EventSystem.current.SetSelectedGameObject(b!=null?b.gameObject:homeRoot.Find("HomeNav角色").gameObject);}
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
    shellLoadingLabel=key=="home-scene"?"准备龟仙屋背景":key.Contains("button")?"准备界面控件":"准备界面资源";RefreshLoadingDisplay();
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
