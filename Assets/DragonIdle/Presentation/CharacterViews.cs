using System;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  string characterSearch="";int ownershipFilter,rarityFilter,roleFilter,tagFilter,bagKind,bagOwner,bagLock;
  Vector2 characterScroll,reserveScroll;Texture2D gokuSample;bool artLoaded;
  void DrawPortrait(int index,Rect rect){
   if(!artLoaded){gokuSample=Resources.Load<Texture2D>("Art/goku-saiyan-sample-v1");artLoaded=true;}
   if(index==0&&gokuSample!=null){GUI.DrawTexture(rect,gokuSample,ScaleMode.ScaleToFit,true);return;}
   float size=Math.Min(rect.width,rect.height)*.65f;
   Circle(rect.x+(rect.width-size)/2,rect.y+(rect.height-size)/2,size,CardColor(index));
   Label(rect.x+rect.width/2-23,rect.y+rect.height/2-31,65,65,Data.Cards[index].Name.Substring(0,1),42,bg);
  }
  void DrawCharacters(){
   Heading("角色图鉴","已收藏 "+profile.Owned.Count(x=>x)+" / "+Data.Cards.Length+" · 角色与阶段为可调整原型 · 点击卡片查看完整技能和队长条件");
   GUIStyle input=new GUIStyle(GUI.skin.textField){font=font,fontSize=18};
   characterSearch=GUI.TextField(new Rect(224,211,290,42),characterSearch,input);
   if(characterSearch=="")Label(234,220,275,28,"搜索名字 / 阶段 / 称号",16,muted);
   string[] ownedNames={"全部收藏","已拥有","未拥有"};
   if(Button(530,211,150,42,ownedNames[ownershipFilter])){ownershipFilter=(ownershipFilter+1)%3;characterScroll=Vector2.zero;}
   if(Button(694,211,145,42,rarityFilter==0?"所有稀有度":Data.Rarities[rarityFilter-1])){rarityFilter=(rarityFilter+1)%4;characterScroll=Vector2.zero;}
   if(Button(853,211,170,42,roleFilter==0?"所有定位":RoleText(Data.Roles[roleFilter-1]))){roleFilter=(roleFilter+1)%7;characterScroll=Vector2.zero;}
   var tags=Data.Cards.SelectMany(c=>c.Tags).Distinct().ToArray();
   if(Button(1037,211,330,42,tagFilter==0?"所有身份标签":tags[tagFilter-1])){tagFilter=(tagFilter+1)%(tags.Length+1);characterScroll=Vector2.zero;}
   if(Button(1381,211,180,42,"清空筛选")){characterSearch="";ownershipFilter=rarityFilter=roleFilter=tagFilter=0;characterScroll=Vector2.zero;}
   var filtered=Enumerable.Range(0,Data.Cards.Length).Where(i=>{
    var c=Data.Cards[i];return (ownershipFilter==0||(ownershipFilter==1)==profile.Owned[i])&&(rarityFilter==0||c.Rarity==Data.Rarities[rarityFilter-1])&&(roleFilter==0||c.Role==Data.Roles[roleFilter-1])&&(tagFilter==0||c.HasTag(tags[tagFilter-1]))&&(c.DisplayName+c.Stage+c.Form+string.Join(" ",c.Tags)).IndexOf(characterSearch,StringComparison.OrdinalIgnoreCase)>=0;
   }).ToArray();
   characterScroll=GUI.BeginScrollView(new Rect(224,276,745,518),characterScroll,new Rect(0,0,720,Math.Max(500,((filtered.Length+2)/3)*174)));
   for(int n=0;n<filtered.Length;n++){
    int i=filtered[n];var c=Data.Cards[i];float x=n%3*239,y=n/3*174;
    Box(x,y,226,160,i==selectedCard?new Color(.17f,.20f,.24f):panel);Box(x,y,226,3,CardColor(i));
    Label(x+12,y+12,205,30,c.Rarity+"  "+c.Name,21,profile.Owned[i]?ink:muted);
    Label(x+12,y+46,205,52,c.Title+"\n"+c.Stage+" · "+RoleText(c.Role),15,muted);
    if(Button(x+12,y+111,202,37,profile.Owned[i]?"查看角色":"未拥有 · 查看",selectedCard==i))selectedCard=i;
   }
   if(filtered.Length==0)Label(20,20,680,60,"没有符合筛选的角色。清空筛选可查看全部卡片。",20,muted);
   GUI.EndScrollView();DrawCharacterDetail(selectedCard);
  }
  void DrawCharacterDetail(int i){
   var c=Data.Cards[i];Box(990,276,574,518,panel);
   DrawPortrait(i,new Rect(1004,289,155,204));
   Label(1172,292,368,68,c.Rarity+"  "+c.DisplayName,24,gold);
   Label(1172,363,365,100,c.Stage+" / "+c.Form+"\n"+string.Join(" · ",c.Tags)+"\n"+(profile.Owned[i]?"已拥有 · 专属资源 "+profile.Shards[i]:"获取：时空召唤"),16,muted);
   Label(1008,489,535,37,"基础：生命 "+c.HP+" / 攻击 "+c.Attack+" / 防御 "+c.Defense+" / 速度 "+c.Speed,15);
   Label(1008,526,535,47,"普攻：单体 1.00 倍攻击。"+c.PassiveText,16);
   Label(1008,574,535,60,c.SpecialText,16);
   Label(1008,638,535,78,c.UltimateText,16);
   Label(1008,718,535,37,c.LeaderText,15,gold);
   if(Button(1008,757,256,31,"养成此卡",false,profile.Owned[i]))page="养成";
   if(Button(1275,757,270,31,"换入阵位 "+(selectedSlot+1),false,profile.Owned[i]))Change(()=>{profile.SetMember(selectedSlot,i);battle=null;notice="已将 "+c.DisplayName+" 换入阵位 "+(selectedSlot+1);});
  }
  void DrawTeam(){
   Heading("队伍编成","先选阵位，再点下方角色换入；点已上阵角色会交换位置。预设保存阵位与队长，装备和策略跟随角色。");
   var leader=Data.Cards[profile.Team[profile.Leader]];
   for(int s=0;s<7;s++){
    int i=profile.Team[s],slot=s;var c=Data.Cards[i];float x=224+(s<3?s:s-3)*337,y=s<3?223:383;
    Box(x,y,322,145,panel);Box(x,y,322,3,CardColor(i));
    Label(x+12,y+12,302,30,(s+1)+"  "+c.Name+(profile.Leader==s?" [队长]":""),22);
    Label(x+12,y+47,300,45,c.Title+" · "+RoleText(c.Role)+"\n"+(c.HasTag(leader.LeaderTag)?"受到队长加成":"未满足队长条件"),15,c.HasTag(leader.LeaderTag)?gold:muted);
    if(Button(x+12,y+100,140,34,selectedSlot==s?"已选择":"选择阵位",selectedSlot==s))selectedSlot=s;
    if(Button(x+165,y+100,145,34,"设为队长"))Change(()=>{profile.Leader=slot;battle=null;});
   }
   Label(224,545,1330,45,leader.LeaderText+"；覆盖 "+profile.Team.Count(i=>Data.Cards[i].HasTag(leader.LeaderTag))+" / 7 人。",19,gold);
   for(int p=0;p<3;p++){
    int preset=p;float x=224+p*450;
    if(Button(x,599,213,40,"保存阵容 "+(p+1)))Change(()=>{profile.SavePreset(preset);notice="已保存阵容 "+(preset+1)+"（阵位 + 队长）";});
    if(Button(x+223,599,207,40,"载入阵容 "+(p+1),false,p<profile.Presets.Count))Change(()=>{profile.LoadPreset(preset);battle=null;notice="已载入阵容 "+(preset+1);});
   }
   Label(224,652,1330,32,"候补与上阵角色 · 目标阵位 "+(selectedSlot+1)+" · 新旧存档均获得雅木茶、饺子、龟仙人用于配队",17,muted);
   var owned=Enumerable.Range(0,Data.Cards.Length).Where(i=>profile.Owned[i]).ToArray();
   reserveScroll=GUI.BeginScrollView(new Rect(224,693,1340,108),reserveScroll,new Rect(0,0,owned.Length*220,85));
   for(int n=0;n<owned.Length;n++){
    int i=owned[n];int slot=Array.IndexOf(profile.Team,i);var c=Data.Cards[i];
    if(Button(n*220,0,208,78,c.Name+" · "+c.Title+"\n"+(slot<0?"候补":("阵位 "+(slot+1))),slot==selectedSlot))Change(()=>{profile.SetMember(selectedSlot,i);battle=null;});
   }GUI.EndScrollView();
  }
  string CompareItem(Item item,Item current){
   double attack=(item.kind==1?0:item.Bonus)+(item.gem?.008:0)-(current==null?0:(current.kind==1?0:current.Bonus)+(current.gem?.008:0));
   double defense=(item.kind==1?item.Bonus:0)-(current!=null&&current.kind==1?current.Bonus:0);
   return "攻 "+(attack*100).ToString("+0.#;-0.#;0")+"% 防 "+(defense*100).ToString("+0.#;-0.#;0")+"%";
  }
  void DrawInventory(int card){
   string[] kinds={"全部装备","攻击护腕","防护战斗服","稀有核心"},owners={"所有归属","未装备","当前角色"};
   if(Button(224,568,250,33,kinds[bagKind])){bagKind=(bagKind+1)%4;bagScroll=Vector2.zero;}
   if(Button(490,568,250,33,owners[bagOwner])){bagOwner=(bagOwner+1)%3;bagScroll=Vector2.zero;}
   if(Button(756,568,250,33,bagLock==0?"全部收藏":"仅收藏")){bagLock=1-bagLock;bagScroll=Vector2.zero;}
   var items=profile.Items.Where(x=>(bagKind==0||x.kind==bagKind-1)&&(bagOwner==0||(bagOwner==1?x.owner<0:x.owner==card))&&(bagLock==0||x.locked)).OrderByDescending(x=>x.locked).ThenByDescending(x=>x.kind).ToArray();
   Label(1022,572,540,29,"背包 "+items.Length+" / "+profile.Items.Count+" · 比较包含镶嵌珠",16,muted);
   bagScroll=GUI.BeginScrollView(new Rect(224,612,1340,185),bagScroll,new Rect(0,0,1300,Math.Max(170,items.Length*80)));
   for(int n=0;n<items.Length;n++){
    var item=items[n];float y=n*80;Box(0,y,1290,73,panel);
    Label(12,y+6,700,30,item.Name+" · "+(item.owner<0?"未装备":Data.Cards[item.owner].DisplayName+" / 槽"+(item.slot+1))+(item.gem?" · 已镶珠":""),16,item.kind==2?gold:ink);
    if(Button(713,y+5,88,30,item.locked?"已收藏":"收藏",item.locked))Change(()=>item.locked=!item.locked);
    for(int s=0;s<3;s++){
     int slot=s;var current=profile.Items.FirstOrDefault(x=>x.owner==card&&x.slot==slot);
     if(Button(820+s*150,y+5,140,31,"装到槽 "+(s+1)))Change(()=>profile.Equip(item.id,card,slot));
     Label(12+s*420,y+42,414,26,"槽 "+(s+1)+" 对比："+CompareItem(item,current),15,muted);
    }
   }
   if(items.Length==0)Label(15,20,1000,45,"当前筛选下没有装备。试炼胜利会掉落装备，收藏不会消耗物品。",18,muted);
   GUI.EndScrollView();
  }
 }
}
