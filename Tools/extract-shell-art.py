from pathlib import Path
import plistlib,re,json,hashlib,shutil,argparse
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('--source',default='.validation/apk-review/wangzhe-jizhan');p.add_argument('--output',default='Assets/DragonIdle/Resources/Interface/V070');a=p.parse_args();root=Path(a.source);out=Path(a.output);out.mkdir(parents=True,exist_ok=True);items=[]
def save(im,key,source,frame=None,rect=None,rotated=False,canvas=None):
 dest=out/(key+'.png');im.save(dest);items.append(dict(key=key,target=dest.as_posix(),sourcePath=source,frame=frame,sourceRectangle=rect,rotated=rotated,sourceCanvas=canvas,width=im.width,height=im.height,sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
for key,path in [('home-scene','asset/ui/bg/mainBg/bg_ZhuCheng.png'),('loading-scene','asset/ui/bg/LoadingBg/BG_DengLu.png'),('currency-crystal','asset/ui/icon/ico_diamond_3.png'),('currency-material','asset/ui/icon/ico_large_exp.png')]:
 im=Image.open(root/'viewable'/path).convert('RGBA');save(im,key,path)
frames=[('common0','Btn_Common_Yellow_Big.png','button-primary'),('common0','Btn_Common_Blue_Big.png','button-secondary'),('common0','icon_Common_gem.png','currency-gem'),('login0','Pic_Loading_JinDu.png','loading-fill'),('login0','Pic_Loading_JinDudi.png','loading-track'),('login0','Pic_Loading_WuKong.png','loading-goku'),('main0','pic_LiaoTian_SheZhi.png','settings')]
for atlas,name,key in frames:
 source='asset/ui/'+atlas+'.png';im=Image.open(root/'viewable'/source).convert('RGBA');d=plistlib.loads((root/'named/readable/asset/ui'/(atlas+'.plist')).read_bytes());f=d['frames'][name];x,y,w,h=map(int,re.findall(r'-?\d+',f['textureRect']));rot=f['textureRotated'];crop=im.crop((x,y,x+(h if rot else w),y+(w if rot else h)))
 if rot:crop=crop.transpose(Image.Transpose.ROTATE_90)
 assert crop.size==(w,h)
 sw,sh=map(int,re.findall(r'-?\d+',f['spriteSourceSize']));ox,oy=map(int,re.findall(r'-?\d+',f['spriteOffset']));full=Image.new('RGBA',(sw,sh));full.paste(crop,((sw-w)//2+ox,(sh-h)//2-oy));save(full,key,source,name,[x,y,w,h],rot,[sw,sh])
manifest=dict(version='0.7.0',origin='User-provided local APK; statically decoded ASTC; no original executable code is executed or imported.',apkSha256='0f14ab4c2f71e787bec9d03f0666f5f183915dba7946db513333edc15b7a2a28',operations='Exact atlas extraction, reverse stored rotation and restore transparent trim offsets; no repainting.',assets=items)
Path('Art/Production/V070/ui-assets.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8');print('UI_ASSETS_EXTRACTED',len(items),[(x['key'],x['width'],x['height']) for x in items])

