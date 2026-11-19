using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  readonly CatalogFilter catalog=new CatalogFilter();
  RectTransform galleryRoot,gridRoot,detailRoot,dialogRoot;
  GameObject galleryCanvas;UI.ScrollRect galleryScroll;UI.InputField gallerySearch;
  UI.Text galleryResources,galleryToast;
  UI.Button mineTab,allTab;readonly List<UI.Button> filterButtons=new List<UI.Button>();
  readonly Dictionary<int,UI.Button> cardButtons=new Dictionary<int,UI.Button>();
  readonly List<UI.Text> measuredText=new List<UI.Text>();
  int detailTab;bool galleryDirty=true;string previousPage="";
  float toastUntil,detailFade=1;CanvasGroup detailGroup;
  static Color uiPaper=>InkText;
  static Color uiPanel=>InkPaper;
  static readonly Color uiDim=new Color32(55,67,62,255),uiAccent=new Color32(105,70,39,255);
  static readonly string[] FeatureRoutes={"挑战","角色","编队","养成","装备","召唤"};
  static string[] SortLabels={"上阵优先","稀有度优先","角色名称","培养程度"};

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
  void ClearUI(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
  void SetButtonActive(UI.Button b,bool active){
   b.targetGraphic.color=!b.interactable?InkDisabled:active?InkDark:InkLight;
   b.GetComponentInChildren<UI.Text>().color=active&&b.interactable?InkIvory:InkText;
  }
  UI.ScrollRect UScroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content,bool showBar=true){
   var viewport=UPanel(parent,name,x,y,w,h,Color.clear,true);viewport.gameObject.AddComponent<UI.RectMask2D>();
   var scroll=viewport.gameObject.AddComponent<UI.ScrollRect>();content=UNode(viewport,"Content",0,0,w-(showBar?14:0),h);
   scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
   if(!showBar)return scroll;
   var track=UPanel(parent,name+"Track",x+w-12,y,10,h,InkPaper,true);
   var slide=UNode(track,"SlidingArea",0,0,10,h);
   slide.anchorMin=Vector2.zero;slide.anchorMax=Vector2.one;slide.pivot=new Vector2(.5f,.5f);slide.offsetMin=slide.offsetMax=Vector2.zero;
   var handle=UPanel(slide,"Handle",0,0,10,0,InkDark,true);
   // Scrollbar controls its handle anchors; explicit size or position offsets make it spill past the track.
   handle.pivot=new Vector2(.5f,.5f);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
   var bar=track.gameObject.AddComponent<UI.Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<UI.Image>();bar.direction=UI.Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
   return scroll;
  }
  void UArt(Transform parent,int i,float x,float y,float w,float h){
   var art=PortraitTexture(i);
   if(art!=null){
    float width=Math.Min(w,h*PortraitAspect(i)),height=width/PortraitAspect(i);
    var r=UNode(parent,"Portrait",x+(w-width)/2,y+(h-height)/2,width,height);var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=art;image.uvRect=new Rect(0,0,1,1);image.raycastTarget=false;
   }else{
    UPanel(parent,"ArtInset",x+8,y+6,w-16,h-12,new Color(.14f,.18f,.21f));
    UPanel(parent,"Accent",x+8,y+6,3,h-12,CardColor(i));
    UText(parent,"Monogram",Data.Cards[i].Name.Substring(0,1),x,y+h*.16f,w,h*.45f,58,CardColor(i),TextAnchor.MiddleCenter);
    UText(parent,"ArtPending","形象待制作",x,y+h-38,w,28,13,uiDim,TextAnchor.MiddleCenter);
   }
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
   UText(galleryChrome,"Section","角色收藏",232,28,500,40,22,InkIvory);
   galleryResources=UText(galleryChrome,"Resources","",900,27,650,40,19,InkOnDark,TextAnchor.MiddleRight);
   for(int i=0;i<FeatureRoutes.Length;i++){
    string target=FeatureRoutes[i];
    UButton(galleryChrome,"Nav"+target,target=="角色"?"图鉴":target,18,126+i*62,154,45,()=>NavigateGallery(target),selected:target=="角色");
   }
   UButton(galleryChrome,"AudioSettings","声音与显示",18,735,154,44,ShowAudioSettings);
   UText(galleryChrome,"Version","v"+Data.Version+"  /  预览版",20,840,160,30,13,uiDim);
   UButton(galleryChrome,"Nav主页","返回主页",18,819,154,47,()=>NavigateGallery("主页"));
   galleryListRoot=UNode(galleryRoot,"CardLibrary",0,0,1600,900);
   UText(galleryListRoot,"Title","图鉴",222,108,300,46,32,uiPaper);
   mineTab=UButton(galleryListRoot,"OwnedTab","我的角色",222,166,152,42,()=>{catalog.OwnedOnly=true;RefreshGallery(true);},true);
   allTab=UButton(galleryListRoot,"AllTab","全部角色",386,166,152,42,()=>{catalog.OwnedOnly=false;RefreshGallery(true);});
   var input=UPanel(galleryListRoot,"Search",558,166,354,42,InkLight,true);
   gallerySearch=input.gameObject.AddComponent<UI.InputField>();gallerySearch.textComponent=UText(input,"InputText","",12,7,328,28,17,uiPaper);gallerySearch.placeholder=UText(input,"Placeholder","搜索名字、形态、称号",12,7,328,28,17,uiDim);gallerySearch.characterLimit=60;
   gallerySearch.onValueChanged.AddListener(v=>{catalog.Search=v;RefreshGallery(true);});
   string[] filters={"稀有度","标签",SortLabels[0]};
   for(int i=0;i<filters.Length;i++){int n=i;filterButtons.Add(UButton(galleryListRoot,"Filter"+i,filters[i],932+i*208,166,198,42,()=>ShowFilterDropdown(n)));}
   UButton(galleryListRoot,"ResetFilters","清空条件",1422,218,126,32,ResetGalleryFilters);
   filterChipRoot=UNode(galleryListRoot,"FilterChips",222,218,1180,32);
   galleryScroll=UScroll(galleryListRoot,"CardScroll",222,258,1336,570,out gridRoot);
   detailRoot=UPanel(galleryRoot,"Detail",222,112,1336,716,uiPanel);
   detailGroup=detailRoot.gameObject.AddComponent<CanvasGroup>();
   galleryToast=UText(galleryRoot,"Toast","",232,845,1320,42,17,uiAccent);
   detailRoot.gameObject.SetActive(false);
  }
  void NavigateGallery(string target){
   if(running&&target!="挑战"){GalleryNotice("请先退出当前战斗，再切换功能。",false);return;}
   if(shellLoad.Active&&target!="加载")shellLoad.Cancel();
   if(page=="角色"&&IsWorkshop(target))browseCard=selectedCard;
   if(IsWorkshop(page)&&target=="角色"&&browseCard>=0)selectedCard=browseCard;
   CloseFilterDropdown();CloseGalleryDialog();page=target;autoRepeat=false;notice="";galleryDirty=true;
   galleryCanvas.SetActive(IsModernPage(target));previousPage="";UpdateGallery();
   if(target=="角色"&&EventSystem.current!=null){
    if(galleryDetailOpen&&selectedCard>=0)EventSystem.current.SetSelectedGameObject(ActiveBackButton(detailRoot,"BackToCards"));
    else if(cardButtons.ContainsKey(selectedCard))EventSystem.current.SetSelectedGameObject(cardButtons[selectedCard].gameObject);
   }
   if(target=="主页"&&EventSystem.current!=null&&homeRoot!=null)EventSystem.current.SetSelectedGameObject(ActiveBackButton(homeRoot,"HomeNav角色"));
   if(target=="召唤"&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(ActiveBackButton(summonRoot,saiyanPool?"SaiyanPool":"ClassicPool"));
  }
  void UpdateGallery(){
   if(IsModernPage(page)){EnsureGallery();if(!galleryCanvas.activeSelf)galleryCanvas.SetActive(true);
    if((previousPage!=page||galleryDirty)&&dialogRoot==null){if(page=="角色")RefreshGallery(false);else if(IsWorkshop(page))DrawWorkshop();else if(page=="主页")DrawHome();else if(page=="召唤")DrawModernSummon();galleryDirty=false;RefreshModernFrame();}
    if(Input.GetKeyDown(KeyCode.Escape)){if(filterDropdown!=null)CloseFilterDropdown();else if(dialogRoot!=null)CloseGalleryDialog();else if(IsWorkshop(page))NavigateGallery("角色");else if(page=="角色"&&galleryDetailOpen)ReturnToCards();else if(page=="角色"||page=="召唤")NavigateGallery("主页");else if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    if(detailGroup!=null){detailFade=Math.Min(1,detailFade+Time.unscaledDeltaTime*8);detailGroup.alpha=audioSettings!=null&&audioSettings.ReducedMotion?1:detailFade;}
    galleryToast.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"";
    if(page=="主页")RefreshHomeNotice();
   }else if(galleryCanvas!=null&&galleryCanvas.activeSelf){CloseFilterDropdown();galleryCanvas.SetActive(false);}
   previousPage=page;
  }
  void GalleryNotice(string message,bool success=true){notice=message;toastUntil=Time.unscaledTime+5;PlayUISound(success?1:2);galleryDirty=true;}
  void ResetGalleryFilters(){CloseFilterDropdown();catalog.Rarity=catalog.Role=catalog.Tag=catalog.Search="";catalog.Sort=0;gallerySearch.SetTextWithoutNotify("");RefreshGallery(true);}
  void RefreshGallery(bool reset){
   galleryDirty=false;if(galleryCanvas==null)return;
   int[] results=catalog.Results(profile);selectedCard=CatalogFilter.Reconcile(selectedCard,results);
   RefreshFeatureHeaderCount();
   SetButtonActive(mineTab,catalog.OwnedOnly);SetButtonActive(allTab,!catalog.OwnedOnly);
   string[] labels={catalog.Rarity==""?"稀有度":catalog.Rarity,catalog.Tag==""?"标签":catalog.Tag,SortLabels[catalog.Sort]};
   for(int i=0;i<labels.Length;i++)filterButtons[i].GetComponentInChildren<UI.Text>().text=labels[i]+"  ▾";
   ClearUI(filterChipRoot);int chips=0;
   for(int n=0;n<2;n++){int kind=n;string value=n==0?catalog.Rarity:catalog.Tag;if(value=="")continue;UButton(filterChipRoot,"ClearFilter"+n,value+" ×",chips++*188,0,178,32,()=>{if(kind==0)catalog.Rarity="";else catalog.Tag="";RefreshGallery(true);});}
   float old=galleryScroll.verticalNormalizedPosition;bool rebuild=renderedCards==null||!renderedCards.SequenceEqual(results);
   if(rebuild){ClearUI(gridRoot);cardButtons.Clear();renderedCards=(int[])results.Clone();}
   gridRoot.sizeDelta=new Vector2(1322,Math.Max(570,((results.Length+3)/4)*448));
   for(int n=0;rebuild&&n<results.Length;n++){
    int index=results[n];var c=Data.Cards[index];float x=6+n%4*330,y=6+n/4*448;
    var b=UButton(gridRoot,"Card_"+c.Id,"",x,y,316,430,()=>OpenGalleryCard(index));
    cardButtons[index]=b;b.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
    var r=b.GetComponent<RectTransform>();b.targetGraphic.color=InkPaper;
    UPanel(r,"PortraitMat",10,46,296,278,InkMist);
    DrawRarityBadge(r,c.Rarity,10,2,55,42,leftAligned:true);
    UText(r,"CharacterPower","",140,11,160,26,17,uiDim,TextAnchor.MiddleRight);
    UArt(r,index,16,44,284,278);
    UText(r,"Name",c.Name,16,330,284,30,24,uiPaper);
    UText(r,"Theme",c.Title,16,363,284,28,17,uiDim);
    UText(r,"CharacterType",c.TypeText,16,398,160,26,16,uiAccent);
    UText(r,"Unowned","未拥有",190,398,110,26,16,uiDim,TextAnchor.UpperRight);
    var focus=b.GetComponent<GalleryFocus>();
    focus.CardSurface=(UI.Image)b.targetGraphic;focus.CardFocusSprite=RoundedSurface(false,cardFocus:true);focus.CardNormalTint=InkPaper;
   }
   foreach(var pair in cardButtons){
    var r=pair.Value.transform;
    bool owned=profile.Owned[pair.Key];
    var power=r.Find("CharacterPower").GetComponent<UI.Text>();
    power.text=owned?"战斗力："+profile.CharacterPower(pair.Key):"";
    power.gameObject.SetActive(owned);
    r.Find("Unowned").gameObject.SetActive(!owned);
   }
   if(rebuild&&results.Length==0){
    UText(gridRoot,"EmptyTitle","没有符合条件的角色",350,100,620,45,25,uiPaper,TextAnchor.MiddleCenter);
    UText(gridRoot,"EmptyHelp","尝试清空筛选，或切换到全部角色。",350,151,620,40,18,uiDim,TextAnchor.MiddleCenter);
    UButton(gridRoot,"EmptyReset","清空筛选",550,220,220,44,ResetGalleryFilters,true);
   }
   Canvas.ForceUpdateCanvases();galleryScroll.StopMovement();galleryScroll.verticalNormalizedPosition=reset?1:old;
   DrawGalleryDetail();
  }
  readonly Dictionary<string,Sprite> rarityBadges=new Dictionary<string,Sprite>();
  readonly Dictionary<string,Sprite> alignedRarityBadges=new Dictionary<string,Sprite>();
  const string RarityResourcePrefix="Interface/Rarity/";
  Sprite RarityBadge(string rank){
   Sprite sprite;
   if(!rarityBadges.TryGetValue(rank,out sprite)){
    sprite=Resources.Load<Sprite>(RarityResourcePrefix+rank);
    rarityBadges[rank]=sprite;
   }
   return sprite;
  }
  void DrawRarityBadge(Transform parent,string rank,float x,float y,float width,float height,bool leftAligned=false){
   var sprite=RarityBadge(rank);
   if(sprite==null){
    UText(parent,"Rarity",rank,x,y,width,height,18,uiAccent,TextAnchor.MiddleCenter);
    return;
   }
   if(leftAligned){
    float scale=Math.Min(width/sprite.rect.width,height/sprite.rect.height);
    float actualHeight=sprite.rect.height*scale;
    // The four original badges have different transparent margins inside the same 256 px canvas.
    if(!alignedRarityBadges.TryGetValue(rank,out var cut)){
     var rect=sprite.rect;
     int blank=rank=="C"?71:rank=="B"?62:rank=="A"?37:24;
     float trim=rect.width*blank/256;
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
