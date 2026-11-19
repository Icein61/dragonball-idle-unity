using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  RectTransform galleryChrome,homeRoot,loadingRoot;
  readonly MenuLoadProgress shellLoad=new MenuLoadProgress();
  UI.Image loadingFill;
  UI.Text loadingStatus,loadingPercent,loadingCount,homeNotice;
  static bool IsShellPage(string p){return p=="主页"||p=="加载";}
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
   UText(progress,"HomeCollectionCount","已收藏 "+profile.Owned.Count(x=>x)+" / "+Data.Cards.Length+"   ·   上阵 "+profile.TeamCount+" / "+Data.TeamSlots,
    40,431,610,34,18,InkMuted);
   var next=InkPanel(homeRoot,"HomeJourney",758,148,802,249,InkDark);
   int chapter=Math.Min(profile.StoryUnlocked,Data.StoryStages.Length-1);
   UText(next,"HomeJourneyLabel","继续剧情",26,22,420,29,17,InkOnDark);
   UText(next,"HomeChapter",Data.StoryStages[chapter],26,59,740,53,32,InkIvory);
   UText(next,"HomeChapterBrief",Data.StoryBriefs[chapter],26,118,739,51,19,InkOnDark);
   UButton(next,"HomeContinue","前往剧情",25,179,310,48,()=>{
    if(Change(()=>profile.StorySelected=chapter)){storyMode=true;battle=null;settled=false;ClearBattlePresentation();NavigateGallery("挑战");}else GalleryNotice(notice,false);
   },true,saveError=="");
   var bank=InkPanel(homeRoot,"HomeBank",758,422,802,150,InkPaper);
   UText(bank,"HomeBankTitle","离线修炼",25,21,310,32,22,InkText);
   UText(bank,"HomeBankCount","待领取素材  "+profile.OfflineBank,25,69,400,37,20,InkMuted);
   string bankLabel=profile.OfflineBank>0?"领取 "+profile.OfflineBank+" 素材":profile.Wins==0?"首胜后开启积累":"暂无待领取素材";
   UButton(bank,"HomeClaim",bankLabel,448,57,329,51,()=>{
    if(Change(()=>{if(profile.OfflineBank<=0)throw new InvalidOperationException("暂无可领取素材");profile.Materials+=profile.OfflineBank;profile.OfflineBank=0;}))GalleryNotice("离线素材已存入背包");else GalleryNotice(notice,false);
    DrawHome();
   },false,profile.OfflineBank>0&&saveError=="");
   UButton(homeRoot,"HomeSettings","声音与显示",758,600,310,52,ShowAudioSettings);
   string firstWin=profile.FirstMission?"首胜奖励已领取":profile.Wins>0?"领取首胜奖励 · 50 晶石":"首胜后可领取 50 晶石";
   UButton(homeRoot,"HomeFirstWin",firstWin,1088,600,472,52,ClaimFirstWin,
    primary:profile.Wins>0&&!profile.FirstMission,enabled:profile.Wins>0&&!profile.FirstMission&&saveError=="");
   var toast=InkPanel(homeRoot,"HomeNoticePanel",40,684,1520,55,InkDark);
   homeNotice=UText(toast,"HomeNotice","",18,7,1484,44,18,InkIvory);
   InkPanel(homeRoot,"HomeDock",0,766,1600,134,InkPaper,true);
   string[] names={"角色","编队","养成","装备","召唤"},titles={"图鉴","队伍编成","角色养成","装备工坊","时空召唤"};
   string[] help={"收藏与招式","阵位与队长","修炼与策略","搭配与比较","寻找新同伴"};
   for(int n=0;n<names.Length;n++){
    string target=names[n];
    float x=40+n*308;
    UButton(homeRoot,"HomeNav"+target,titles[n],x,786,288,62,()=>NavigateGallery(target));
    UText(homeRoot,"HomeNavHint"+n,help[n],x,855,288,27,16,InkMuted,TextAnchor.MiddleCenter);
   }
   RefreshHomeNotice();
   if(EventSystem.current!=null){var b=homeRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(x=>x.name==focus&&x.interactable);EventSystem.current.SetSelectedGameObject(b!=null?b.gameObject:ActiveBackButton(homeRoot,"HomeNav角色"));}
  }
  void ClaimFirstWin(){
   if(profile.Wins==0||profile.FirstMission||saveError!="")return;
   if(Change(()=>{profile.Crystals+=50;profile.FirstMission=true;}))GalleryNotice("已领取首胜奖励 · 晶石 +50");
   else GalleryNotice(notice,false);
   DrawHome();
  }
  void RefreshHomeNotice(){if(homeNotice!=null){homeNotice.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"从图鉴了解同伴，或前往剧情开始冒险。";}}
  void DrawShellLoading(){
   if(loadingRoot==null)loadingRoot=InkPanel(galleryRoot,"Loading",0,0,1600,900,InkMist,true);
   else ClearUI(loadingRoot);
   InkPanel(loadingRoot,"LoadingHeader",0,0,1600,90,InkDark,true);
   UText(loadingRoot,"LoadingBrand",Data.GameTitle,40,19,500,54,33,InkIvory);
   UText(loadingRoot,"LoadingVersion","v"+Data.Version,1290,19,270,54,19,InkOnDark,TextAnchor.MiddleRight);

   var info=InkPanel(loadingRoot,"LoadingInfo",215,216,565,468,InkPaper);
   UText(info,"LoadingTitle","修炼准备",38,66,489,85,51,InkText);
   loadingStatus=UText(info,"LoadingStatus","",41,199,483,72,23,InkText);
   UPanel(info,"LoadingDivider",41,318,483,1,InkBorder);
   UText(info,"LoadingTip","必杀消耗怒气，在正常行动之外额外释放。",41,346,483,74,19,InkMuted);

   var charge=InkPanel(loadingRoot,"LoadingCharge",808,216,577,468,InkDark);
   loadingFill=InkRadialMeter(charge,"LoadingProgress",141,41,295,InkBorder,InkGold);
   loadingPercent=UText(charge,"LoadingPercent","",141,41,295,295,63,InkIvory,TextAnchor.MiddleCenter);
   loadingCount=UText(charge,"LoadingCount","",45,356,487,56,21,InkOnDark,TextAnchor.MiddleCenter);
   RefreshLoadingDisplay("准备资源");
  }
  void RefreshLoadingDisplay(string stage){
   loadingFill.fillAmount=(float)shellLoad.Fraction;
   loadingStatus.text=stage;
   loadingCount.text="已处理 "+shellLoad.Completed+" / "+shellLoad.Total+" 项资源";
   loadingPercent.text=(shellLoad.Completed==shellLoad.Total?100:(int)(shellLoad.Fraction*100))+"%";
  }
  string[] StartupResourcePaths(){
   return Data.Rarities.Select(rank=>RarityResourcePrefix+rank)
    .Concat(Enumerable.Range(0,Data.Cards.Length).Select(index=>"Art/"+PortraitKey(index)))
    .Distinct().ToArray();
  }
  void BeginShellLoad(string target){
   EnsureGallery();
   var paths=StartupResourcePaths();
   int generation=shellLoad.Begin(paths.Length);
   page="加载";CloseGalleryDialog();DrawShellLoading();RefreshModernFrame();galleryCanvas.SetActive(true);previousPage=page;
   StartCoroutine(LoadShellResources(generation,target,paths));
  }
  IEnumerator LoadShellResources(int generation,string target,string[] paths){
   yield return null;
   foreach(string path in paths){
    if(!shellLoad.IsCurrent(generation))yield break;
    bool badge=path.StartsWith(RarityResourcePrefix,StringComparison.Ordinal);
    string stage=badge?"准备稀有度徽章":"准备角色立绘";
    RefreshLoadingDisplay(stage);
    ResourceRequest request=badge?Resources.LoadAsync<Sprite>(path):Resources.LoadAsync<Texture2D>(path);
    yield return request;
    if(!shellLoad.IsCurrent(generation))yield break;
    if(badge)rarityBadges[path.Substring(RarityResourcePrefix.Length)]=request.asset as Sprite;
    else portraits[path.Substring("Art/".Length)]=request.asset as Texture2D;
    shellLoad.Advance(generation,request.asset==null);
    RefreshLoadingDisplay(stage);
   }
   if(!shellLoad.IsCurrent(generation))yield break;
   RefreshLoadingDisplay(shellLoad.Missing>0?"资源准备结束":"资源准备完成");
   yield return null; // Allow the completed state to render before changing pages.
   if(!shellLoad.Finish(generation))yield break;
   int missing=shellLoad.Missing;NavigateGallery(target);
   if(missing>0)GalleryNotice("部分美术资源未能载入，将显示占位内容。",false);
  }
 }
}
