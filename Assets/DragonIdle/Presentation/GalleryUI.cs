using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  const int DefaultCharacterSort=4;
  readonly CatalogFilter catalog=new CatalogFilter{RoundFormat=true,OwnedOnly=false,Sort=DefaultCharacterSort};
  RectTransform galleryRoot,gridRoot,detailRoot,dialogRoot;
  GameObject galleryCanvas;UI.ScrollRect galleryScroll;UI.InputField gallerySearch;
  UI.Text galleryResources,galleryToast;
  readonly List<UI.Button> filterButtons=new List<UI.Button>();
  int renderedOwnedCount=-1,renderedFavoriteCount=-1;
  bool[] renderedOwnership;
  Material portraitGray;
  static readonly Color GalleryLockedCard=new Color32(219,222,217,255),GalleryLockedMat=new Color32(198,204,195,255);
  readonly Dictionary<int,UI.Button> cardButtons=new Dictionary<int,UI.Button>();
  readonly List<UI.Text> measuredText=new List<UI.Text>();
  int detailTab;bool galleryDirty=true;string previousPage="";
  float toastUntil,detailFade=1;CanvasGroup detailGroup;
  static Color uiPaper=>InkText;
  static Color uiPanel=>InkPaper;
  static readonly Color uiDim=new Color32(55,67,62,255),uiAccent=new Color32(105,70,39,255);
  static readonly string[] FeatureRoutes={"角色",FormationRoute,DemoBattleRoute};
  static string[] SortLabels={"上阵优先","稀有度优先","角色名称","角色类型","按序号"};

  RectTransform UNode(Transform parent,string name,float x,float y,float w,float h){
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
   r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
  }
  RectTransform UPanel(Transform parent,string name,float x,float y,float w,float h,Color color,bool raycast=false){
   var r=UNode(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<UI.Image>();image.color=color;image.raycastTarget=raycast;
   if(w>=32&&h>=24&&color.a>0&&name!="Background"&&name!="Header"&&name!="Sidebar"&&name!="ModalBackdrop"){
    image.sprite=RoundedSurface(false);
    image.type=UI.Image.Type.Sliced;
    image.pixelsPerUnitMultiplier=2;
   }
   return r;
  }
  UI.Text UText(Transform parent,string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.UpperLeft){
   var r=UNode(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<UI.Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;
   t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
  }
  UI.Button UButton(Transform parent,string name,string value,float x,float y,float w,float h,Action action,bool primary=false,bool enabled=true,bool selected=false){
   var r=UPanel(parent,name,x,y,w,h,InkLight,true);
   var b=r.gameObject.AddComponent<UI.Button>();b.targetGraphic=r.GetComponent<UI.Image>();b.interactable=enabled;
   var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(.96f,.96f,.96f);c.pressedColor=new Color(.88f,.88f,.88f);c.selectedColor=new Color(.96f,.96f,.96f);c.disabledColor=new Color(.75f,.75f,.75f);c.fadeDuration=audioSettings!=null&&audioSettings.ReducedMotion?0:.12f;b.colors=c;
   UText(r,"Label",value,10,0,w-20,h,18,InkText,TextAnchor.MiddleCenter);
   if(primary&&enabled&&!selected){
    b.targetGraphic.color=InkGold;
    b.GetComponentInChildren<UI.Text>().color=InkWarmText;
   }else SetButtonActive(b,selected);
   var outline=r.gameObject.AddComponent<UI.Outline>();outline.effectColor=InkCharge;outline.effectDistance=new Vector2(3,-3);outline.enabled=false;r.gameObject.AddComponent<GalleryFocus>().Outline=outline;
   b.onClick.AddListener(()=>{if(!b.interactable)return;PlayUISound(0);action();});return b;
  }
  void ClearUI(Transform parent,Transform keep=null){
   for(int index=parent.childCount-1;index>=0;index--){
    var child=parent.GetChild(index);
    if(child==keep)continue;
    child.gameObject.SetActive(false);
    child.SetParent(null,false);
    Destroy(child.gameObject);
   }
  }
  void SetButtonActive(UI.Button b,bool active){
   b.targetGraphic.color=!b.interactable?InkDisabled:active?InkDark:InkLight;
   b.GetComponentInChildren<UI.Text>().color=active&&b.interactable?InkIvory:InkText;
  }
  UI.ScrollRect UScroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content,bool showBar=true){
   UPanel(parent,name+"Surface",x-3,y-3,w+6,h+6,InkPaper);
   var viewport=UPanel(parent,name,x,y,w,h,Color.clear,true);viewport.gameObject.AddComponent<UI.RectMask2D>();
   UI.ScrollRect scroll=viewport.gameObject.AddComponent<BoundedScrollRect>();content=UNode(viewport,"Content",0,0,w-(showBar?14:0),h);
   scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=!(audioSettings?.ReducedMotion??false)?
    UI.ScrollRect.MovementType.Elastic:UI.ScrollRect.MovementType.Clamped;
   scroll.elasticity=.145f;
   scroll.scrollSensitivity=35;
   if(!showBar)return scroll;
   var track=UPanel(parent,name+"Track",x+w-12,y,10,h,InkPaper,true);
   var slide=UNode(track,"SlidingArea",0,0,10,h);
   slide.anchorMin=Vector2.zero;slide.anchorMax=Vector2.one;slide.pivot=new Vector2(.5f,.5f);slide.offsetMin=slide.offsetMax=Vector2.zero;
   var handle=UPanel(slide,"Handle",0,0,10,0,InkDark,true);
   RoundMeterCorners(track,10);
   RoundMeterCorners(handle,10);
   // Scrollbar controls its handle anchors; explicit size or position offsets make it spill past the track.
   handle.pivot=new Vector2(.5f,.5f);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
   var bar=track.gameObject.AddComponent<UI.Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<UI.Image>();bar.direction=UI.Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
   return scroll;
  }
  static Rect? CharacterCardPortraitRegion(int index){
   if(Data.Cards[index].Id!=RoundMotionArt.PiccoloCardId)return null;
   // Frame the new figure around its painted bounds, with space for the hands and boots.
   return new Rect(0,126f/1536,907f/1024,1145f/1536);
  }
  void UArt(Transform parent,int i,float x,float y,float w,float h,Rect? region=null,bool muted=false){
   var art=PortraitTexture(i);
   if(art!=null){
    var uv=region??new Rect(0,0,1,1);
    float aspect=PortraitAspect(i)*uv.width/uv.height;
    float width=Math.Min(w,h*aspect),height=width/aspect;
    var r=UNode(parent,"Portrait",x+(w-width)/2,y+(h-height)/2,width,height);var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=art;image.uvRect=uv;image.raycastTarget=false;
    if(muted)MutePortrait(image);
   }else{
    UPanel(parent,"ArtInset",x+8,y+6,w-16,h-12,new Color(.14f,.18f,.21f));
    UPanel(parent,"Accent",x+8,y+6,3,h-12,CardColor(i));
    UText(parent,"Monogram",Data.Cards[i].Name.Substring(0,1),x,y+h*.16f,w,h*.45f,58,CardColor(i),TextAnchor.MiddleCenter);
    UText(parent,"ArtPending","形象待制作",x,y+h-38,w,28,13,uiDim,TextAnchor.MiddleCenter);
   }
  }
  void MutePortrait(UI.RawImage image){
   if(portraitGray==null){
    var shader=Resources.Load<Shader>("Art/Effects/PortraitGray");
    if(shader==null)throw new InvalidOperationException("缺少未拥有角色的灰阶材质");
    portraitGray=new Material(shader);
   }
   image.material=portraitGray;
  }
  void EnsureGallery(){
   if(galleryCanvas!=null)return;
   galleryCanvas=new GameObject("CharacterGallery",typeof(RectTransform),typeof(Canvas),typeof(UI.CanvasScaler),typeof(UI.GraphicRaycaster));
   var canvas=galleryCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
   var scaler=galleryCanvas.GetComponent<UI.CanvasScaler>();scaler.uiScaleMode=UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=UI.CanvasScaler.ScreenMatchMode.Expand;
   galleryRoot=UNode(galleryCanvas.transform,"GalleryFrame",0,0,1600,900);galleryRoot.anchorMin=galleryRoot.anchorMax=galleryRoot.pivot=new Vector2(.5f,.5f);galleryRoot.anchoredPosition=Vector2.zero;
   if(EventSystem.current==null)new GameObject("UIEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
   galleryChrome=UNode(galleryRoot,"Chrome",0,0,1600,900);
   UPanel(galleryChrome,"Background",0,0,1600,900,InkMist);
   UPanel(galleryChrome,"Header",0,0,1600,90,InkDark);
   UPanel(galleryChrome,"Sidebar",0,90,190,810,InkPaper);
   UPanel(galleryChrome,"HeaderLine",0,88,1600,2,InkBorder);
   UText(galleryChrome,"Brand",Data.GameTitle,25,20,180,40,26,InkIvory);
   UText(galleryChrome,"Section","角色",232,28,500,40,22,InkIvory);
   galleryResources=UText(galleryChrome,"Resources","",900,27,650,40,19,InkOnDark,TextAnchor.MiddleRight);
   for(int i=0;i<FeatureRoutes.Length;i++){
    string target=FeatureRoutes[i];
    UButton(galleryChrome,"Nav"+target,target,18,126+i*62,154,45,()=>OpenFeature(target),selected:target=="角色");
   }
   UButton(galleryChrome,"AudioSettings","声音与显示",18,735,154,44,ShowAudioSettings);
   UText(galleryChrome,"Version","v"+Data.Version+"  /  预览版",20,840,160,30,13,uiDim);
   UButton(galleryChrome,"Nav主页","返回主页",18,819,154,47,()=>NavigateGallery("主页"));
   galleryListRoot=UNode(galleryRoot,"CardLibrary",0,0,1600,900);
   var input=UPanel(galleryListRoot,"Search",222,105,450,44,InkLight,true);
   gallerySearch=input.gameObject.AddComponent<UI.InputField>();
   gallerySearch.textComponent=UText(input,"InputText","",12,0,426,44,17,uiPaper,TextAnchor.MiddleCenter);
   gallerySearch.placeholder=UText(input,"Placeholder","搜索名字、形态、称号",12,0,426,44,17,uiDim,TextAnchor.MiddleCenter);
   gallerySearch.characterLimit=60;
   gallerySearch.onValueChanged.AddListener(v=>{catalog.Search=v;RefreshGallery(true);});
   string[] filters={"稀有度","标签",SortLabels[DefaultCharacterSort]};
   for(int i=0;i<filters.Length;i++){
    int n=i;
    var filter=UButton(galleryListRoot,"Filter"+i,filters[i],688+i*218,105,202,44,()=>ShowFilterDropdown(n));
    UText(filter.transform,"Caret","▾",168,0,26,44,17,InkMuted,TextAnchor.MiddleCenter);
    filterButtons.Add(filter);
   }
   UButton(galleryListRoot,"ResetFilters","清空条件",1376,105,174,44,ResetGalleryFilters);
   filterChipRoot=UNode(galleryListRoot,"FilterChips",222,158,1180,32);
   galleryScroll=UScroll(galleryListRoot,"CardScroll",222,198,1336,630,out gridRoot);
   detailRoot=UPanel(galleryRoot,"Detail",222,96,1336,732,uiPanel);
   detailGroup=detailRoot.gameObject.AddComponent<CanvasGroup>();
   galleryToast=UText(galleryRoot,"Toast","",232,845,1320,42,17,uiAccent);
   detailRoot.gameObject.SetActive(false);
  }
  void NavigateGallery(string target){
   if(!IsModernPage(target)||target==RoundPreviewPage&&roundBattle==null)return;
   if(shellLoad.Active&&target!="加载")shellLoad.Cancel();
   bool openingCharacterList=target=="角色"&&page!="角色";
   if(openingCharacterList){
    galleryDetailOpen=false;
    selectedCard=-1;
    detailTab=0;
   }
   if(page==FormationRoute&&target!=FormationRoute)LeaveFormation();
   if(page==RoundPreviewPage&&target!=RoundPreviewPage){roundBattle=null;roundLast=null;roundLog.Clear();ResetRoundMotion();roundDemo=false;}
   CloseFilterDropdown();CloseGalleryDialog();page=target;autoRepeat=false;notice="";galleryDirty=true;
   galleryCanvas.SetActive(IsModernPage(target));previousPage="";UpdateGallery();
   if(openingCharacterList){
    galleryScroll.StopMovement();
    galleryScroll.verticalNormalizedPosition=1;
   }
   if(target=="角色"&&EventSystem.current!=null){
    if(galleryDetailOpen&&selectedCard>=0)EventSystem.current.SetSelectedGameObject(null);
    else if(cardButtons.ContainsKey(selectedCard))EventSystem.current.SetSelectedGameObject(cardButtons[selectedCard].gameObject);
   }
   if(target=="主页"&&EventSystem.current!=null&&homeRoot!=null)EventSystem.current.SetSelectedGameObject(ActiveBackButton(homeRoot,"HomeNav角色"));
  }
  void UpdateGallery(){
   if(IsModernPage(page)){EnsureGallery();if(!galleryCanvas.activeSelf)galleryCanvas.SetActive(true);
    if((previousPage!=page||galleryDirty)&&dialogRoot==null){if(page=="角色")RefreshGallery(false);else if(page==FormationRoute)DrawFormation();else if(page=="主页")DrawHome();else if(page==RoundPreviewPage)DrawRoundPreview();galleryDirty=false;RefreshModernFrame();}
    if(Input.GetKeyDown(KeyCode.Escape)){if(filterDropdown!=null)CloseFilterDropdown();else if(dialogRoot!=null)CloseGalleryDialog();else if(page==RoundPreviewPage)NavigateGallery(RoundBackPage());else if(page=="角色"&&galleryDetailOpen)ReturnToCards();else if(page=="角色"||page==FormationRoute)NavigateGallery("主页");else if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    if(detailGroup!=null){detailFade=Math.Min(1,detailFade+Time.unscaledDeltaTime*8);detailGroup.alpha=audioSettings!=null&&audioSettings.ReducedMotion?1:detailFade;}
    galleryToast.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"";
    if(page=="主页")RefreshHomeNotice();
   }else if(galleryCanvas!=null&&galleryCanvas.activeSelf){CloseFilterDropdown();galleryCanvas.SetActive(false);}
   previousPage=page;
  }
  void GalleryNotice(string message,bool success=true){notice=message;toastUntil=Time.unscaledTime+5;PlayUISound(success?1:2);galleryDirty=true;}
  void ResetGalleryFilters(){CloseFilterDropdown();catalog.Rarity=catalog.Role=catalog.Tag=catalog.Search="";gallerySearch.SetTextWithoutNotify("");RefreshGallery(true);}
  void RefreshGallery(bool reset){
   galleryDirty=false;if(galleryCanvas==null)return;
   int[] results=catalog.Results(profile);
   int[] favorites=results.Where(profile.IsFavoriteCard).ToArray();
   int[] owned=results.Where(index=>profile.Owned[index]&&!profile.IsFavoriteCard(index)).ToArray();
   int[] unowned=results.Where(index=>!profile.Owned[index]&&!profile.IsFavoriteCard(index)).ToArray();
   var ordered=favorites.Concat(owned).Concat(unowned).ToArray();
   var ownership=ordered.Select(index=>profile.Owned[index]).ToArray();
   selectedCard=CatalogFilter.Reconcile(selectedCard,ordered);
   RefreshFeatureHeaderCount();
   string[] labels={catalog.Rarity==""?"稀有度":catalog.Rarity,catalog.Tag==""?"标签":catalog.Tag,SortLabels[catalog.Sort]};
   for(int i=0;i<filterButtons.Count;i++){
    var button=filterButtons[i];
    button.GetComponentInChildren<UI.Text>().text=labels[i];
   }
   ClearUI(filterChipRoot);int chips=0;
   for(int n=0;n<2;n++){int kind=n;string value=kind==0?catalog.Rarity:catalog.Tag;if(value=="")continue;UButton(filterChipRoot,"ClearFilter"+n,value+" ×",chips++*188,0,178,32,()=>{if(kind==0)catalog.Rarity="";else catalog.Tag="";RefreshGallery(true);});}
   float old=galleryScroll.verticalNormalizedPosition;
   if(renderedCards==null||!renderedCards.SequenceEqual(ordered)||renderedOwnedCount!=owned.Length||
      renderedFavoriteCount!=favorites.Length||!renderedOwnership.SequenceEqual(ownership)){
    ClearUI(gridRoot);cardButtons.Clear();
    renderedCards=ordered;renderedOwnedCount=owned.Length;renderedFavoriteCount=favorites.Length;renderedOwnership=ownership;
    float ownedTop=favorites.Length>0?DrawGalleryGroup("FavoriteGroup","收藏",favorites,0,true):0;
    float unownedTop=DrawGalleryGroup("OwnedGroup","已拥有",owned,ownedTop,true);
    float bottom=DrawGalleryGroup("UnownedGroup","未拥有",unowned,unownedTop,false);
    gridRoot.sizeDelta=new Vector2(1322,Mathf.Max(galleryScroll.viewport.rect.height,bottom));
   }
   foreach(var pair in cardButtons){
    var power=pair.Value.transform.Find("CharacterPower").GetComponent<UI.Text>();
    power.text=profile.Owned[pair.Key]?GalleryPowerText(pair.Key):"";
   }
   Canvas.ForceUpdateCanvases();galleryScroll.StopMovement();galleryScroll.verticalNormalizedPosition=reset?1:old;
   DrawGalleryDetail();
  }

  float DrawGalleryGroup(string id,string title,int[] cards,float y,bool owned){
   const float width=1322, rowHeight=492, head=50;
   float rows=cards.Length==0?63:Mathf.Ceil(cards.Length/4f)*rowHeight;
   var group=UNode(gridRoot,id,0,y,width,head+rows+12);
   UText(group,"Heading",title+"  "+cards.Length,8,0,205,32,23,owned?InkText:InkMuted,TextAnchor.MiddleLeft);
   UPanel(group,"Separator",195,16,1118,1,InkBorder);
   if(cards.Length==0)
    UText(group,"Empty",owned?"没有符合条件的已拥有角色":"没有符合条件的未拥有角色",10,38,1250,36,17,InkMuted,TextAnchor.MiddleLeft);
   for(int n=0;n<cards.Length;n++)DrawGalleryCard(group,cards[n],6+n%4*330,head+n/4*rowHeight,profile.Owned[cards[n]]);
   return y+head+rows+12;
  }

  void DrawGalleryCard(Transform group,int index,float x,float y,bool owned){
   var card=Data.Cards[index];
   var button=UButton(group,"Card_"+card.Id,"",x,y,316,474,()=>OpenGalleryCard(index));
   cardButtons[index]=button;
   button.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
   var r=button.transform;
   Color surface=owned?InkPaper:GalleryLockedCard;
   Color ink=owned?uiPaper:InkMuted;
   button.targetGraphic.color=surface;
   DrawRarityBackdrop(r,"PortraitMat",card.Rarity,10,46,296,278);
   DrawRarityBadge(r,card.Rarity,10,2,55,42,leftAligned:true);
   UText(r,"CatalogNumber","No. "+card.CatalogNumber.ToString("D3"),146,10,107,28,15,InkMuted,TextAnchor.MiddleRight);
   DrawFavoriteButton(r,index,264,2);
   UArt(r,index,16,44,284,278,CharacterCardPortraitRegion(index),muted:!owned);
   UText(r,"Name",card.Name,16,329,284,33,24,ink);
   var theme=UText(r,"Theme",card.Title,16,364,244,25,17,ink,TextAnchor.MiddleLeft);
   DrawCharacterRole(r,card,16+Mathf.Min(244,Mathf.Ceil(theme.preferredWidth))+5,364,25);
   DrawCardPotentialChart(r,card);
   var power=UText(r,"CharacterPower","",126,441,172,26,16,uiDim,TextAnchor.MiddleRight);
   power.supportRichText=true;power.gameObject.SetActive(owned);
   var focus=button.GetComponent<GalleryFocus>();
   focus.CardSurface=(UI.Image)button.targetGraphic;
   focus.CardFocusSprite=RoundedSurface(false,cardFocus:true);
   focus.CardNormalTint=surface;
  }
  string GalleryPowerText(int index)=>FormatGallerySkillText("战斗力 ["+RoundPower(index)+"]");

  void DrawFavoriteButton(Transform parent,int index,float x,float y,float lift=0){
   bool marked=profile.IsFavoriteCard(index);
   var button=UButton(parent,"Favorite_"+Data.Cards[index].Id,marked?"★":"☆",x,y,42,42,
    ()=>{if(Change(()=>profile.ToggleFavoriteCard(index)))RefreshGallery(false);});
   button.targetGraphic.color=Color.clear;
   var label=button.GetComponentInChildren<UI.Text>();
   label.rectTransform.anchoredPosition=new Vector2(0,lift);
   label.rectTransform.sizeDelta=new Vector2(42,42);
   label.fontSize=34;label.color=marked?new Color32(196,139,26,255):InkMuted;
  }

  readonly Dictionary<string,Sprite> rarityBadges=new Dictionary<string,Sprite>();
  readonly Dictionary<string,Sprite> alignedRarityBadges=new Dictionary<string,Sprite>();
  readonly Dictionary<string,Sprite> detailRarityBadges=new Dictionary<string,Sprite>();
  const string RarityResourcePrefix="Interface/Rarity/";
  static string RarityResource(string rank)=>RarityResourcePrefix+(rank=="C"?"V0170/C":rank);
  Sprite RarityBadge(string rank){
   Sprite sprite;
   if(!rarityBadges.TryGetValue(rank,out sprite)){
    sprite=Resources.Load<Sprite>(RarityResource(rank));
    rarityBadges[rank]=sprite;
   }
   return sprite;
  }
  // Sprite rects are measured bottom-origin bounds of the painted marks.
  static Rect DetailBadgeBounds(string rank){
   switch(rank){
    case "C":return new Rect(389,142,670,803);
    case "B":return new Rect(62,17,133,156);
    case "A":return new Rect(37,14,183,156);
    default:return new Rect(24,12,209,178);
   }
  }
  void DrawRarityBadge(Transform parent,string rank,float x,float y,float width,float height,bool leftAligned=false,bool matchVisibleHeight=false){
   var sprite=RarityBadge(rank);
   if(sprite==null){
    UText(parent,"Rarity",rank,x,y,width,height,18,uiAccent,TextAnchor.MiddleCenter);
    return;
   }
   if(matchVisibleHeight){
    if(!detailRarityBadges.TryGetValue(rank,out var crop)){
     var bounds=DetailBadgeBounds(rank);
     crop=Sprite.Create(sprite.texture,bounds,new Vector2(.5f,.5f),sprite.pixelsPerUnit);
     detailRarityBadges[rank]=crop;
    }
    sprite=crop;
    width=height*sprite.rect.width/sprite.rect.height;
   }else if(leftAligned){
    float scale=Math.Min(width/sprite.rect.width,height/sprite.rect.height);
    float actualHeight=sprite.rect.height*scale;
    // The four original badges have different transparent margins inside the same 256 px canvas.
    if(!alignedRarityBadges.TryGetValue(rank,out var cut)){
     var rect=sprite.rect;
     float trim=DetailBadgeBounds(rank).x-rect.x;
     cut=Sprite.Create(sprite.texture,new Rect(rect.x+trim,rect.y,rect.width-trim,rect.height),new Vector2(.5f,.5f),sprite.pixelsPerUnit);
     alignedRarityBadges[rank]=cut;
    }
    sprite=cut;
    y+=(height-actualHeight)/2;width=sprite.rect.width*scale;height=actualHeight;
   }
   var badge=UNode(parent,"Rarity_"+rank,x,y,width,height).gameObject.AddComponent<UI.Image>();
   badge.sprite=sprite;
   badge.preserveAspect=true;
   badge.raycastTarget=false;
  }
 }
}
