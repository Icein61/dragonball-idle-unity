using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 [RequireComponent(typeof(CanvasRenderer))]
 internal sealed class GalleryDragonBallWatermarks : MaskableGraphic {
  // Positions use the same UV space as the painted four-star ball, so portrait cropping moves them together.
  static readonly Vector3[] SmallOrbs={
   new Vector3(.18f,.76f,.048f),new Vector3(.37f,.89f,.028f),new Vector3(.62f,.82f,.034f),
   new Vector3(.12f,.43f,.035f),new Vector3(.26f,.25f,.056f),new Vector3(.57f,.18f,.041f),
   new Vector3(.86f,.33f,.056f)
  };
  static readonly int[] Stars={2,5,1,3,7,4,6};
  Rect visible;
  Color ink,glint;
  internal int SmallBallCount=>SmallOrbs.Length;

  internal void Bind(string rarity,Rect uv){
   visible=uv;
   switch(rarity){
    case "B": ink=new Color32(4,108,143,74);glint=new Color32(222,250,255,90);break;
    case "A": ink=new Color32(99,36,153,75);glint=new Color32(244,224,255,111);break;
    case "S": ink=new Color32(148,75,9,86);glint=new Color32(255,237,164,107);break;
    default: ink=new Color32(38,89,68,74);glint=new Color32(217,247,219,82);break;
   }
   raycastTarget=false;useLegacyMeshGeneration=false;SetVerticesDirty();
  }

  protected override void OnPopulateMesh(VertexHelper mesh){
   mesh.Clear();
   if(visible.width<=0)return;
   var rect=rectTransform.rect;
   for(int n=0;n<SmallOrbs.Length;n++){
    var orb=SmallOrbs[n];
    var center=new Vector2(rect.xMin+(orb.x-visible.x)*rect.width/visible.width,
      rect.yMin+(orb.y-visible.y)*rect.height/visible.height);
    float radius=orb.z*rect.width/visible.width;
    DrawOrb(mesh,center,radius,Stars[n]);
   }
  }

  static readonly float[] DiffusionRings={.54f,.82f,.945f,1f};

  void DrawOrb(VertexHelper mesh,Vector2 center,float radius,int count){
   const int sides=32;
   int middle=mesh.currentVertCount;
   Color inner=new Color(glint.r,glint.g,glint.b,.065f);
   mesh.AddVert(center,inner,Vector2.zero);
   Vector2 light=new Vector2(-.60f,.80f);
   for(int i=0;i<=sides;i++){
    var axis=Direction(i*2*Mathf.PI/sides);
    float lit=Mathf.Clamp01(.5f+.5f*Vector2.Dot(axis,light));
    float reflected=Mathf.Pow(Mathf.Clamp01(Vector2.Dot(axis,new Vector2(.715f,-.7f))),7)*.30f;
    for(int j=0;j<DiffusionRings.Length;j++){
     Color tint;
     if(j==0)tint=new Color(glint.r,glint.g,glint.b,.07f+.12f*lit);
     else if(j==1)tint=Color.Lerp(new Color(ink.r,ink.g,ink.b,.20f),new Color(glint.r,glint.g,glint.b,.23f),lit);
     else if(j==2)tint=Color.Lerp(new Color(ink.r,ink.g,ink.b,.57f),new Color(glint.r,glint.g,glint.b,.52f),Mathf.Pow(lit,2)+reflected);
     else tint=new Color(ink.r,ink.g,ink.b,0);
     mesh.AddVert(center+axis*radius*DiffusionRings[j],tint,Vector2.zero);
    }
    if(i==0)continue;
    int a=middle+1+(i-1)*DiffusionRings.Length,b=a+DiffusionRings.Length;
    mesh.AddTriangle(middle,a,b);
    for(int j=1;j<DiffusionRings.Length;j++){
     mesh.AddTriangle(a+j-1,a+j,b+j);
     mesh.AddTriangle(a+j-1,b+j,b+j-1);
    }
   }
   for(int n=0;n<count;n++){
    bool centerStar=count==1||(count==7&&n==6);
    int ring=count==7?6:count;
    var p=centerStar?center:center+Direction(Mathf.PI*.5f+n*2*Mathf.PI/ring)*radius*.45f;
    DrawStar(mesh,p,radius*(count<4?.20f:.145f));
   }
   DrawReflection(mesh,center+new Vector2(-.30f,.54f)*radius,radius*.25f,radius*.092f,.79f);
   DrawReflection(mesh,center+new Vector2(-.58f,.33f)*radius,radius*.078f,radius*.057f,.48f);
  }

  void DrawReflection(VertexHelper mesh,Vector2 center,float width,float height,float opacity){
   const int edges=16;
   int first=mesh.currentVertCount;
   Color clear=new Color(glint.r,glint.g,glint.b,0);
   Color core=Color.Lerp(glint,Color.white,.64f);
   core.a=opacity;
   mesh.AddVert(center,core,Vector2.zero);
   for(int i=0;i<=edges;i++){
    var axis=Direction(i*Mathf.PI*2/edges);
    var delta=new Vector2(width*axis.x,height*axis.y);
    var rotated=new Vector2(.83f*delta.x-.55f*delta.y,.55f*delta.x+.83f*delta.y);
    mesh.AddVert(center+rotated,clear,Vector2.zero);
    if(i>0)mesh.AddTriangle(first,first+i,first+i+1);
   }
  }

  void DrawStar(VertexHelper mesh,Vector2 center,float radius){
   int middle=mesh.currentVertCount;
   mesh.AddVert(center,ink,Vector2.zero);
   for(int n=0;n<=10;n++){
    var point=Direction(Mathf.PI*.5f+n*Mathf.PI/5)*radius*(n%2==0?1:.41f);
    mesh.AddVert(center+point,ink,Vector2.zero);
    if(n>0)mesh.AddTriangle(middle,middle+n,middle+n+1);
   }
  }

  static Vector2 Direction(float angle)=>new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
 }
}
