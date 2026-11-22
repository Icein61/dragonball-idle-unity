using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 // Shared, code-native nine-slice surfaces; character pixels are never modified.
 public sealed class GalleryFocus:MonoBehaviour,ISelectHandler,IDeselectHandler {
  public UI.Outline Outline;
  public System.Action Focused;
  public UI.Image CardSurface;
  public Sprite CardFocusSprite;
  public Color CardNormalTint;
  void SetFocus(bool active){
   if(CardSurface==null){if(Outline!=null)Outline.enabled=active;return;}
   CardSurface.overrideSprite=active?CardFocusSprite:null;
   CardSurface.color=active?Color.white:CardNormalTint;
  }
  public void OnSelect(BaseEventData e){SetFocus(true);Focused?.Invoke();}
  public void OnDeselect(BaseEventData e){SetFocus(false);}
  void OnDisable(){if(CardSurface!=null)SetFocus(false);}
 }
 public partial class DemoGame {
  Sprite roundedControl,roundedPanel,roundedCardFocus;
  Sprite RoundedSurface(bool panel,bool cardFocus=false){
   var cached=cardFocus?roundedCardFocus:panel?roundedPanel:roundedControl;if(cached!=null)return cached;
   int radius=panel?24:14,size=radius*4;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.name=cardFocus?"RoundedCardFocus":panel?"RoundedPanel":"RoundedControl";texture.wrapMode=TextureWrapMode.Clamp;
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float dx=Mathf.Max(radius-(x+.5f),x+.5f-(size-radius),0),dy=Mathf.Max(radius-(y+.5f),y+.5f-(size-radius),0);
    float inside=radius-Mathf.Sqrt(dx*dx+dy*dy),alpha=Mathf.Clamp01(inside+.5f);
    // Paint the border and paper on one silhouette so the ordinary card cannot show through at the edge.
    Color pixel=cardFocus?Color.Lerp(InkDark,InkPaper,Mathf.Clamp01(inside-5.5f)):Color.white;
    pixel.a=alpha;texture.SetPixel(x,y,pixel);
   }
   texture.Apply(false,!cardFocus||!Application.isBatchMode);var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
   if(cardFocus)roundedCardFocus=sprite;else if(panel)roundedPanel=sprite;else roundedControl=sprite;return sprite;
  }
  readonly Dictionary<string,Texture2D> rarityBackdrops=new Dictionary<string,Texture2D>();
  Texture2D RarityBackdrop(string rarity){
   if(!rarityBackdrops.TryGetValue(rarity,out var texture)){
    texture=Resources.Load<Texture2D>("Interface/Gallery/Rarity/V0171/"+rarity);
    rarityBackdrops.Add(rarity,texture);
   }
   return texture;
  }

  void DrawRarityBackdrop(Transform parent,string name,string rarity,float x,float y,float width,float height){
   var mat=UPanel(parent,name,x,y,width,height,InkMist);
   var image=mat.GetComponent<UI.Image>();
   image.color=Color.white;
   mat.gameObject.AddComponent<UI.Mask>().showMaskGraphic=false;
   var art=UNode(mat,"RarityBackdrop",0,0,width,height).gameObject.AddComponent<UI.RawImage>();
   art.texture=RarityBackdrop(rarity);
   art.color=art.texture!=null?Color.white:InkMist;
   art.raycastTarget=false;
   float ratio=width/height;
   art.uvRect=ratio>=1?new Rect(0,(1-1/ratio)*.5f,1,1/ratio):new Rect((1-ratio)*.5f,0,ratio,1);
   var orbs=UNode(mat,"DragonBallWatermarks",0,0,width,height).gameObject.AddComponent<GalleryDragonBallWatermarks>();
   orbs.Bind(rarity,art.uvRect);
  }

  RectTransform galleryListRoot,filterChipRoot;bool galleryDetailOpen;int[] renderedCards;
  static double ThemeLuminance(Color c){return .2126*ThemeChannel(c.r)+.7152*ThemeChannel(c.g)+.0722*ThemeChannel(c.b);}
  static double ThemeChannel(double value){return value<=.04045?value/12.92:System.Math.Pow((value+.055)/1.055,2.4);}
  static double ThemeContrast(Color a,Color b){double x=ThemeLuminance(a),y=ThemeLuminance(b);return (System.Math.Max(x,y)+.05)/(System.Math.Min(x,y)+.05);}
  void OpenGalleryCard(int index){
   selectedCard=index;galleryDetailOpen=true;galleryListRoot.gameObject.SetActive(false);detailRoot.gameObject.SetActive(true);detailFade=.35f;DrawGalleryDetail();
   if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
  }
  void ReturnToCards(){
   galleryDetailOpen=false;detailRoot.gameObject.SetActive(false);galleryListRoot.gameObject.SetActive(true);
   if(EventSystem.current!=null&&cardButtons.ContainsKey(selectedCard))EventSystem.current.SetSelectedGameObject(cardButtons[selectedCard].gameObject);
  }
 }
}

