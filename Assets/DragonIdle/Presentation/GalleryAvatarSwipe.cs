using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;

namespace DragonIdle {
 internal sealed class GalleryAvatarSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
  internal const float CenterSize=80, Step=77;
  const float NearSize=56, FarSize=48, FarStep=61, EdgeTravel=32, ReboundSpeed=630;
  readonly Dictionary<int,RectTransform> frames=new Dictionary<int,RectTransform>();
  internal Color SelectedFrameColor,RestingFrameColor;
  Func<int,RectTransform> createAvatar;
  Action<int> selectCard;
  int count, selected;
  float position, startPosition;
  Vector2 start;
  bool dragging, horizontal, reducedMotion;

  internal void Initialize(int index,int total,bool reduced,Func<int,RectTransform> create,Action<int> select){
   count=total;selected=index;position=index;reducedMotion=reduced;
   createAvatar=create;selectCard=select;
   Layout();
  }

  public void OnBeginDrag(PointerEventData eventData){
   if(count==0||eventData.button!=PointerEventData.InputButton.Left||
      !RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,eventData.pressPosition,eventData.pressEventCamera,out start))return;
   dragging=true;horizontal=false;startPosition=position;
  }

  public void OnDrag(PointerEventData eventData){
   if(!dragging||!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,eventData.position,eventData.pressEventCamera,out var end))return;
   var delta=end-start;
   if(!horizontal){
    if(delta.sqrMagnitude<1)return;
    if(Mathf.Abs(delta.x)<=Mathf.Abs(delta.y)){dragging=false;return;}
    horizontal=true;
   }
   eventData.eligibleForClick=false;
   float requested=startPosition-delta.x/Step;
   float bounded=Mathf.Clamp(requested,0,count-1);
   float stretch=(requested-bounded)*Step;
   position=bounded+stretch/(Step*(1+Mathf.Abs(stretch)/EdgeTravel));
   int nearest=Mathf.FloorToInt(bounded+.5f);
   bool changed=nearest!=selected;
   selected=nearest;
   Layout();
   if(changed)selectCard(selected);
  }

  public void OnEndDrag(PointerEventData eventData){
   dragging=false;
   if(reducedMotion)AdvanceRebound(1);
  }

  void Update()=>AdvanceRebound(Time.unscaledDeltaTime);

  internal void AdvanceRebound(float seconds){
   if(dragging||position==selected)return;
   position=Mathf.MoveTowards(position,selected,ReboundSpeed*seconds/Step);
   Layout();
  }

  void Layout(){
   // At rest render two neighbors per side. While moving, let the next card enter through the mask.
   int first=Mathf.Max(0,Mathf.CeilToInt(position-2.8f));
   int last=Mathf.Min(count-1,Mathf.FloorToInt(position+2.8f));
   foreach(int index in frames.Keys.Where(key=>key<first||key>last).ToArray()){
    var frame=frames[index];
    frame.gameObject.SetActive(false);
    frame.SetParent(null,false);
    Destroy(frame.gameObject);
    frames.Remove(index);
   }
   var width=((RectTransform)transform).rect.width;
   for(int i=first;i<=last;i++){
    if(!frames.TryGetValue(i,out var frame))frames[i]=frame=createAvatar(i);
    float difference=i-position,distance=Mathf.Abs(difference);
    float size=distance<=1?Mathf.Lerp(CenterSize,NearSize,distance):Mathf.Lerp(NearSize,FarSize,distance-1);
    float shift=distance<=1?Step*distance:Step+FarStep*(distance-1);
    frame.localScale=Vector3.one*(size/CenterSize);
    frame.anchoredPosition=new Vector2(width*.5f+Mathf.Sign(difference)*shift-size*.5f,-(CenterSize-size)*.5f);
    int relative=i-selected;
    if(relative==0){
     frame.name="CurrentAvatar";
     frame.SetAsLastSibling();
    }else{
     frame.name=(relative<0?"PreviousCard":"NextCard")+(Mathf.Abs(relative)==1?"":Mathf.Abs(relative).ToString());
    }
    var fill=frame.GetComponent<UI.Image>();
    fill.color=relative==0?SelectedFrameColor:RestingFrameColor;
   }
  }
 }
}
