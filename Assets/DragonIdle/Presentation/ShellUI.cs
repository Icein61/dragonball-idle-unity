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
   var selected=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   string focus=selected!=null&&homeRoot!=null&&selected.transform.IsChildOf(homeRoot)?selected.name:null;
   if(homeRoot==null)homeRoot=InkPanel(galleryRoot,"Home",0,0,1600,900,InkMist,true);else ClearUI(homeRoot);
   InkPanel(homeRoot,"HomeTopbar",0,0,1600,90,InkDark,true);
   UText(homeRoot,"HomeBrand",Data.GameTitle,40,19,500,54,33,InkIvory);
   UText(homeRoot,"HomeVersion","v"+Data.Version,1210,24,328,42,20,InkOnDark,TextAnchor.MiddleRight);
   var summary=InkPanel(homeRoot,"HomeRoundSummary",58,160,1484,337,InkPaper);
   UText(summary,"HomeRoundHeading","整备出战",45,40,720,66,44,InkText);
   UText(summary,"HomeRoundHelp","在编队安排己方阵位，进入战斗 Demo 挑战随机五人。",48,136,1300,54,22,InkMuted);
   UText(summary,"HomeRoundCount","已拥有 "+RoundOwnedCount+"/"+Data.RoundRoster.Count,48,228,1100,50,23,InkText);
   UButton(homeRoot,"HomeNav角色","角色",58,541,474,115,()=>OpenFeature("角色"));
   UButton(homeRoot,"HomeNav"+FormationRoute,FormationRoute,563,541,474,115,()=>OpenFeature(FormationRoute));
   UButton(homeRoot,"HomeNav"+DemoBattleRoute,DemoBattleRoute,1068,541,474,115,BeginRoundDemo,primary:true);
   UButton(homeRoot,"HomeSettings","声音与显示",58,718,240,44,ShowAudioSettings);
   homeNotice=UText(homeRoot,"HomeNotice","",58,797,1484,47,18,InkText);
   RefreshHomeNotice();
   if(EventSystem.current!=null){var b=homeRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(x=>x.name==focus&&x.interactable);EventSystem.current.SetSelectedGameObject(b!=null?b.gameObject:ActiveBackButton(homeRoot,"HomeNav角色"));}
  }
  void RefreshHomeNotice(){if(homeNotice!=null)homeNotice.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"";}
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
  int RoundOwnedCount => Data.RoundRoster.Count(id=>profile.Owned[Data.Index(id)]);
  string[] StartupResourcePaths(){
   return Data.Rarities.Select(RarityResource)
    .Concat(Data.RoundRoster.Select(id=>"Art/"+PortraitKey(Data.Index(id))))
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
    if(badge)rarityBadges[path.Substring(path.LastIndexOf('/')+1)]=request.asset as Sprite;
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
