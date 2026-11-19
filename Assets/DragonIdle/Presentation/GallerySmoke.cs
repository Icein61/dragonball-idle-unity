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
   allTab.onClick.Invoke();AssertSmoke(cardButtons.Count==9&&!catalog.OwnedOnly,"All gallery tab failed");
   AssertSmoke(galleryListRoot.Find("Title").GetComponent<UI.Text>().text=="图鉴"&&allTab.GetComponentInChildren<UI.Text>().text=="全部角色"&&
    galleryListRoot.Find("AppliedFilters")==null&&filterButtons.Count==3,"Gallery rename, help removal or filter count failed");
   AssertSmoke(galleryResources.text=="拥有8/9"&&galleryListRoot.Find("CollectionCount")==null,"Gallery header does not show owned/total exclusively");
   foreach(var pair in cardButtons){
    var card=Data.Cards[pair.Key];var root=pair.Value.transform;
    bool owned=profile.Owned[pair.Key];var power=root.Find("CharacterPower").GetComponent<UI.Text>();
    AssertSmoke(power.gameObject.activeSelf==owned&&power.text==(owned?"战斗力："+profile.CharacterPower(pair.Key):"")&&
     root.Find("CharacterType").GetComponent<UI.Text>().text==card.TypeText&&root.Find("Keywords")==null&&
     root.Find("Unowned").gameObject.activeSelf!=profile.Owned[pair.Key],"Card power, type or unowned marker incorrect");
    var badge=(RectTransform)root.Find("Rarity_"+card.Rarity);var mat=(RectTransform)root.Find("PortraitMat");
    AssertSmoke(badge!=null&&Math.Abs(badge.anchoredPosition.x-mat.anchoredPosition.x)<.001,"Rarity badge lost portrait mat alignment");
   }
   int lockedCard=Data.Index("goku-saiyan-kaioken-v020");
   profile.Owned[lockedCard]=true;RefreshGallery(false);
   AssertSmoke(cardButtons[lockedCard].transform.Find("CharacterPower").gameObject.activeSelf&&galleryResources.text=="拥有9/9","Card power stayed hidden after acquisition");
   profile.Owned[lockedCard]=false;RefreshGallery(false);
   AssertSmoke(Data.Rarities.SequenceEqual(new[]{"C","B","A","S"})&&Data.Rarities.All(rank=>RarityBadge(rank)!=null),"Rarity badge resource missing");
   AssertSmoke(cardButtons.All(pair=>pair.Value.transform.Find("Rarity_"+Data.Cards[pair.Key].Rarity)!=null),"Gallery card missing rarity badge");
   var cachedCard=cardButtons[0];var cachedGrid=gridRoot.GetChild(0);float priorScroll=galleryScroll.verticalNormalizedPosition;
   cachedCard.onClick.Invoke();AssertSmoke(galleryDetailOpen&&!galleryListRoot.gameObject.activeSelf&&detailRoot.gameObject.activeSelf,"Dedicated detail screen did not replace grid");
   AssertSmoke(GalleryTestButton("DetailTab0").GetComponentInChildren<UI.Text>().text=="信息"&&detailRoot.Find("Ownership")==null&&
    detailRoot.Find("DetailType").GetComponent<UI.Text>().text==Data.Cards[0].TypeText&&
    detailRoot.Find("CharacterTitle").GetComponent<UI.Text>().fontStyle==FontStyle.Normal,"Detail identity or information tab incorrect");
   var info=detailBody.GetComponentsInChildren<UI.Text>();
   AssertSmoke(info.Where(t=>t.name=="StatLabel").Select(t=>t.text).SequenceEqual(new[]{"生命","攻击","防御","速度","会心","韧性","穿透","闪避"})&&
    info.All(t=>!t.text.StartsWith("修炼")&&!t.text.StartsWith("共鸣")&&t.text!="战前效果")&&
    detailBody.GetComponentsInChildren<UI.Button>().Length==0,"Attribute grid or read-only info failed");
   AssertSmoke(detailRoot.Find("DetailTags").GetComponentsInChildren<UI.Text>().Select(t=>t.text).SequenceEqual(Data.Cards[0].Tags),"Identity chips do not match card");
   int adjacent=renderedCards[1];
   AssertSmoke(!GalleryTestButton("PreviousCard").interactable&&GalleryTestButton("NextCard").interactable,"Detail neighbors do not respect result bounds");
   var nextAvatar=GalleryTestButton("NextAvatar");
   var avatarRect=(RectTransform)nextAvatar.transform;
   AssertSmoke(avatarRect.rect.width==avatarRect.rect.height&&nextAvatar.transform.Find("Rarity_"+Data.Cards[adjacent].Rarity)!=null&&
    nextAvatar.GetComponentInChildren<UI.RawImage>().texture==PortraitTexture(adjacent),"Detail avatar or badge incorrect");
   GalleryTestButton("NextCard").onClick.Invoke();AssertSmoke(selectedCard==adjacent&&galleryDetailOpen,"Next arrow did not switch detail card");
   GalleryTestButton("PreviousAvatar").onClick.Invoke();AssertSmoke(selectedCard==0&&galleryDetailOpen,"Previous avatar did not switch detail card");
   GalleryTestButton("BackToCards").onClick.Invoke();
   AssertSmoke(!galleryDetailOpen&&galleryListRoot.gameObject.activeSelf&&cardButtons[0]==cachedCard&&gridRoot.GetChild(0)==cachedGrid&&Math.Abs(priorScroll-galleryScroll.verticalNormalizedPosition)<.001,"Returning rebuilt card grid or lost scroll");
   AssertSmoke(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==cachedCard.gameObject,"Return did not restore keyboard focus");
   var cardFocus=cachedCard.GetComponent<GalleryFocus>();
   var surface=(UI.Image)cachedCard.targetGraphic;var focused=surface.overrideSprite;var plain=surface.sprite;
   AssertSmoke(focused==cardFocus.CardFocusSprite&&focused!=plain&&focused.rect==plain.rect&&focused.border==plain.border&&
    surface.fillCenter&&!cachedCard.GetComponent<UI.Outline>().enabled,"Selected card does not use the same silhouette as the plain card");
   var tex=focused.texture;int mid=tex.width/2,last=tex.width-1;
   var sides=new[]{tex.GetPixel(0,mid),tex.GetPixel(last,mid),tex.GetPixel(mid,0),tex.GetPixel(mid,last)};
   AssertSmoke(sides.All(color=>Vector4.Distance(color,InkDark)<.02f)&&Vector4.Distance(tex.GetPixel(mid,mid),InkPaper)<.02f,
    "Selected surface leaves a paper rim at its outer edges");
   UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
   AssertSmoke(surface.overrideSprite==plain&&surface.color==InkPaper,"Deselect did not restore plain card surface");
   AssertSmoke(cachedCard.targetGraphic is UI.Image&&((UI.Image)cachedCard.targetGraphic).type==UI.Image.Type.Sliced,"Rounded card surface missing");
   AssertSmoke(ThemeContrast(uiPaper,uiPanel)>=4.5&&ThemeContrast(uiDim,InkMist)>=4.5&&
    ThemeContrast(InkIvory,InkDark)>=4.5&&ThemeContrast(InkWarmText,InkGold)>=4.5,"Ink text contrast below target");
   AssertSmoke(mineTab.targetGraphic.color==InkLight&&allTab.targetGraphic.color==InkDark&&
    ((UI.Image)allTab.targetGraphic).pixelsPerUnitMultiplier==2,"Ink tab states or button radius missing");
   RefreshGallery(false);AssertSmoke(cardButtons[0]==cachedCard,"Unchanged refresh rebuilt card nodes");
   foreach(var pair in cardButtons){var art=pair.Value.GetComponentInChildren<UI.RawImage>();AssertSmoke(art!=null&&Math.Abs(art.rectTransform.rect.width/art.rectTransform.rect.height-PortraitAspect(pair.Key))<.001&&art.uvRect==new Rect(0,0,1,1),"Card portrait stretched or cropped");}
   foreach(var viewport in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080),new Vector2(2560,1080)}){
    float scale=Mathf.Min(viewport.x/1600,viewport.y/900);Vector2 offset=(viewport-new Vector2(1600,900)*scale)/2;
    AssertSmoke(offset.x>=-.001f&&offset.y>=-.001f&&offset.x+1558*scale<=viewport.x+.001f&&offset.y+887*scale<=viewport.y+.001f,"Reference canvas exceeds viewport mathematical bounds");
   }
   Canvas.ForceUpdateCanvases();AssertSmoke(gridRoot.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="Theme"||t.name=="CharacterType"||t.name=="CharacterPower").All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Card form/title/power/type text clipped");
   gallerySearch.text="界王拳";
   int kaioken=Data.Index("goku-saiyan-kaioken-v020");
   AssertSmoke(selectedCard==kaioken&&cardButtons.Count==1&&!galleryEquipButton.interactable,"Search selection or unowned actions failed");
   cardButtons[kaioken].onClick.Invoke();
   AssertSmoke(!GalleryTestButton("PreviousCard").interactable&&!GalleryTestButton("NextCard").interactable,"Filtered detail escaped single-card results");
   GalleryTestButton("BackToCards").onClick.Invoke();
   gallerySearch.text="不存在的角色";
   AssertSmoke(selectedCard==-1&&cardButtons.Count==0&&galleryEquipButton==null&&gallerySwapButton==null,"Empty query leaked stale character actions");
   ResetGalleryFilters();GalleryTestButton("Filter0").onClick.Invoke();
   var menu=(RectTransform)filterDropdown.Find("FilterOptions");
   var trigger=(RectTransform)filterButtons[0].transform;
   AssertSmoke(dialogRoot==null&&menu.anchoredPosition.x==trigger.anchoredPosition.x&&
    menu.anchoredPosition.y<trigger.anchoredPosition.y-trigger.rect.height&&filterDropdown.GetComponent<UI.Image>().color.a==0,
    "Gallery filter is not an anchored dropdown");
   var firstOption=GalleryTestButton("FilterOption0");
   var events=UnityEngine.EventSystems.EventSystem.current;
   firstOption.OnMove(new UnityEngine.EventSystems.AxisEventData(events){moveDir=UnityEngine.EventSystems.MoveDirection.Down});
   AssertSmoke(events.currentSelectedGameObject==GalleryTestButton("FilterOption1").gameObject,"Dropdown keyboard focus escaped menu");
   GalleryTestButton("FilterOption3").onClick.Invoke();
   AssertSmoke(catalog.Rarity=="A"&&cardButtons.Count>0&&cardButtons.Keys.All(i=>Data.Cards[i].Rarity=="A")&&filterDropdown==null&&
    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==trigger.gameObject,"Rarity selection, dismissal or focus failed");
   GalleryTestButton("Filter1").onClick.Invoke();
   var tagScroll=filterDropdown.GetComponentInChildren<UI.ScrollRect>();
   var lastOption=tagScroll.content.GetComponentsInChildren<UI.Button>().Last();
   AssertSmoke(tagScroll.viewport.GetComponent<UI.RectMask2D>()!=null&&tagScroll.verticalScrollbar!=null&&
    tagScroll.viewport.rect.height==6*FilterRowHeight&&tagScroll.content.rect.height>tagScroll.viewport.rect.height,"Tag list is not limited to six visible rows");
   var bar=tagScroll.verticalScrollbar;var track=(RectTransform)bar.transform;
   foreach(float value in new[]{0f,.5f,1f}){
    bar.size=tagScroll.viewport.rect.height/tagScroll.content.rect.height;bar.value=value;
    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(track,bar.handleRect);
    AssertSmoke(bounds.min.y>=track.rect.yMin-.01f&&bounds.max.y<=track.rect.yMax+.01f&&
     bounds.min.x>=track.rect.xMin-.01f&&bounds.max.x<=track.rect.xMax+.01f,"Scrollbar handle spilled outside track");
   }
   tagScroll.verticalNormalizedPosition=1;
   tagScroll.OnScroll(new UnityEngine.EventSystems.PointerEventData(events){scrollDelta=new Vector2(0,-2)});
   AssertSmoke(tagScroll.verticalNormalizedPosition<.99f,"Wheel did not scroll tag options");
   events.SetSelectedGameObject(lastOption.gameObject);
   AssertSmoke(tagScroll.verticalNormalizedPosition<.01f,"Keyboard focus did not reveal last clipped tag");
   var tagOption=filterDropdown.GetComponentsInChildren<UI.Button>().Single(b=>b.GetComponentInChildren<UI.Text>()?.text=="赛亚人");
   tagOption.onClick.Invoke();
   AssertSmoke(catalog.Tag=="赛亚人"&&catalog.Role==""&&cardButtons.Count>0&&cardButtons.Keys.All(i=>Data.Cards[i].Tags.Contains("赛亚人")&&Data.Cards[i].Rarity=="A"),"Tag and rarity did not combine");
   GalleryTestButton("ClearFilter0").onClick.Invoke();
   AssertSmoke(catalog.Rarity==""&&catalog.Tag=="赛亚人"&&cardButtons.Count==Data.Cards.Count(c=>c.Tags.Contains("赛亚人")),"Individual filter chip did not preserve other filters");
   GalleryTestButton("Filter2").onClick.Invoke();
   GalleryTestButton("FilterOption1").onClick.Invoke();
   AssertSmoke(catalog.Sort==1&&filterDropdown==null&&filterButtons[2].GetComponentInChildren<UI.Text>().text.StartsWith(SortLabels[1]),"Sort dropdown failed");
   GalleryTestButton("Filter1").onClick.Invoke();filterDropdown.GetComponent<UI.Button>().onClick.Invoke();
   AssertSmoke(filterDropdown==null&&catalog.Tag=="赛亚人","Clicking outside changed selection or failed to close");
   ResetGalleryFilters();cardButtons[0].onClick.Invoke();detailTab=0;DrawGalleryDetail();
   AssertSmoke(detailRoot.GetComponentsInChildren<UI.Button>().All(b=>b.name!="TrainOnce"&&b.name!="Resonance"),"Information page leaked training entry");
   RefreshGallery(false);ShowFormationPicker(0);GalleryTestButton("Slot1").onClick.Invoke();
   AssertSmoke(profile.Team[1]==0&&profile.Team[0]==1&&profile.Leader==0&&selectedCard==0,"Formation picker swap failed");
   var snapshot=profile.InspectCharacter(0,true);var actual=profile.CreateTeam()[1];
   AssertSmoke(Math.Abs(snapshot.AttackValue-actual.AttackValue)<.000001,"Gallery stat mismatch");
   galleryScroll.verticalNormalizedPosition=.35f;float scroll=galleryScroll.verticalNormalizedPosition;
   galleryEquipButton.onClick.Invoke();AssertSmoke(page=="装备"&&selectedCard==0,"Equipment navigation lost character");
   page="角色";UpdateGallery();AssertSmoke(Math.Abs(galleryScroll.verticalNormalizedPosition-scroll)<.01f&&selectedCard==0,"Gallery return lost browsing position");
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
    AssertSmoke(detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.name=="DetailType"&&t.text==Data.Cards[i].TypeText),"Detail type missing");
    AssertSmoke(detailRoot.GetComponentsInChildren<UI.Text>().Any(t=>t.text==Data.Cards[i].LeaderText),"Leader definition UI mismatch");
   }
   foreach(int i in Enumerable.Range(0,Data.Cards.Length)){
    selectedCard=i;DrawGalleryDetail();var art=detailRoot.GetComponentsInChildren<UI.RawImage>()
     .SingleOrDefault(image=>image.transform.parent==detailRoot&&image.name=="Portrait");AssertSmoke(art!=null,"Detail art missing");
    AssertSmoke(art.texture==PortraitTexture(i)&&art.texture!=FighterTexture(i)&&art.uvRect==new Rect(0,0,1,1),"Detail reused atlas or cropped portrait UV");
    var rect=art.rectTransform.rect;var image=art.texture;
    AssertSmoke(Mathf.Abs(rect.width/rect.height-image.width/(float)image.height)<.001f,"Detail distorted portrait aspect: "+Data.Cards[i].Id);
   }
   AssertSmoke(galleryCanvas.GetComponent<UI.CanvasScaler>().screenMatchMode==UI.CanvasScaler.ScreenMatchMode.Expand,"UI aspect ratio scaler incorrect");
   File.WriteAllText(Path.Combine(dir,"gallery-refresh-smoke.json"),"{\"dedicatedDetail\":true,\"cardCache\":true,\"focusReturn\":true,\"roundedSurfaces\":true,\"individualFilterClear\":true,\"filterDropdowns\":true,\"simplifiedCards\":true,\"characterPowerAndTypes\":true,\"flushCardFocus\":true,\"unownedPowerHidden\":true,\"sixRowFilterScroll\":true,\"boundedScrollbarHandle\":true,\"detailNavigationAndInfo\":true,\"fullPortraitAspect\":true,\"enabledTextContrast\":true,\"viewportMath\":[\"1280x720\",\"1600x900\",\"1920x1080\",\"2560x1080\"],\"renderVerified\":false}");
   File.WriteAllText(Path.Combine(dir,"gallery-smoke.json"),"{\"eventsWired\":true,\"filters\":true,\"stats\":true,\"navigation\":true,\"textLayout\":true,\"audioPreferences\":true,\"audioSilentDuringTest\":true,\"visualVerified\":false,\"listened\":false}");
   Save();
  }
 }
}
