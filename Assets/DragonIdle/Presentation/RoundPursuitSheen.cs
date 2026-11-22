using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  static readonly int SheenBandY=Shader.PropertyToID("_BandY");
  static readonly int SheenBandHalf=Shader.PropertyToID("_BandHalfHeight");
  Material pursuitSheenMaterial;
  RawImage pursuitSheen;

  void PaintRoundSheen(RawImage model,float progress,bool opening){
   if(model==null||model.texture==null||progress<0){
    ClearPursuitSheen();
    return;
   }
   if(pursuitSheen==null||pursuitSheen.transform.parent!=model.transform){
    if(pursuitSheen!=null)Destroy(pursuitSheen.gameObject);
    if(pursuitSheenMaterial==null){
     var shader=Resources.Load<Shader>("Art/Effects/PursuitSheen");
     if(shader==null)throw new InvalidOperationException("Missing pursuit sheen shader");
     pursuitSheenMaterial=new Material(shader);
    }
    pursuitSheen=RoundPoseImage(model.transform,"PursuitSheen");
    pursuitSheen.material=pursuitSheenMaterial;
    var rect=pursuitSheen.rectTransform;
    rect.anchorMin=Vector2.zero;
    rect.anchorMax=Vector2.one;
    rect.offsetMin=rect.offsetMax=Vector2.zero;
   }

   var uv=model.uvRect;
   pursuitSheen.texture=model.texture;
   pursuitSheen.uvRect=uv;
   float phase=Mathf.Clamp01(progress);
   pursuitSheenMaterial.SetFloat(SheenBandY,uv.y+uv.height*Mathf.Lerp(-.07f,1.07f,phase));
   pursuitSheenMaterial.SetFloat(SheenBandHalf,uv.height*.16f);
   float alpha=Mathf.Sin(Mathf.PI*phase)*(opening?.62f:.43f);
   pursuitSheen.color=opening?new Color(1,.75f,.29f,alpha):new Color(1,1,1,alpha);
   if(!pursuitSheen.gameObject.activeSelf)pursuitSheen.gameObject.SetActive(true);
  }

  void ClearPursuitSheen(){
   if(pursuitSheen!=null&&pursuitSheen.gameObject.activeSelf)pursuitSheen.gameObject.SetActive(false);
  }

  void OnDestroy(){
   if(pursuitSheenMaterial!=null)Destroy(pursuitSheenMaterial);
  }
 }
}
