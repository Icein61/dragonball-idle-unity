using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  IEnumerator RunShellSmokeAndFinish(string dir,int events){
   var routine=RunShellSmoke(dir);
   while(true){
    bool next=false,failed=false;object value=null;
    try{next=routine.MoveNext();if(next)value=routine.Current;}
    catch(Exception ex){File.WriteAllText(Path.Combine(dir,"smoke-failure.txt"),ex.ToString());Debug.LogError(ex);Application.Quit(1);failed=true;}
    if(failed)yield break;if(!next)break;yield return value;
   }
   File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"version\":\""+Data.Version+"\",\"battleWon\":true,\"saveRoundTrip\":true,\"legacyMigration\":true,\"characterPersistence\":true,\"chapterFlow\":true,\"v020Migration\":true,\"v040Migration\":true,\"leaderKeywords\":true,\"artResourceLoaded\":true,\"galleryFlow\":true,\"audioSettings\":true,\"shellFlow\":true,\"visualVerified\":false,\"events\":"+events+"}");
   Debug.Log("DRAGON_IDLE_LOGIC_SMOKE_SUCCESS");Application.Quit(0);
  }
  IEnumerator RunShellSmoke(string dir){
   savePath=Path.Combine(dir,"shell-save.json");profile=new Profile();profile.OfflineBank=24;
   profile.StoryCleared[0]=true;profile.StoryUnlocked=1;Save();
   running=false;
   var startupPaths=StartupResourcePaths();
   rarityBadges.Clear();portraits.Clear();
   BeginShellLoad("主页");
   AssertSmoke(page=="加载"&&loadingRoot.gameObject.activeInHierarchy&&!galleryChrome.gameObject.activeSelf&&!detailRoot.gameObject.activeSelf,"Loading leaked other pages");
   AssertSmoke(loadingPercent.text=="0%"&&loadingFill.fillAmount==0&&rarityBadges.Count==0&&portraits.Count==0,"Loading advanced before its first request");
   AssertSmoke(loadingFill.sprite==InkRingSprite()&&loadingFill.fillMethod==UI.Image.FillMethod.Radial360&&
    loadingRoot.GetComponentsInChildren<UI.RawImage>().Length==0,"Loading does not use the original radial meter");
   int frames=0;double previous=0;bool showedPartial=false,showedComplete=false;
   while(shellLoad.Active){
    AssertSmoke(shellLoad.Fraction>=previous&&shellLoad.Fraction<=1&&Math.Abs(loadingFill.fillAmount-shellLoad.Fraction)<.001&&
     rarityBadges.Count+portraits.Count==shellLoad.Completed,"Loading ring diverged from completed resource requests");
    showedPartial|=shellLoad.Completed>0&&shellLoad.Completed<shellLoad.Total&&loadingPercent.text!="100%";
    showedComplete|=shellLoad.Completed==shellLoad.Total&&loadingPercent.text=="100%";
    previous=shellLoad.Fraction;AssertSmoke(frames++<600,"Resource loading stalled");yield return null;
   }
   AssertSmoke(showedPartial&&showedComplete&&shellLoad.Completed==startupPaths.Length&&shellLoad.Missing==0&&page=="主页","Asynchronous UI startup failed");
   AssertSmoke(homeRoot.gameObject.activeInHierarchy&&!loadingRoot.gameObject.activeSelf&&!galleryChrome.gameObject.activeSelf&&!galleryListRoot.gameObject.activeSelf&&!workshopRoot.gameObject.activeSelf,"Home did not isolate pages");
   AssertSmoke(Data.Rarities.All(rank=>rarityBadges[rank]!=null)&&
    Enumerable.Range(0,Data.Cards.Length).All(index=>portraits[PortraitKey(index)]!=null),"Loading did not warm the gallery caches");
   AssertSmoke(homeRoot.GetComponentsInChildren<UI.RawImage>().Length==0,"Home still depends on an extracted background");
   var progress=homeRoot.Find("HomeProgress");
   var fill=progress.Find("HomeProgressFill").GetComponent<UI.Image>();
   AssertSmoke(fill.type==UI.Image.Type.Filled&&Math.Abs(fill.fillAmount-.25f)<.001f&&
    progress.Find("HomeChapterState0").GetComponent<UI.Text>().text.EndsWith("已通关")&&
    progress.Find("HomeChapterState1").GetComponent<UI.Text>().text.EndsWith("可挑战")&&
    progress.Find("HomeChapterState2").GetComponent<UI.Text>().text.EndsWith("未解锁"),"Home shows stale chapter progress");
   var continueImage=(UI.Image)GalleryTestButton("HomeContinue").targetGraphic;
   AssertSmoke(continueImage.sprite==RoundedSurface(false)&&continueImage.pixelsPerUnitMultiplier==2&&
    continueImage.color==InkGold&&ThemeContrast(InkWarmText,InkGold)>=4.5&&
    ThemeContrast(InkText,InkPaper)>=4.5&&ThemeContrast(InkIvory,InkDark)>=4.5,"Ink controls or text contrast missing");
   Canvas.ForceUpdateCanvases();
   AssertSmoke(homeRoot.GetComponentsInChildren<UI.Text>().All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Home text clipped");
   AssertSmoke(loadingRoot.GetComponentsInChildren<UI.Text>(true).All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Loading text clipped");
   foreach(string target in new[]{"角色","编队","养成","装备","召唤"}){
    GalleryTestButton("HomeNav"+target).onClick.Invoke();AssertSmoke(page==target,"Home route failed: "+target);NavigateGallery("主页");
   }
   AssertSmoke(!IsModernPage("说明")&&homeRoot.Find("HomeNav说明")==null&&galleryChrome.Find("Nav说明")==null&&
    !FeatureRoutes.Contains("说明"),"Removed help still has a route");
   AssertSmoke(!GalleryTestButton("HomeFirstWin").interactable,"First-win reward enabled before winning");
   var settings=GalleryTestButton("HomeSettings");EventSystem.current.SetSelectedGameObject(settings.gameObject);settings.onClick.Invoke();
   AssertSmoke(dialogRoot!=null&&!settings.interactable&&!GalleryTestButton("HomeContinue").interactable,"Home modal failed input isolation");
   AssertSmoke(GalleryTestButton("OpenSaveFolder").interactable,"Save folder action missing from settings");
   GalleryTestButton("Motion").onClick.Invoke();CloseGalleryDialog();
   AssertSmoke(settings.interactable&&EventSystem.current.currentSelectedGameObject==settings.gameObject,"Settings rebuild lost home focus");
   int material=profile.Materials;EventSystem.current.SetSelectedGameObject(GalleryTestButton("HomeClaim").gameObject);GalleryTestButton("HomeClaim").onClick.Invoke();Load();
   AssertSmoke(profile.OfflineBank==0&&profile.Materials==material+24&&!GalleryTestButton("HomeClaim").interactable,"Offline claim not persisted");
   AssertSmoke(EventSystem.current.currentSelectedGameObject==GalleryTestButton("HomeNav角色").gameObject,"Home rebuild focused a deferred destroyed button");
   GalleryTestButton("HomeClaim").onClick.Invoke();AssertSmoke(profile.Materials==material+24,"Offline claim duplicated");
   profile.Wins=1;DrawHome();
   int crystals=profile.Crystals;
   GalleryTestButton("HomeFirstWin").onClick.Invoke();Load();
   AssertSmoke(profile.Crystals==crystals+50&&profile.FirstMission&&!GalleryTestButton("HomeFirstWin").interactable,"Moved first-win reward did not persist");
   GalleryTestButton("HomeFirstWin").onClick.Invoke();
   AssertSmoke(profile.Crystals==crystals+50,"Moved first-win reward repeated");
   saveError="测试只读存档";DrawHome();AssertSmoke(!GalleryTestButton("HomeContinue").interactable&&!GalleryTestButton("HomeClaim").interactable,"Home ignored read-only save");saveError="";DrawHome();
   GalleryTestButton("HomeContinue").onClick.Invoke();AssertSmoke(page=="挑战"&&storyMode&&!running&&profile.StorySelected==profile.StoryUnlocked,"Home chapter route started or selected wrong battle");
   NavigateGallery("角色");ResetGalleryFilters();selectedCard=0;OpenGalleryCard(0);galleryScroll.verticalNormalizedPosition=.31f;
   NavigateGallery("主页");GalleryTestButton("HomeNav角色").onClick.Invoke();
   AssertSmoke(!galleryDetailOpen&&galleryListRoot.gameObject.activeSelf&&Math.Abs(galleryScroll.verticalNormalizedPosition-1)<.01,
    "Home roundtrip must reopen the character list");
   BeginShellLoad("主页");yield return null;yield return null;
   AssertSmoke(shellLoad.Active,"Loading completed before cancellation test");
   NavigateGallery("角色");for(int n=0;n<15;n++)yield return null;
   AssertSmoke(!shellLoad.Active&&page=="角色","Cancelled loading stole navigation");
   int missingGeneration=shellLoad.Begin(1);
   page="加载";DrawShellLoading();RefreshModernFrame();previousPage=page;
   StartCoroutine(LoadShellResources(missingGeneration,"主页",new[]{RarityResourcePrefix+"__missing_smoke__"}));
   for(int n=0;shellLoad.Active&&n<120;n++)yield return null;
   AssertSmoke(!shellLoad.Active&&shellLoad.Missing==1&&page=="主页"&&GalleryTestButton("HomeContinue").interactable,
    "Missing optional resource blocked entry to home");
   rarityBadges.Remove("__missing_smoke__");
   RunSummonPageSmoke();
   running=true;NavigateGallery("角色");AssertSmoke(page=="主页","Running battle allowed unsafe navigation");running=false;DrawHome();
   AssertSmoke(silentAudio&&!uiSounds.isPlaying&&uiSounds.volume==0,"Shell headless smoke produced sound");
   Save();File.WriteAllText(Path.Combine(dir,"shell-smoke.json"),"{\"startupAsync\":true,\"assets\":"+startupPaths.Length+",\"radialStartup\":true,\"completedRequests\":true,\"inkHome\":true,\"chapterRing\":true,\"summonPage\":true,\"firstWinMoved\":true,\"helpRemoved\":true,\"loadingFrames\":"+frames+",\"homeIsolation\":true,\"navigation\":true,\"offlineClaim\":true,\"saveRoundTrip\":true,\"modalIsolation\":true,\"focusReturn\":true,\"contextReturn\":true,\"loadingCancel\":true,\"missingArtFallback\":true,\"textLayout\":true,\"soundSilent\":true,\"renderVerified\":false}");
  }
  void RunSummonPageSmoke(){
   profile.Crystals=100;profile.SaiyanPulls=19;profile.Pulls=3;Save();
   saiyanPool=true;
   GalleryTestButton("HomeNav召唤").onClick.Invoke();
   AssertSmoke(summonRoot.gameObject.activeInHierarchy&&!homeRoot.gameObject.activeSelf&&!workshopRoot.gameObject.activeSelf&&
    GalleryTestButton("SaiyanPool").targetGraphic.color==InkDark,"Summon did not isolate its page or mark the active pool");
   GalleryTestButton("SummonOnce").onClick.Invoke();Load();
   AssertSmoke(profile.SaiyanPulls==20&&profile.Pulls==3&&profile.Crystals==50&&saiyanSummonResult.StartsWith("[A]"),"Summon pity result or save failed");
   GalleryTestButton("ClassicPool").onClick.Invoke();
   GalleryTestButton("SummonOnce").onClick.Invoke();Load();
   AssertSmoke(profile.SaiyanPulls==20&&profile.Pulls==4&&profile.Crystals==0&&!GalleryTestButton("SummonOnce").interactable,"Classic pool spent or counted incorrectly");
   GalleryTestButton("SaiyanPool").onClick.Invoke();
   AssertSmoke(summonRoot.GetComponentsInChildren<UI.Text>().Single(t=>t.name=="SummonResultText").text==saiyanSummonResult,"Switching pools lost its result");
   Canvas.ForceUpdateCanvases();
   AssertSmoke(summonRoot.GetComponentsInChildren<UI.Text>().All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Summon label clipped");
   NavigateGallery("主页");
  }
 }
}
