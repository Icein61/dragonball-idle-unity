using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
// Read-only PNG inspection and sprite import rectangles. Never modifies pixel data.
class InspectAtlas {
 static void Main(string[] args){bool failed=false;foreach(string path in Directory.GetFiles(args[0],"*-atlas.png")){try{Inspect(path,args.Length>1&&args[1]=="--check");}catch(Exception e){Console.WriteLine("ART CHECK FAILED: "+e.Message);failed=true;}}if(failed)Environment.Exit(1);}
 static void Inspect(string path,bool check){
  using(var bitmap=new Bitmap(path)){
   int w=bitmap.Width,h=bitmap.Height;if(w!=1536||h!=1024)throw new Exception("Unexpected atlas size: "+path);
   var locked=bitmap.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   byte[] bytes=new byte[locked.Stride*h];Marshal.Copy(locked.Scan0,bytes,0,bytes.Length);int stride=locked.Stride;bitmap.UnlockBits(locked);
   bool[] seen=new bool[w*h];int[] owners=new int[w*h];int[] queue=new int[w*h];Rectangle[] boxes=new Rectangle[13];int[] mass=new int[13];int transparent=0;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(bytes[y*stride+x*4+3]==0)transparent++;
   if(transparent<w*h/5)throw new Exception("Atlas has no useful transparent background: "+path);
   for(int start=0;start<w*h;start++){
    if(seen[start]||bytes[(start/w)*stride+(start%w)*4+3]<32)continue;
    int head=0,tail=1,minX=start%w,maxX=minX,minY=start/w,maxY=minY;queue[0]=start;seen[start]=true;
    while(head<tail){int p=queue[head++],px=p%w,py=p/w;minX=Math.Min(minX,px);maxX=Math.Max(maxX,px);minY=Math.Min(minY,py);maxY=Math.Max(maxY,py);
     for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
      int nx=px+dx,ny=py+dy;if(nx<0||nx>=w||ny<0||ny>=h)continue;int n=ny*w+nx;
      if(!seen[n]&&bytes[ny*stride+nx*4+3]>=32){seen[n]=true;queue[tail++]=n;}
     }
    }
    if(tail<30)continue;
    double cx=(minX+maxX)*.5,cy=(minY+maxY)*.5;
    int id=cx<475&&maxY-minY>500?0:1+Math.Min(3,Math.Max(0,(int)((cx-512)/256)))+4*Math.Min(2,(int)(cy/(1024.0/3)));
    var rect=new Rectangle(minX,minY,maxX-minX+1,maxY-minY+1);
    boxes[id]=mass[id]==0?rect:Rectangle.Union(boxes[id],rect);mass[id]+=tail;
    for(int q=0;q<tail;q++)owners[queue[q]]=id+1;
   }
   var rows=new List<string>();
   for(int i=0;i<13;i++){
    if(mass[i]<500)throw new Exception("Missing pose "+i+": "+path);
    var b=boxes[i];if(i>0&&(b.Width>350||b.Height>380))throw new Exception("Merged or misaligned pose "+i+": "+path);
    int contamination=0;
    for(int padding=2;padding>=0;padding--){
     b=boxes[i];b.Inflate(padding,padding);b=Rectangle.Intersect(b,new Rectangle(0,0,w,h));contamination=0;
     for(int y=b.Top;y<b.Bottom;y++)for(int x=b.Left;x<b.Right;x++)if(owners[y*w+x]>0&&owners[y*w+x]!=i+1)contamination++;
     if(contamination==0)break;
    }
    if(contamination>0)throw new Exception("Neighbor in crop "+Path.GetFileName(path)+" region "+i+" pixels "+contamination);
    rows.Add("{\"x\":"+b.X+",\"y\":"+b.Y+",\"w\":"+b.Width+",\"h\":"+b.Height+"}");
   }
   string json="{\"width\":"+w+",\"height\":"+h+",\"frames\":["+string.Join(",",rows.ToArray())+"]}";
   string output=path.Replace("-atlas.png","-frames.json");
   if(check){if(!File.Exists(output)||File.ReadAllText(output)!=json)throw new Exception("Outdated import rectangles: "+path);}
   else File.WriteAllText(output,json);
   Console.WriteLine(Path.GetFileName(path)+" : 13 regions; transparent="+transparent+"/"+(w*h));
  }
 }
}
