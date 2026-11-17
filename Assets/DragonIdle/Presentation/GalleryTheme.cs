using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 // Shared, code-native nine-slice surfaces; character pixels are never modified.
 public sealed class GalleryFocus:MonoBehaviour,ISelectHandler,IDeselectHandler {
  public UI.Outline Outline;
  public void OnSelect(BaseEventData e){Outline.enabled=true;}
  public void OnDeselect(BaseEventData e){Outline.enabled=false;}
 }
 public partial class DemoGame {
  Sprite roundedControl,roundedPanel;
  Sprite RoundedSurface(bool panel){
   if(panel&&roundedPanel!=null)return roundedPanel;if(!panel&&roundedControl!=null)return roundedControl;
   int radius=panel?24:14,size=radius*4;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.name=panel?"RoundedPanel":"RoundedControl";texture.wrapMode=TextureWrapMode.Clamp;
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float dx=Mathf.Max(radius-(x+.5f),x+.5f-(size-radius),0),dy=Mathf.Max(radius-(y+.5f),y+.5f-(size-radius),0);
    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy))));
   }
   texture.Apply(false,true);var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
   if(panel)roundedPanel=sprite;else roundedControl=sprite;return sprite;
  }
  RectTransform galleryListRoot,filterChipRoot;bool galleryDetailOpen,statsExpanded;int[] renderedCards;
  static double ThemeLuminance(Color c){return .2126*ThemeChannel(c.r)+.7152*ThemeChannel(c.g)+.0722*ThemeChannel(c.b);}
  static double ThemeChannel(double value){return value<=.04045?value/12.92:System.Math.Pow((value+.055)/1.055,2.4);}
  static double ThemeContrast(Color a,Color b){double x=ThemeLuminance(a),y=ThemeLuminance(b);return (System.Math.Max(x,y)+.05)/(System.Math.Min(x,y)+.05);}
  void OpenGalleryCard(int index){
   selectedCard=index;galleryDetailOpen=true;galleryListRoot.gameObject.SetActive(false);detailRoot.gameObject.SetActive(true);detailFade=.35f;DrawGalleryDetail();
   if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(ActiveBackButton(detailRoot,"BackToCards"));
  }
  void ReturnToCards(){
   galleryDetailOpen=false;detailRoot.gameObject.SetActive(false);galleryListRoot.gameObject.SetActive(true);
   if(EventSystem.current!=null&&cardButtons.ContainsKey(selectedCard))EventSystem.current.SetSelectedGameObject(cardButtons[selectedCard].gameObject);
  }
 }
}

