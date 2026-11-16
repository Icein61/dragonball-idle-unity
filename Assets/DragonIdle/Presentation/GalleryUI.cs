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
  readonly Color uiPaper=new Color(.92f,.90f,.84f),uiInk=new Color(.10f,.13f,.17f),uiDim=new Color(.61f,.66f,.69f),uiAccent=new Color(1f,.62f,.27f),uiPanel=new Color(.09f,.13f,.17f);
  static string[] SortLabels={"上阵优先","稀有度优先","角色名称","培养程度"};

  RectTransform UNode(Transform parent,string name,float x,float y,float w,float h){
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
   r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
  }
  RectTransform UPanel(Transform parent,string name,float x,float y,float w,float h,Color color,bool raycast=false){
   var r=UNode(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<UI.Image>();image.color=color;image.raycastTarget=raycast;return r;
  }
  UI.Text UText(Transform parent,string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.UpperLeft){
   var r=UNode(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<UI.Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;
   t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
  }
  UI.Button UButton(Transform parent,string name,string value,float x,float y,float w,float h,Action action,bool primary=false,bool enabled=true){
   var r=UPanel(parent,name,x,y,w,h,primary?uiAccent:new Color(.16f,.21f,.26f),true);
   var b=r.gameObject.AddComponent<UI.Button>();b.targetGraphic=r.GetComponent<UI.Image>();b.interactable=enabled;
   var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.18f,1.18f,1.18f);c.pressedColor=new Color(.78f,.78f,.78f);c.selectedColor=new Color(1.1f,1.1f,1.1f);c.disabledColor=new Color(.42f,.42f,.42f,.7f);c.fadeDuration=audioSettings!=null&&audioSettings.ReducedMotion?0:.12f;b.colors=c;
   UText(r,"Label",value,10,0,w-20,h,17,primary?uiInk:uiPaper,TextAnchor.MiddleCenter);
   b.onClick.AddListener(()=>{if(!b.interactable)return;PlayUISound(0);action();});return b;
  }
  void ClearUI(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
  void SetButtonActive(UI.Button b,bool active){b.targetGraphic.color=active?uiAccent:new Color(.16f,.21f,.26f);b.GetComponentInChildren<UI.Text>().color=active?uiInk:uiPaper;}
  UI.ScrollRect UScroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content){
   var viewport=UPanel(parent,name,x,y,w,h,Color.clear,true);viewport.gameObject.AddComponent<UI.RectMask2D>();
   var scroll=viewport.gameObject.AddComponent<UI.ScrollRect>();content=UNode(viewport,"Content",0,0,w-14,h);
   scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
   var track=UPanel(parent,name+"Track",x+w-7,y,5,h,new Color(.15f,.19f,.22f),true);
   var handle=UPanel(track,"Handle",0,0,5,60,new Color(.48f,.51f,.50f),true);
   var bar=track.gameObject.AddComponent<UI.Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<UI.Image>();bar.direction=UI.Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
   return scroll;
  }
  void UArt(Transform parent,int i,float x,float y,float w,float h){
   var art=PortraitTexture(i);
   if(art!=null){
    float width=Math.Min(w,h*PortraitAspect(i)),height=width/PortraitAspect(i);
    var r=UNode(parent,"Portrait",x+(w-width)/2,y+(h-height)/2,width,height);var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=art;image.uvRect=ArtUV(i,0);image.raycastTarget=false;
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
   UPanel(galleryRoot,"Background",0,0,1600,900,new Color(.035f,.055f,.075f));
   UPanel(galleryRoot,"Header",0,0,1600,90,new Color(.065f,.09f,.12f));
   UPanel(galleryRoot,"Sidebar",0,90,190,810,new Color(.055f,.075f,.10f));
   UPanel(galleryRoot,"HeaderLine",218,87,1344,2,uiAccent);
   UText(galleryRoot,"Brand","试炼前线",25,20,180,40,29,uiPaper);
   UText(galleryRoot,"Section","龙珠  /  角色收藏",232,28,500,40,22,uiDim);
   galleryResources=UText(galleryRoot,"Resources","",900,27,650,40,19,uiPaper,TextAnchor.MiddleRight);
   string[] names={"挑战","角色","编队","养成","装备","召唤","说明"};
   for(int i=0;i<names.Length;i++){string target=names[i];UButton(galleryRoot,"Nav"+target,target,18,126+i*68,154,50,()=>NavigateGallery(target),target=="角色");}
   UButton(galleryRoot,"AudioSettings","声音与显示",18,735,154,44,ShowAudioSettings);
   UText(galleryRoot,"Version","v"+Data.Version+"  /  预览版",20,840,160,30,13,uiDim);
   UText(galleryRoot,"Title","人物志",222,113,300,46,32,uiPaper);
   galleryCount=UText(galleryRoot,"CollectionCount","",222,164,700,30,17,uiDim);
   mineTab=UButton(galleryRoot,"OwnedTab","我的角色",222,205,160,40,()=>{catalog.OwnedOnly=true;RefreshGallery(true);},true);
   allTab=UButton(galleryRoot,"AllTab","全部图鉴",392,205,160,40,()=>{catalog.OwnedOnly=false;RefreshGallery(true);});
   var input=UPanel(galleryRoot,"Search",570,205,354,40,new Color(.12f,.17f,.21f),true);
   gallerySearch=input.gameObject.AddComponent<UI.InputField>();gallerySearch.textComponent=UText(input,"InputText","",12,7,328,28,17,uiPaper);gallerySearch.placeholder=UText(input,"Placeholder","搜索名字、形态、称号",12,7,328,28,17,uiDim);gallerySearch.characterLimit=60;
   gallerySearch.onValueChanged.AddListener(v=>{catalog.Search=v;RefreshGallery(true);});
   string[] filters={"稀有度","定位","身份","排序"};
   for(int i=0;i<4;i++){int n=i;filterButtons.Add(UButton(galleryRoot,"Filter"+i,filters[i],222+i*157,260,147,38,()=>ShowFilter(n)));}
   UButton(galleryRoot,"ResetFilters","重置",854,260,70,38,ResetGalleryFilters);
   galleryFilterText=UText(galleryRoot,"AppliedFilters","",222,307,700,35,14,uiDim);
   galleryScroll=UScroll(galleryRoot,"CardScroll",222,349,708,478,out gridRoot);
   detailRoot=UPanel(galleryRoot,"Detail",958,118,604,709,uiPanel);
   detailGroup=detailRoot.gameObject.AddComponent<CanvasGroup>();
   galleryToast=UText(galleryRoot,"Toast","",232,845,1320,42,17,uiAccent);
   RefreshGallery(true);
  }
  void NavigateGallery(string target){
   CloseGalleryDialog();page=target;autoRepeat=false;notice="";galleryDirty=true;
   galleryCanvas.SetActive(target=="角色");previousPage=target=="角色"?"":"角色";
  }
  void UpdateGallery(){
   if(page=="角色"){EnsureGallery();if(!galleryCanvas.activeSelf)galleryCanvas.SetActive(true);if((previousPage!="角色"||galleryDirty)&&dialogRoot==null)RefreshGallery(false);
    if(Input.GetKeyDown(KeyCode.Escape)){if(dialogRoot!=null)CloseGalleryDialog();else if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    if(detailGroup!=null){detailFade=Math.Min(1,detailFade+Time.unscaledDeltaTime*8);detailGroup.alpha=audioSettings!=null&&audioSettings.ReducedMotion?1:detailFade;}
    galleryToast.text=saveError!=""?saveError:Time.unscaledTime<toastUntil?notice:"";
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
   galleryFilterText.text=catalog.OwnedOnly?"管理已拥有的角色 · 点击卡面查看属性与搭配":"浏览全部卡片 · 未拥有角色也可查看完整形象与获取途径";
   float old=galleryScroll.verticalNormalizedPosition;ClearUI(gridRoot);cardButtons.Clear();
   gridRoot.sizeDelta=new Vector2(694,Math.Max(478,((results.Length+2)/3)*272));
   for(int n=0;n<results.Length;n++){
    int index=results[n];var c=Data.Cards[index];float x=n%3*232,y=n/3*272;
    var b=UButton(gridRoot,"Card_"+c.Id,"",x,y,219,258,()=>{selectedCard=index;detailFade=.35f;RefreshGallery(false);});
    cardButtons[index]=b;b.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
    var r=b.GetComponent<RectTransform>();b.targetGraphic.color=index==selectedCard?new Color(.24f,.29f,.31f):new Color(.10f,.15f,.19f);
    UPanel(r,"TopInk",0,0,219,index==selectedCard?4:2,index==selectedCard?uiAccent:CardColor(index));
    UText(r,"Rarity",c.Rarity,12,10,65,24,18,uiAccent);
    int slot=Array.IndexOf(profile.Team,index);UText(r,"Owned",slot>=0?"上阵 "+(slot+1):profile.Owned[index]?"已拥有":"未拥有",104,12,102,23,14,uiDim,TextAnchor.UpperRight);
    UArt(r,index,14,36,191,127);
    UText(r,"Name",c.Name,12,169,195,28,22,uiPaper);
    UText(r,"Theme",c.Title+" · "+c.Form,12,200,195,36,14,uiDim);
    UText(r,"Keywords",c.KeywordText,12,239,195,18,11,uiAccent);
   }
   if(results.Length==0){
    UText(gridRoot,"EmptyTitle","没有符合条件的角色",30,100,620,45,25,uiPaper,TextAnchor.MiddleCenter);
    UText(gridRoot,"EmptyHelp","尝试清空筛选，或切换到全部图鉴。",30,151,620,40,18,uiDim,TextAnchor.MiddleCenter);
    UButton(gridRoot,"EmptyReset","清空筛选",235,220,220,44,ResetGalleryFilters,true);
   }
   Canvas.ForceUpdateCanvases();galleryScroll.StopMovement();galleryScroll.verticalNormalizedPosition=reset?1:old;
   DrawGalleryDetail();
  }
 }
}
