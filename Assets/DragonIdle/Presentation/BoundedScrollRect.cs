using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DragonIdle {
 internal sealed class BoundedScrollRect : ScrollRect {
  const float EdgeTravel=48;

  public override void OnScroll(PointerEventData eventData){
   var parentScroll=transform.parent.GetComponentInParent<ScrollRect>();
   bool overflow=content.rect.height>viewport.rect.height+1;
   bool atEdge=eventData.scrollDelta.y>0?verticalNormalizedPosition>=.999f:verticalNormalizedPosition<=.001f;
   if(parentScroll!=null&&(!overflow||atEdge)){
    parentScroll.OnScroll(eventData);
    return;
   }
   base.OnScroll(eventData);
  }

  protected override void SetContentAnchoredPosition(Vector2 position) {
   if(movementType==MovementType.Elastic&&content!=null&&viewport!=null){
    float bottom=Mathf.Max(0,content.rect.height-viewport.rect.height);
    position.y=Mathf.Clamp(position.y,-EdgeTravel,bottom+EdgeTravel);
   }
   base.SetContentAnchoredPosition(position);
  }
 }
}
