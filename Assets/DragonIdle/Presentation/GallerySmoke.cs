using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  UI.Button GalleryTestButton(string name){return galleryRoot.GetComponentsInChildren<UI.Button>().First(x=>x.name==name);}
  void RunGallerySmoke(string dir){
   savePath=Path.Combine(dir,"gallery-save.json");profile=new Profile();Save();page="角色";selectedCard=0;
   EnsureGallery();galleryCanvas.SetActive(true);catalog.OwnedOnly=true;ResetGalleryFilters();
   AssertSmoke(cardButtons.Count==10&&selectedCard==0,"Owned gallery failed");
   allTab.onClick.Invoke();AssertSmoke(cardButtons.Count==25&&!catalog.OwnedOnly,"All gallery tab failed");
   Canvas.ForceUpdateCanvases();AssertSmoke(gridRoot.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="Theme").All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Card form/title text clipped");
   gallerySearch.text="界王拳";
   int kaioken=Data.Index("goku-saiyan-kaioken-v020");
   AssertSmoke(selectedCard==kaioken&&cardButtons.Count==1&&!galleryEquipButton.interactable,"Search selection or unowned actions failed");
   gallerySearch.text="不存在的角色";
   AssertSmoke(selectedCard==-1&&cardButtons.Count==0&&galleryTrainButton==null&&gallerySwapButton==null,"Empty query leaked stale character actions");
   ResetGalleryFilters();ShowFilter(0);
   AssertSmoke(!mineTab.interactable&&!gallerySearch.interactable&&cardButtons.Values.All(b=>!b.interactable),"Modal does not isolate background input");
   GalleryTestButton("Option3").onClick.Invoke();
   AssertSmoke(mineTab.interactable&&gallerySearch.interactable,"Modal did not restore background input");
   AssertSmoke(catalog.Rarity=="SSR"&&cardButtons.Keys.All(i=>Data.Cards[i].Rarity=="SSR")&&dialogRoot==null,"Direct rarity picker failed");
   ResetGalleryFilters();cardButtons[0].onClick.Invoke();detailTab=0;DrawGalleryDetail();
   int material=profile.Materials;galleryTrainButton.onClick.Invoke();
   AssertSmoke(profile.Training[0]==1&&profile.Materials==material-30,"Training action transaction failed");
   Load();AssertSmoke(profile.Training[0]==1&&profile.Materials==material-30,"Training UI persistence failed");
   RefreshGallery(false);ShowFormationPicker(0);GalleryTestButton("Slot1").onClick.Invoke();
   AssertSmoke(profile.Team[1]==0&&profile.Team[0]==1&&profile.Leader==0&&selectedCard==0,"Formation picker swap failed");
   var snapshot=profile.InspectCharacter(0,true);var actual=profile.CreateTeam()[1];
   AssertSmoke(Math.Abs(snapshot.AttackValue-actual.AttackValue)<.000001,"Gallery stat mismatch");
   galleryScroll.verticalNormalizedPosition=.35f;float scroll=galleryScroll.verticalNormalizedPosition;
   galleryEquipButton.onClick.Invoke();AssertSmoke(page=="装备"&&selectedCard==0,"Equipment navigation lost character");
   page="角色";UpdateGallery();AssertSmoke(Math.Abs(galleryScroll.verticalNormalizedPosition-scroll)<.01f&&selectedCard==0,"Gallery return lost browsing position");
   ShowAcquisition(Data.Index("piccolo-raditz-v030"));GalleryTestButton("GoStory").onClick.Invoke();
   AssertSmoke(page=="挑战"&&storyMode&&profile.StorySelected==0,"Story acquisition route failed");
   page="角色";UpdateGallery();ShowAcquisition(kaioken);GalleryTestButton("GoSaiyanPool").onClick.Invoke();
   AssertSmoke(page=="召唤"&&saiyanPool,"Pool acquisition route failed");
   page="角色";UpdateGallery();ShowAudioSettings();
   var sliders=dialogRoot.GetComponentsInChildren<UI.Slider>();AssertSmoke(sliders.Length==3,"Audio controls missing");
   sliders[0].value=.4f;sliders[1].value=.1f;sliders[2].value=.2f;GalleryTestButton("Motion").onClick.Invoke();CloseGalleryDialog();
   var restored=JsonUtility.FromJson<AudioPreferences>(File.ReadAllText(audioPath));
   AssertSmoke(Math.Abs(restored.Master-.4f)<.001&&Math.Abs(restored.Music-.1f)<.001&&restored.ReducedMotion,"Audio preferences persistence failed");
   AssertSmoke(silentAudio&&!menuMusic.isPlaying&&menuMusic.volume==0&&uiSounds.volume==0,"Headless test must never produce sound");
   AssertSmoke(menuMusic.clip.samples==22050*32&&uiClips.Length==3&&uiSoundEvents>0,"Audio synthesis or feedback wiring missing");
   float[] samples=new float[menuMusic.clip.samples];menuMusic.clip.GetData(samples,0);
   AssertSmoke(samples.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<=1)&&samples.Any(v=>Math.Abs(v)>.01f),"Invalid or silent music samples");
   ResetGalleryFilters();selectedCard=0;detailTab=1;DrawGalleryDetail();Canvas.ForceUpdateCanvases();
   AssertSmoke(measuredText.Count>=6&&measuredText.All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Long Chinese skill text clipped in layout");
   foreach(int i in new[]{0,Data.Index("piccolo-raditz-v030"),Data.Index("raditz-arrival-v030")}){
    selectedCard=i;DrawGalleryDetail();AssertSmoke(detailRoot.GetComponentsInChildren<UI.RawImage>().Length==1,"Detail art missing");
   }
   AssertSmoke(galleryCanvas.GetComponent<UI.CanvasScaler>().screenMatchMode==UI.CanvasScaler.ScreenMatchMode.Expand,"UI aspect ratio scaler incorrect");
   File.WriteAllText(Path.Combine(dir,"gallery-smoke.json"),"{\"eventsWired\":true,\"filters\":true,\"stats\":true,\"navigation\":true,\"textLayout\":true,\"audioPreferences\":true,\"audioSilentDuringTest\":true,\"visualVerified\":false,\"listened\":false}");
   Save();
  }
 }
}
