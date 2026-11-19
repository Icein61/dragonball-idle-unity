using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
// Read-only alpha/size/margin validation; generated PNGs are never altered here.
class InspectPortraits {
 static void Main(string[] args){try{
  var files=Directory.GetFiles(args[0],"*-portrait.png");if(files.Length!=9&&args.Length<2)throw new Exception("Expected 9 independent portraits");
  foreach(var path in files)using(var png=new Bitmap(path)){
   int w=png.Width,h=png.Height;
   string name=Path.GetFileName(path);
   bool expectedSize;
   switch(name){
    case "gohan-test-portrait.png":expectedSize=w==1080&&h==1456;break;
    case "chiaotzu-saiyan-v020-portrait.png":
    case "yamcha-saiyan-v020-portrait.png":
    case "krillin-test-portrait.png":
    case "piccolo-test-portrait.png":
    case "tien-test-portrait.png":expectedSize=w==1086&&h==1448;break;
    default:expectedSize=w==1024&&h==1536;break;
   }
   if(!expectedSize)throw new Exception("Unexpected portrait resolution: "+path);
   var locked=png.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   byte[] pixels=new byte[locked.Stride*h];Marshal.Copy(locked.Scan0,pixels,0,pixels.Length);int stride=locked.Stride;png.UnlockBits(locked);
   int clear=0,solid=0,edge=0;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    int alpha=pixels[y*stride+x*4+3];if(alpha<4)clear++;if(alpha>200)solid++;
    // The approved Kaioken artwork deliberately sends the beam off the lower-right edge.
    bool beamExit=name=="goku-saiyan-kaioken-v020-portrait.png"&&
     ((x>=w-3&&y>=h*2/5)||(y>=h-3&&x>=w*4/5));
    if(alpha>64&&!beamExit&&(x<3||x>=w-3||y<3||y>=h-3))edge++;
   }
   if(clear<w*h/5||solid<w*h/20||edge>8)throw new Exception("Portrait transparency/content/margin failed: "+Path.GetFileName(path)+" clear="+clear+" solid="+solid+" edge="+edge);
   Console.WriteLine("PORTRAIT_PASS "+Path.GetFileName(path)+" "+w+"x"+h+" / alpha / margins");
  }
 }catch(Exception e){Console.WriteLine(e.Message);Environment.Exit(1);}}
}
