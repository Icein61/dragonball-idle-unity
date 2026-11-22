using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  const string FormationRoute="编队";
  RectTransform formationRoot,formationDragLayer,formationListViewport;
  FormationGridGraphic formationGrid;
  UI.RawImage formationGhost;
  int[] formationDraft;
  int formationSelection,formationDragCard=-1,formationDragSource;

  void DrawFormation(){
   CancelFormationDrag();
   if(formationRoot==null)formationRoot=UNode(galleryRoot,"FormationEditor",222,112,1336,718);
   if(formationDraft==null){formationDraft=profile.RoundFormation();formationSelection=0;}
   var previousScroll=formationRoot.GetComponentInChildren<UI.ScrollRect>();
   float scrollPosition=previousScroll!=null?previousScroll.verticalNormalizedPosition:1;
   ClearUI(formationRoot);
   var board=UPanel(formationRoot,"FormationBattlefield",0,25,795,640,InkMist);
   board.gameObject.AddComponent<UI.Mask>().showMaskGraphic=false;
   var bg=RoundPoseImage(board,"FormationTerrain");
   bg.rectTransform.sizeDelta=board.sizeDelta;
   bg.texture=Resources.Load<Texture2D>(DemoScenes().Scene(DemoScenes().demo.scene).background);
   float aspect=bg.texture.width/(float)bg.texture.height,ratio=795f/640;
   float uvWidth=Mathf.Min(1,ratio/aspect),uvHeight=Mathf.Min(1,aspect/ratio);
   bg.uvRect=new Rect((1-uvWidth)*.5f,(1-uvHeight)*.5f,uvWidth,uvHeight);
   UPanel(board,"FormationTerrainShade",0,0,795,640,new Color(.12f,.23f,.19f,.11f));

   var grid=UNode(board,"FormationGrid",55,389,692,220);
   formationGrid=grid.gameObject.AddComponent<FormationGridGraphic>();
   grid.gameObject.AddComponent<FormationGridInput>().Bind(this);
   formationGrid.SetHighlight(formationSelection,false);
   for(int slot=1;slot<=RoundBoard.Cells;slot++){
    var center=FormationGridGraphic.Center(slot);
    UText(grid,"FormationSlotNumber"+slot,slot.ToString(),center.x-31,center.y+10,62,22,15,InkIvory,TextAnchor.MiddleCenter);
    int card=formationDraft[slot-1];
    if(card<0)continue;
    var actor=RoundPoseImage(grid,"FormationActor"+slot);
    DrawFormationActor(actor,card,center+new Vector2(0,11),.166f+.010f*RoundBoard.Row(slot));
   }

   var list=UPanel(formationRoot,"FormationRoster",819,25,513,640,InkPaper);
   UText(list,"FormationRosterTitle","角色列表",17,12,340,37,22,InkText);
   var scroll=UScroll(list,"FormationRosterScroll",17,62,476,507,out var content);
   formationListViewport=scroll.viewport;
   var owned=Data.RoundRoster.Select(Data.Index).Where(i=>profile.Owned[i]).OrderBy(i=>Data.Cards[i].CatalogNumber).ToArray();
   for(int n=0;n<owned.Length;n++){
    int id=owned[n];
    var card=Data.Cards[id];
    float x=3+n%2*228,y=3+n/2*160;
    var button=UButton(content,"FormationCard"+card.Id,"",x,y,218,148,()=>ShowFormationCard(id));
    button.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
    var tile=button.transform;
    DrawRarityBackdrop(tile,"PortraitMat",card.Rarity,6,32,206,110);
    UArt(tile,id,72,33,133,107,CharacterCardPortraitRegion(id));
    var name=UText(tile,"FormationCardName",card.Name,10,5,119,23,16,InkText,TextAnchor.MiddleLeft);
    float descriptor=15+Mathf.Min(119,Mathf.Ceil(name.preferredWidth));
    UText(tile,"FormationCardDescription",card.Title,descriptor,5,211-descriptor,23,13,InkMuted,TextAnchor.MiddleLeft);
    var badges=UPanel(tile,"FormationCardBadges",9,37,66,28,new Color(InkIvory.r,InkIvory.g,InkIvory.b,.88f));
    DrawRarityBadge(badges,card.Rarity,2,1,33,26,leftAligned:true);
    DrawCharacterRole(badges,card,36,1,26);
    tile.gameObject.AddComponent<FormationDragHandle>().Bind(this,id,scroll);
   }
   content.sizeDelta=new Vector2(462,Mathf.Max(507,((owned.Length+1)/2)*160));
   bool dirty=!formationDraft.SequenceEqual(profile.RoundFormation());
   UText(list,"FormationHint",dirty?"阵容有未保存的更改":"拖回列表可下阵",18,581,331,44,15,InkMuted,TextAnchor.MiddleLeft);
   UButton(list,"FormationSave","保存编队",351,581,143,44,SaveFormationDraft,primary:true,enabled:dirty&&saveError=="");
   formationDragLayer=UNode(formationRoot,"FormationDragLayer",0,0,1336,718);
   Canvas.ForceUpdateCanvases();
   scroll.verticalNormalizedPosition=scrollPosition;
  }

  void ShowFormationCard(int index){
   var card=Data.Cards[index];
   var modal=NewGalleryDialog(card.Name,card.Title+"  ·  "+card.Rarity+"  ·  "+card.TypeText,
    focusClose:false,headerRule:false,width:1170,height:690);
   DrawRarityBackdrop(modal,"PortraitMat",card.Rarity,22,110,307,542);
   UArt(modal,index,22,110,307,542,CharacterCardPortraitRegion(index));
   UScroll(modal,"FormationCardScroll",351,105,791,552,out var content);
   float y=DrawCardStats(content,index,0);
   content.sizeDelta=new Vector2(content.sizeDelta.x,DrawCardSkills(content,index,y+10)+10);
  }

  void DrawFormationActor(UI.RawImage image,int card,Vector2 feet,float scale){
   string id=Data.Cards[card].Id;
   var art=BattleMotionLibrary(id);
   if(art!=null)RoundMotionArt.Draw(image,art.At(art["idle"],0),feet,scale*BattleModelSize(id),false);
   else DrawRoundAtlasActor(image,new RoundFighter(id,0,1,1,1,1,0),feet,1,FighterScale(card)*scale*4.2f);
  }

  internal int SelectFormationCell(Vector2 screen,Camera camera){
   int slot=FormationSlotAt(screen,camera);
   if(slot==0||formationDraft==null||dialogRoot!=null)return 0;
   formationSelection=slot;
   formationGrid.SetHighlight(slot,false);
   return formationDraft[slot-1]<0?0:slot;
  }

  internal void BeginFormationCellDrag(int slot){
   if(formationDraft!=null&&slot>0&&formationDraft[slot-1]>=0)
    BeginFormationDrag(formationDraft[slot-1],slot);
  }

  internal void BeginFormationDrag(int card,int source=0){
   CancelFormationDrag();
   if(saveError==""&&page==FormationRoute&&dialogRoot==null){formationDragCard=card;formationDragSource=source;}
  }

  internal void UpdateFormationDrag(Vector2 screen,Camera camera,bool fromList){
   if(formationDragCard<0||page!=FormationRoute)return;
   bool show=!fromList||!RectTransformUtility.RectangleContainsScreenPoint(formationListViewport,screen,camera);
   int slot=show?FormationSlotAt(screen,camera):0;
   formationGrid.SetHighlight(slot==0?formationSelection:slot,slot!=0);
   if(!show){
    if(formationGhost!=null)formationGhost.gameObject.SetActive(false);
    return;
   }
   if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(formationDragLayer,screen,camera,out var pt))return;
   if(formationGhost==null)formationGhost=RoundPoseImage(formationDragLayer,"FormationDragSprite");
   formationGhost.gameObject.SetActive(true);
   DrawFormationActor(formationGhost,formationDragCard,new Vector2(pt.x,-pt.y+4),.248f);
  }

  internal void EndFormationDrag(Vector2 screen,Camera camera){
   int card=formationDragCard,source=formationDragSource,slot=FormationSlotAt(screen,camera);
   CancelFormationDrag();
   if(card<0||page!=FormationRoute||formationDraft==null)return;
   if(slot>0){
    if(!profile.TryPlaceRoundMember(formationDraft,slot,card)){
     GalleryNotice("最多上阵五人，可拖到已有成员的阵位替换",false);
     return;
    }
    formationSelection=slot;
   }else if(source>0&&RectTransformUtility.RectangleContainsScreenPoint(formationListViewport,screen,camera)){
    if(formationDraft.Count(i=>i>=0)==1){GalleryNotice("至少保留一名上阵角色",false);return;}
    formationDraft[source-1]=-1;formationSelection=0;
   }else return;
   DrawFormation();
  }

  void SaveFormationDraft(){
   if(formationDraft==null||saveError!="")return;
   if(Change(()=>{
    if(!profile.TrySaveRoundFormation(formationDraft))throw new InvalidOperationException("阵容无效");
   })){
    galleryDirty=false;
    GalleryNotice("阵容已保存");
    DrawFormation();
   }else GalleryNotice(notice,false);
  }

  void LeaveFormation(){
   CancelFormationDrag();
   formationDraft=null;
  }

  internal void CancelFormationDrag(){
   formationDragCard=-1;formationDragSource=0;
   if(formationGhost!=null){formationGhost.gameObject.SetActive(false);Destroy(formationGhost.gameObject);formationGhost=null;}
   if(formationGrid!=null)formationGrid.SetHighlight(formationSelection,false);
  }

  int FormationSlotAt(Vector2 screen,Camera camera)=>formationGrid==null?0:formationGrid.SlotAt(screen,camera);
 }

 internal sealed class FormationGridInput : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
  DemoGame game;
  int pressed,source;
  internal void Bind(DemoGame owner){game=owner;}
  public void OnPointerDown(PointerEventData e){
   if(e.button==PointerEventData.InputButton.Left)pressed=game.SelectFormationCell(e.position,e.pressEventCamera);
  }
  public void OnPointerUp(PointerEventData e){pressed=0;}
  public void OnBeginDrag(PointerEventData e){
   if(e.button!=PointerEventData.InputButton.Left||pressed==0)return;
   source=pressed;game.BeginFormationCellDrag(source);
  }
  public void OnDrag(PointerEventData e){if(source>0)game.UpdateFormationDrag(e.position,e.pressEventCamera,false);}
  public void OnEndDrag(PointerEventData e){
   if(source==0)return;
   source=0;game.EndFormationDrag(e.position,e.pressEventCamera);
  }
  void OnDisable(){if(source>0)game.CancelFormationDrag();pressed=source=0;}
 }

 internal sealed class FormationDragHandle : MonoBehaviour,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
  DemoGame game;
  UI.ScrollRect scroll;
  int card;
  bool scrolling,dragging;

  internal void Bind(DemoGame owner,int index,UI.ScrollRect list){game=owner;card=index;scroll=list;}
  public void OnInitializePotentialDrag(PointerEventData e)=>scroll.OnInitializePotentialDrag(e);
  public void OnBeginDrag(PointerEventData e){
   if(e.button!=PointerEventData.InputButton.Left)return;
   Vector2 delta=e.position-e.pressPosition;
   scrolling=Mathf.Abs(delta.y)>Mathf.Abs(delta.x);
   e.eligibleForClick=false;
   if(scrolling)scroll.OnBeginDrag(e);
   else {dragging=true;game.BeginFormationDrag(card);}
  }
  public void OnDrag(PointerEventData e){
   if(scrolling)scroll.OnDrag(e);
   else if(dragging)game.UpdateFormationDrag(e.position,e.pressEventCamera,true);
  }
  public void OnEndDrag(PointerEventData e){
   if(scrolling){scrolling=false;scroll.OnEndDrag(e);return;}
   if(!dragging)return;
   dragging=false;game.EndFormationDrag(e.position,e.pressEventCamera);
  }
  void OnDisable(){if(dragging)game.CancelFormationDrag();scrolling=dragging=false;}
 }

 [RequireComponent(typeof(CanvasRenderer))]
 internal sealed class FormationGridGraphic : UI.MaskableGraphic {
  static readonly Vector2 Origin=new Vector2(14,11),Across=new Vector2(183,0),Depth=new Vector2(31,64);
  int highlighted;
  bool dragging;

  protected override void Awake(){base.Awake();useLegacyMeshGeneration=false;raycastTarget=true;}

  public override bool Raycast(Vector2 position,Camera camera)=>base.Raycast(position,camera)&&SlotAt(position,camera)!=0;

  internal int SlotAt(Vector2 point,Camera camera){
   if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,point,camera,out var local))return 0;
   var rect=rectTransform.rect;
   return SlotAt(new Vector2(local.x-rect.xMin,rect.yMax-local.y));
  }

  internal static Vector2 Center(int slot)=>Origin+Across*(RoundBoard.Column(slot,0)+.5f)+Depth*(RoundBoard.Row(slot)+.5f);
  internal static int SlotAt(Vector2 point){
   Vector2 relative=point-Origin;
   float row=relative.y/Depth.y;
   float column=(relative.x-row*Depth.x)/Across.x;
   if(row<0||row>=3||column<0||column>=3)return 0;
   return (2-(int)column)*3+(int)row+1;
  }

  internal void SetHighlight(int slot,bool fromDrag){
   if(highlighted==slot&&dragging==fromDrag)return;
   highlighted=slot;dragging=fromDrag;SetVerticesDirty();
  }

  protected override void OnPopulateMesh(UI.VertexHelper mesh){
   mesh.Clear();
   Quad(mesh,Origin,Origin+Across*3,Origin+(Across+Depth)*3,Origin+Depth*3,new Color(.13f,.24f,.20f,.46f));
   if(highlighted!=0){
    var at=Origin+Across*RoundBoard.Column(highlighted,0)+Depth*RoundBoard.Row(highlighted);
    Quad(mesh,at,at+Across,at+Across+Depth,at+Depth,dragging?new Color(.96f,.77f,.42f,.52f):new Color(.93f,.78f,.55f,.24f));
   }
   for(int i=0;i<4;i++){
    Stroke(mesh,Origin+Across*i,Origin+Across*i+Depth*3);
    Stroke(mesh,Origin+Depth*i,Origin+Depth*i+Across*3);
   }
  }
  static void Stroke(UI.VertexHelper mesh,Vector2 start,Vector2 end){
   Vector2 delta=(end-start).normalized;
   var normal=new Vector2(-delta.y,delta.x);
   Quad(mesh,start-normal*2,end-normal*2,end+normal*2,start+normal*2,new Color(.10f,.17f,.15f,.78f));
   Quad(mesh,start-normal,end-normal,end+normal,start+normal,new Color(.96f,.87f,.68f,.93f));
  }
  static void Quad(UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){
   int start=mesh.currentVertCount;
   mesh.AddVert(new Vector2(a.x,-a.y),tint,Vector2.zero);mesh.AddVert(new Vector2(b.x,-b.y),tint,Vector2.zero);
   mesh.AddVert(new Vector2(c.x,-c.y),tint,Vector2.zero);mesh.AddVert(new Vector2(d.x,-d.y),tint,Vector2.zero);
   mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
  }
 }
}
