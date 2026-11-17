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
   var brand=galleryRoot.GetComponentsInChildren<UI.Text>().Single(t=>t.name=="Brand");
   AssertSmoke(brand.text==Data.GameTitle&&brand.preferredWidth<=brand.rectTransform.rect.width&&brand.preferredHeight<=brand.rectTransform.rect.height,"New game title clipped or missing");
   AssertSmoke(cardButtons.Count==8&&selectedCard==0,"Owned gallery failed");
   allTab.onClick.Invoke();AssertSmoke(cardButtons.Count==15&&!catalog.OwnedOnly,"All gallery tab failed");
   var cachedCard=cardButtons[0];var cachedGrid=gridRoot.GetChild(0);float priorScroll=galleryScroll.verticalNormalizedPosition;
   cachedCard.onClick.Invoke();AssertSmoke(galleryDetailOpen&&!galleryListRoot.gameObject.activeSelf&&detailRoot.gameObject.activeSelf,"Dedicated detail screen did not replace grid");
   var breakdown=GalleryTestButton("ToggleStatBreakdown");UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(breakdown.gameObject);breakdown.onClick.Invoke();
   AssertSmoke(statsExpanded&&detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.name=="Base0")&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==GalleryTestButton("ToggleStatBreakdown").gameObject,"Stat breakdown failed or lost focus on refresh");
   GalleryTestButton("ToggleStatBreakdown").onClick.Invoke();AssertSmoke(!statsExpanded&&!detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.name=="Base0"),"Stat breakdown did not collapse");
   GalleryTestButton("BackToCards").onClick.Invoke();
   AssertSmoke(!galleryDetailOpen&&galleryListRoot.gameObject.activeSelf&&cardButtons[0]==cachedCard&&gridRoot.GetChild(0)==cachedGrid&&Math.Abs(priorScroll-galleryScroll.verticalNormalizedPosition)<.001,"Returning rebuilt card grid or lost scroll");
   AssertSmoke(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==cachedCard.gameObject,"Return did not restore keyboard focus");
   AssertSmoke(cachedCard.targetGraphic is UI.Image&&((UI.Image)cachedCard.targetGraphic).type==UI.Image.Type.Sliced,"Rounded card surface missing");
   AssertSmoke(ThemeContrast(uiPaper,uiPanel)>=4.5&&ThemeContrast(uiDim,uiPanel)>=4.5&&ThemeContrast(Color.white,uiAccent)>=4.5,"Enabled theme text contrast below target");
   RefreshGallery(false);AssertSmoke(cardButtons[0]==cachedCard,"Unchanged refresh rebuilt card nodes");
   foreach(var pair in cardButtons){var art=pair.Value.GetComponentInChildren<UI.RawImage>();AssertSmoke(art!=null&&Math.Abs(art.rectTransform.rect.width/art.rectTransform.rect.height-PortraitAspect(pair.Key))<.001&&art.uvRect==new Rect(0,0,1,1),"Card portrait stretched or cropped");}
   foreach(var viewport in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080),new Vector2(2560,1080)}){
    float scale=Mathf.Min(viewport.x/1600,viewport.y/900);Vector2 offset=(viewport-new Vector2(1600,900)*scale)/2;
    AssertSmoke(offset.x>=-.001f&&offset.y>=-.001f&&offset.x+1558*scale<=viewport.x+.001f&&offset.y+887*scale<=viewport.y+.001f,"Reference canvas exceeds viewport mathematical bounds");
   }
   Canvas.ForceUpdateCanvases();AssertSmoke(gridRoot.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="Theme"||t.name=="Keywords").All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Card form/title text clipped");
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
   GalleryTestButton("ClearFilter0").onClick.Invoke();AssertSmoke(catalog.Rarity==""&&cardButtons.Count==15,"Individual filter chip did not clear condition");
   ResetGalleryFilters();ShowFilter(1);GalleryTestButton("Option1").onClick.Invoke();
   AssertSmoke(catalog.Role=="自我强化"&&cardButtons.Count==Data.Cards.Count(c=>c.Keywords.Contains("自我强化"))&&cardButtons.Keys.All(i=>Data.Cards[i].Keywords.Contains("自我强化")),"Keyword picker did not filter actual role keywords");
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
   foreach(int i in Enumerable.Range(0,Data.Cards.Length)){
    selectedCard=i;detailTab=1;DrawGalleryDetail();Canvas.ForceUpdateCanvases();
    AssertSmoke(measuredText.All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Leader or keyword detail clipped");
    AssertSmoke(detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.text.Contains(Data.Cards[i].KeywordText)),"Role summary missing");
    AssertSmoke(detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.text==Data.Cards[i].LeaderText),"Leader definition UI mismatch");
   }
   foreach(int i in Enumerable.Range(0,Data.Cards.Length)){
    selectedCard=i;DrawGalleryDetail();var images=detailRoot.GetComponentsInChildren<UI.RawImage>();AssertSmoke(images.Length==1,"Detail art missing");
    AssertSmoke(images[0].texture==PortraitTexture(i)&&images[0].texture!=FighterTexture(i)&&images[0].uvRect==new Rect(0,0,1,1),"Detail reused atlas or cropped portrait UV");
   }
   AssertSmoke(galleryCanvas.GetComponent<UI.CanvasScaler>().screenMatchMode==UI.CanvasScaler.ScreenMatchMode.Expand,"UI aspect ratio scaler incorrect");
   File.WriteAllText(Path.Combine(dir,"gallery-refresh-smoke.json"),"{\"dedicatedDetail\":true,\"cardCache\":true,\"focusReturn\":true,\"roundedSurfaces\":true,\"individualFilterClear\":true,\"fullPortraitAspect\":true,\"enabledTextContrast\":true,\"viewportMath\":[\"1280x720\",\"1600x900\",\"1920x1080\",\"2560x1080\"],\"renderVerified\":false}");
   File.WriteAllText(Path.Combine(dir,"gallery-smoke.json"),"{\"eventsWired\":true,\"filters\":true,\"stats\":true,\"navigation\":true,\"textLayout\":true,\"audioPreferences\":true,\"audioSilentDuringTest\":true,\"visualVerified\":false,\"listened\":false}");
   Save();
  }
 }
}
