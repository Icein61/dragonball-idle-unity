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
  UI.Text galleryCount,galleryResources,galleryFilterText,galleryToast;
  UI.Button mineTab,allTab;readonly List<UI.Button> filterButtons=new List<UI.Button>();
  readonly Dictionary<int,UI.Button> cardButtons=new Dictionary<int,UI.Button>();
  readonly List<UI.Text> measuredText=new List<UI.Text>();
  int detailTab;bool galleryDirty=true;string previousPage="";
  float toastUntil,detailFade=1;CanvasGroup detailGroup;
  readonly Color uiPaper=new Color(.10f,.15f,.21f),uiInk=new Color(.10f,.13f,.17f),uiDim=new Color(.34f,.40f,.46f),uiAccent=new Color(.76f,.27f,.06f),uiPanel=new Color(.99f,.985f,.97f);
  static string[] SortLabels={"上阵优先","稀有度优先","角色名称","培养程度"};

  RectTransform UNode(Transform parent,string name,float x,float y,float w,float h){
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
   r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
  }
  RectTransform UPanel(Transform parent,string name,float x,float y,float w,float h,Color color,bool raycast=false){
   var r=UNode(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<UI.Image>();image.color=color;image.raycastTarget=raycast;
   if(w>=32&&h>=24&&color.a>0&&name!="Background"&&name!="Header"&&name!="Sidebar"&&name!="ModalBackdrop"){image.sprite=RoundedSurface(name=="Detail"||name=="Modal");image.type=UI.Image.Type.Sliced;}
   return r;
  }
  UI.Text UText(Transform parent,string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.UpperLeft){
   var r=UNode(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<UI.Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;
   t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
  }
  UI.Button UButton(Transform parent,string name,string value,float x,float y,float w,float h,Action action,bool primary=false,bool enabled=true){
   var r=UPanel(parent,name,x,y,w,h,primary?uiAccent:new Color(.90f,.925f,.95f),true);
   var b=r.gameObject.AddComponent<UI.Button>();b.targetGraphic=r.GetComponent<UI.Image>();b.interactable=enabled;
   var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.18f,1.18f,1.18f);c.pressedColor=new Color(.78f,.78f,.78f);c.selectedColor=new Color(1.1f,1.1f,1.1f);c.disabledColor=new Color(.42f,.42f,.42f,.7f);c.fadeDuration=audioSettings!=null&&audioSettings.ReducedMotion?0:.12f;b.colors=c;
   UText(r,"Label",value,10,0,w-20,h,18,primary?Color.white:uiPaper,TextAnchor.MiddleCenter);
   var outline=r.gameObject.AddComponent<UI.Outline>();outline.effectColor=new Color(.12f,.44f,.80f);outline.effectDistance=new Vector2(3,-3);outline.enabled=false;r.gameObject.AddComponent<GalleryFocus>().Outline=outline;
   b.onClick.AddListener(()=>{if(!b.interactable)return;PlayUISound(0);action();});return b;
  }
  void ClearUI(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
  void SetButtonActive(UI.Button b,bool active){b.targetGraphic.color=active?uiAccent:new Color(.90f,.925f,.95f);b.GetComponentInChildren<UI.Text>().color=active?Color.white:uiPaper;}
  UI.ScrollRect UScroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content){
   var viewport=UPanel(parent,name,x,y,w,h,Color.clear,true);viewport.gameObject.AddComponent<UI.RectMask2D>();
   var scroll=viewport.gameObject.AddComponent<UI.ScrollRect>();content=UNode(viewport,"Content",0,0,w-14,h);
   scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
   var track=UPanel(parent,name+"Track",x+w-12,y,10,h,new Color(.83f,.86f,.89f),true);
   var handle=UPanel(track,"Handle",0,0,10,60,new Color(.40f,.47f,.54f),true);
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
   UPanel(galleryChrome,"Background",0,0,1600,900,new Color(.95f,.955f,.96f));
   UPanel(galleryChrome,"Header",0,0,1600,90,new Color(.065f,.09f,.12f));
   UPanel(galleryChrome,"Sidebar",0,90,190,810,new Color(.99f,.985f,.97f));
   UPanel(galleryChrome,"HeaderLine",218,87,1344,2,uiAccent);
   UText(galleryChrome,"Brand",Data.GameTitle,25,20,180,40,26,Color.white);
   UText(galleryChrome,"Section","角色收藏",232,28,500,40,22,new Color(.77f,.84f,.91f));
   galleryResources=UText(galleryChrome,"Resources","",900,27,650,40,19,Color.white,TextAnchor.MiddleRight);
   string[] names={"挑战","角色","编队","养成","装备","召唤","说明"};
   for(int i=0;i<names.Length;i++){string target=names[i];UButton(galleryChrome,"Nav"+target,target,18,126+i*68,154,50,()=>NavigateGallery(target),target=="角色");}
   UButton(galleryChrome,"AudioSettings","声音与显示",18,735,154,44,ShowAudioSettings);
   UText(galleryChrome,"Version","v"+Data.Version+"  /  预览版",20,840,160,30,13,uiDim);
   ShellButton(galleryChrome,"Nav主页","返回主页",18,665,154,44,()=>NavigateGallery("主页"));
   galleryListRoot=UNode(galleryRoot,"CardLibrary",0,0,1600,900);
   UText(galleryListRoot,"Title","人物志",222,108,300,46,32,uiPaper);
   galleryCount=UText(galleryListRoot,"CollectionCount","",1040,119,510,30,17,uiDim,TextAnchor.MiddleRight);
   mineTab=UButton(galleryListRoot,"OwnedTab","我的角色",222,166,152,42,()=>{catalog.OwnedOnly=true;RefreshGallery(true);},true);
   allTab=UButton(galleryListRoot,"AllTab","全部图鉴",386,166,152,42,()=>{catalog.OwnedOnly=false;RefreshGallery(true);});
   var input=UPanel(galleryListRoot,"Search",558,166,354,42,Color.white,true);
   gallerySearch=input.gameObject.AddComponent<UI.InputField>();gallerySearch.textComponent=UText(input,"InputText","",12,7,328,28,17,uiPaper);gallerySearch.placeholder=UText(input,"Placeholder","搜索名字、形态、称号",12,7,328,28,17,uiDim);gallerySearch.characterLimit=60;
   gallerySearch.onValueChanged.AddListener(v=>{catalog.Search=v;RefreshGallery(true);});
   string[] filters={"稀有度","定位","身份","排序"};
   for(int i=0;i<4;i++){int n=i;filterButtons.Add(UButton(galleryListRoot,"Filter"+i,filters[i],932+i*158,166,148,42,()=>ShowFilter(n)));}
   UButton(galleryListRoot,"ResetFilters","清空条件",1422,218,126,32,ResetGalleryFilters);
   galleryFilterText=UText(galleryListRoot,"AppliedFilters","",222,218,1180,30,16,uiDim);
   filterChipRoot=UNode(galleryListRoot,"FilterChips",222,218,1180,32);
   galleryScroll=UScroll(galleryListRoot,"CardScroll",222,258,1336,570,out gridRoot);
   detailRoot=UPanel(galleryRoot,"Detail",222,112,1336,716,uiPanel);
   detailGroup=detailRoot.gameObject.AddComponent<CanvasGroup>();
   galleryToast=UText(galleryRoot,"Toast","",232,845,1320,42,17,uiAccent);
   RefreshGallery(true);
   detailRoot.gameObject.SetActive(false);
  }
  void NavigateGallery(string target){
   if(running&&target!="挑战"){GalleryNotice("请先退出当前战斗，再切换功能。",false);return;}
   if(shellLoad.Active&&target!="加载")shellLoad.Cancel();
   if(page=="角色"&&IsWorkshop(target))browseCard=selectedCard;
   if(IsWorkshop(page)&&target=="角色"&&browseCard>=0)selectedCard=browseCard;
   CloseGalleryDialog();page=target;autoRepeat=false;notice="";galleryDirty=true;
   galleryCanvas.SetActive(IsModernPage(target));previousPage="";UpdateGallery();
   if(target=="角色"&&EventSystem.current!=null){
    if(galleryDetailOpen&&selectedCard>=0)EventSystem.current.SetSelectedGameObject(ActiveBackButton(detailRoot,"BackToCards"));
    else if(cardButtons.ContainsKey(selectedCard))EventSystem.current.SetSelectedGameObject(cardButtons[selectedCard].gameObject);
   }
   if(target=="主页"&&EventSystem.current!=null&&homeRoot!=null)EventSystem.current.SetSelectedGameObject(ActiveBackButton(homeRoot,"HomeNav角色"));
  }
  void UpdateGallery(){
   if(IsModernPage(page)){EnsureGallery();if(!galleryCanvas.activeSelf)galleryCanvas.SetActive(true);
    if((previousPage!=page||galleryDirty)&&dialogRoot==null){if(page=="角色")RefreshGallery(false);else if(IsWorkshop(page))DrawWorkshop();else if(page=="主页")DrawHome();galleryDirty=false;RefreshModernFrame();}
    if(Input.GetKeyDown(KeyCode.Escape)){if(dialogRoot!=null)CloseGalleryDialog();else if(IsWorkshop(page))NavigateGallery("角色");else if(page=="角色"&&galleryDetailOpen)ReturnToCards();else if(page=="角色")NavigateGallery("主页");else if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    if(detailGroup!=null){detailFade=Math.Min(1,detailFade+Time.unscaledDeltaTime*8);detailGroup.alpha=audioSettings!=null&&audioSettings.ReducedMotion?1:detailFade;}
    galleryToast.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"";
    if(page=="主页")RefreshHomeNotice();
   }else if(galleryCanvas!=null&&galleryCanvas.activeSelf)galleryCanvas.SetActive(false);
   previousPage=page;
  }
  void GalleryNotice(string message,bool success=true){notice=message;toastUntil=Time.unscaledTime+5;PlayUISound(success?1:2);galleryDirty=true;}
  void ResetGalleryFilters(){catalog.Rarity=catalog.Role=catalog.Tag=catalog.Search="";catalog.Sort=0;gallerySearch.SetTextWithoutNotify("");RefreshGallery(true);}
  void RefreshGallery(bool reset){
   galleryDirty=false;if(galleryCanvas==null)return;
   int[] results=catalog.Results(profile);selectedCard=CatalogFilter.Reconcile(selectedCard,results);
   galleryResources.text="素材  "+profile.Materials+"     晶石  "+profile.Crystals+"     珠子  "+profile.Gems;
   galleryCount.text="已收藏 "+profile.Owned.Count(x=>x)+" / "+Data.Cards.Length+"    ·    当前显示 "+results.Length+" 张";
   SetButtonActive(mineTab,catalog.OwnedOnly);SetButtonActive(allTab,!catalog.OwnedOnly);
   string[] labels={catalog.Rarity==""?"全部稀有度":catalog.Rarity,catalog.Role==""?"全部定位":catalog.Role,catalog.Tag==""?"全部身份":catalog.Tag,SortLabels[catalog.Sort]};
   for(int i=0;i<4;i++)filterButtons[i].GetComponentInChildren<UI.Text>().text=labels[i]+"  ▾";
   galleryFilterText.text=string.Join("   ·   ",labels.Take(3).Where(label=>!label.StartsWith("全部")).ToArray());
   if(galleryFilterText.text=="")galleryFilterText.text="点击角色查看详情 · 卡面完整显示，不裁切原图";
   ClearUI(filterChipRoot);int chips=0;
   for(int n=0;n<3;n++){int kind=n;string value=new[]{catalog.Rarity,catalog.Role,catalog.Tag}[n];if(value=="")continue;UButton(filterChipRoot,"ClearFilter"+n,value+" ×",chips++*188,0,178,32,()=>{if(kind==0)catalog.Rarity="";else if(kind==1)catalog.Role="";else catalog.Tag="";RefreshGallery(true);});}
   galleryFilterText.gameObject.SetActive(chips==0);
   float old=galleryScroll.verticalNormalizedPosition;bool rebuild=renderedCards==null||!renderedCards.SequenceEqual(results);
   if(rebuild){ClearUI(gridRoot);cardButtons.Clear();renderedCards=(int[])results.Clone();}
   gridRoot.sizeDelta=new Vector2(1322,Math.Max(570,((results.Length+3)/4)*448));
   for(int n=0;rebuild&&n<results.Length;n++){
    int index=results[n];var c=Data.Cards[index];float x=n%4*334,y=n/4*448;
    var b=UButton(gridRoot,"Card_"+c.Id,"",x,y,316,430,()=>OpenGalleryCard(index));
    cardButtons[index]=b;b.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
    var r=b.GetComponent<RectTransform>();b.targetGraphic.color=Color.white;
    DrawRarityBadge(r,c.Rarity,12,1,69,47);
    UText(r,"Owned","",140,14,160,25,16,uiDim,TextAnchor.UpperRight);
    UArt(r,index,16,44,284,278);
    UText(r,"Name",c.Name,16,330,284,30,24,uiPaper);
    UText(r,"Theme",c.Title+" · "+c.Form,16,363,284,28,17,uiDim);
    UText(r,"Keywords",c.KeywordText,16,398,284,26,16,uiAccent);
   }
   foreach(var pair in cardButtons){int slot=Array.IndexOf(profile.Team,pair.Key);pair.Value.transform.Find("Owned").GetComponent<UI.Text>().text=slot>=0?"上阵 "+(slot+1):profile.Owned[pair.Key]?"已拥有":"未拥有";}
   if(rebuild&&results.Length==0){
    UText(gridRoot,"EmptyTitle","没有符合条件的角色",350,100,620,45,25,uiPaper,TextAnchor.MiddleCenter);
    UText(gridRoot,"EmptyHelp","尝试清空筛选，或切换到全部图鉴。",350,151,620,40,18,uiDim,TextAnchor.MiddleCenter);
    UButton(gridRoot,"EmptyReset","清空筛选",550,220,220,44,ResetGalleryFilters,true);
   }
   Canvas.ForceUpdateCanvases();galleryScroll.StopMovement();galleryScroll.verticalNormalizedPosition=reset?1:old;
   DrawGalleryDetail();
  }
  readonly Dictionary<string,Sprite> rarityBadges=new Dictionary<string,Sprite>();
  Sprite RarityBadge(string rank){
   Sprite sprite;
   if(!rarityBadges.TryGetValue(rank,out sprite)){
    sprite=Resources.Load<Sprite>("Interface/Rarity/"+rank);
    rarityBadges[rank]=sprite;
   }
   return sprite;
  }
  void DrawRarityBadge(Transform parent,string rank,float x,float y,float width,float height){
   var sprite=RarityBadge(rank);
   if(sprite==null){
    UText(parent,"Rarity",rank,x,y,width,height,18,uiAccent,TextAnchor.MiddleCenter);
    return;
   }
   var badge=UNode(parent,"Rarity_"+rank,x,y,width,height).gameObject.AddComponent<UI.Image>();
   badge.sprite=sprite;
   badge.preserveAspect=true;
   badge.raycastTarget=false;
  }
 }
}
