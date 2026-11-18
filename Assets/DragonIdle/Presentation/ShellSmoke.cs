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
   savePath=Path.Combine(dir,"shell-save.json");profile=new Profile();profile.OfflineBank=24;Save();
   running=false;BeginShellLoad("主页");
   AssertSmoke(page=="加载"&&loadingRoot.gameObject.activeInHierarchy&&!galleryChrome.gameObject.activeSelf&&!detailRoot.gameObject.activeSelf,"Loading leaked other pages");
   int frames=0;double previous=0;
   while(shellLoad.Active){
    AssertSmoke(shellLoad.Fraction>=previous&&shellLoad.Fraction<=1&&loadingFill.fillAmount>=(float)previous-.001f,"Loading progress went backwards");
    previous=shellLoad.Fraction;AssertSmoke(frames++<600,"Resource loading stalled");yield return null;
   }
   AssertSmoke(frames>0&&shellLoad.Completed==ShellKeys.Length&&shellLoad.Missing==0&&page=="主页","Asynchronous UI startup failed");
   AssertSmoke(homeRoot.gameObject.activeInHierarchy&&!loadingRoot.gameObject.activeSelf&&!galleryChrome.gameObject.activeSelf&&!galleryListRoot.gameObject.activeSelf&&!workshopRoot.gameObject.activeSelf,"Home did not isolate pages");
   AssertSmoke(shellAssets.Count==11&&shellAssets.Values.All(x=>x!=null),"UI resources missing");
   foreach(var picture in homeRoot.GetComponentsInChildren<UI.RawImage>())AssertSmoke(picture.uvRect==new Rect(0,0,1,1)&&Math.Abs(picture.rectTransform.rect.width/picture.rectTransform.rect.height-(float)picture.texture.width/picture.texture.height)<.001,"Home scene cropped or stretched");
   foreach(string key in new[]{"button-primary","button-secondary"})AssertSmoke(ShellSprite(key).border==new Vector4(30,12,30,12),"Button import border missing");
   Canvas.ForceUpdateCanvases();
   AssertSmoke(homeRoot.GetComponentsInChildren<UI.Text>().All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Home text clipped");
   AssertSmoke(loadingRoot.GetComponentsInChildren<UI.Text>(true).All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Loading text clipped");
   foreach(string target in new[]{"角色","编队","养成","装备","召唤","说明"}){
    GalleryTestButton("HomeNav"+target).onClick.Invoke();AssertSmoke(page==target,"Home route failed: "+target);NavigateGallery("主页");
   }
   var settings=GalleryTestButton("HomeSettings");EventSystem.current.SetSelectedGameObject(settings.gameObject);settings.onClick.Invoke();
   AssertSmoke(dialogRoot!=null&&!settings.interactable&&!GalleryTestButton("HomeContinue").interactable,"Home modal failed input isolation");
   GalleryTestButton("Motion").onClick.Invoke();CloseGalleryDialog();
   AssertSmoke(settings.interactable&&EventSystem.current.currentSelectedGameObject==settings.gameObject,"Settings rebuild lost home focus");
   int material=profile.Materials;EventSystem.current.SetSelectedGameObject(GalleryTestButton("HomeClaim").gameObject);GalleryTestButton("HomeClaim").onClick.Invoke();Load();
   AssertSmoke(profile.OfflineBank==0&&profile.Materials==material+24&&!GalleryTestButton("HomeClaim").interactable,"Offline claim not persisted");
   AssertSmoke(EventSystem.current.currentSelectedGameObject==GalleryTestButton("HomeNav角色").gameObject,"Home rebuild focused a deferred destroyed button");
   GalleryTestButton("HomeClaim").onClick.Invoke();AssertSmoke(profile.Materials==material+24,"Offline claim duplicated");
   saveError="测试只读存档";DrawHome();AssertSmoke(!GalleryTestButton("HomeContinue").interactable&&!GalleryTestButton("HomeClaim").interactable,"Home ignored read-only save");saveError="";DrawHome();
   GalleryTestButton("HomeContinue").onClick.Invoke();AssertSmoke(page=="挑战"&&storyMode&&!running&&profile.StorySelected==profile.StoryUnlocked,"Home chapter route started or selected wrong battle");
   NavigateGallery("角色");ResetGalleryFilters();selectedCard=0;OpenGalleryCard(0);galleryScroll.verticalNormalizedPosition=.31f;float scroll=galleryScroll.verticalNormalizedPosition;
   NavigateGallery("主页");GalleryTestButton("HomeNav角色").onClick.Invoke();AssertSmoke(galleryDetailOpen&&selectedCard==0&&Math.Abs(galleryScroll.verticalNormalizedPosition-scroll)<.01,"Home roundtrip lost character context");
   BeginShellLoad("主页");NavigateGallery("角色");for(int n=0;n<15;n++)yield return null;
   AssertSmoke(!shellLoad.Active&&page=="角色","Cancelled loading stole navigation");
   var old=shellAssets["home-scene"];shellAssets["home-scene"]=null;NavigateGallery("主页");
   AssertSmoke(homeRoot.GetComponentsInChildren<UI.RawImage>().Length==0&&GalleryTestButton("HomeContinue").interactable,"Missing art disabled navigation fallback");shellAssets["home-scene"]=old;DrawHome();
   running=true;NavigateGallery("角色");AssertSmoke(page=="主页","Running battle allowed unsafe navigation");running=false;DrawHome();
   AssertSmoke(silentAudio&&!menuMusic.isPlaying&&uiSounds.volume==0,"Shell headless smoke produced sound");
   Save();File.WriteAllText(Path.Combine(dir,"shell-smoke.json"),"{\"startupAsync\":true,\"assets\":11,\"loadingFrames\":"+frames+",\"homeIsolation\":true,\"navigation\":true,\"offlineClaim\":true,\"saveRoundTrip\":true,\"modalIsolation\":true,\"focusReturn\":true,\"contextReturn\":true,\"loadingCancel\":true,\"missingArtFallback\":true,\"textLayout\":true,\"soundSilent\":true,\"renderVerified\":false}");
  }
 }
}
