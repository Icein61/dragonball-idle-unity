using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame:MonoBehaviour {
  Profile profile;Battle battle;BattleEvent last;
  string page="角色",notice="",reward="",savePath,saveError="";
  int selectedCard,selectedSlot,selectedSkillSlot,seed=1024,rate=1;bool running,settled,autoRepeat,paused;
  float clock,fx,repeatTimer,saveClock;Font font;Texture2D gradient,disc;
  List<string> log=new List<string>();Vector2 bagScroll;
  readonly Color bg=new Color(.035f,.055f,.09f), panel=new Color(.07f,.10f,.15f), muted=new Color(.53f,.62f,.73f),gold=new Color(1,.72f,.30f),ink=new Color(.90f,.94f,1);
  GUIStyle text,button;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(FindObjectOfType<DemoGame>()==null)new GameObject("Dragon Idle").AddComponent<DemoGame>();}
  void Awake(){
   Application.targetFrameRate=60;Application.runInBackground=true;
   font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
   gradient=new Texture2D(1,256);for(int i=0;i<256;i++)gradient.SetPixel(0,i,Color.Lerp(new Color(.025f,.04f,.07f),new Color(.12f,.19f,.25f),i/255f));gradient.Apply();
   disc=new Texture2D(64,64);for(int y=0;y<64;y++)for(int x=0;x<64;x++)disc.SetPixel(x,y,Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))<31?Color.white:Color.clear);disc.Apply();
   string[] args=Environment.GetCommandLineArgs();
   int saveArg=Array.IndexOf(args,"-savePath");savePath=saveArg>=0&&saveArg+1<args.Length?args[saveArg+1]:Path.Combine(Application.persistentDataPath,"profile-v1.json");
   if(args.Contains("-logicSmoke")&&saveArg<0){Debug.LogError("Smoke requires isolated -savePath");Application.Quit(1);return;}
   Load();
   if(saveError=="")Change(()=>{profile.OfflineBank+=profile.Offline(DateTime.UtcNow.Ticks);profile.LastClaimTicks=DateTime.UtcNow.Ticks;});
   InitializeMenuAudio(args.Contains("-logicSmoke")||Application.isBatchMode);
   if(args.Contains("-logicSmoke")){RunLogicSmoke(args);return;}
   EnsureGallery();
   if(args.Contains("-smoke"))StartCoroutine(Smoke(args));
  }
  void Load(){
   try{if(File.Exists(savePath)){
    profile=JsonUtility.FromJson<Profile>(File.ReadAllText(savePath));if(profile==null)throw new Exception("空存档");
    bool migrating=profile.schemaVersion<3;
    if(migrating)File.Copy(savePath,savePath+".pre-v041-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".bak",false);
    profile.Normalize();profile.CreateTeam();
    if(migrating)notice="已升级为赛亚人篇目录；移除角色成长已归档，其装备退回背包，阵容已补齐。";
   }else{profile=new Profile();profile.LastClaimTicks=DateTime.UtcNow.Ticks;Save();}}
   catch(Exception ex){profile=new Profile();profile.LastClaimTicks=DateTime.UtcNow.Ticks;saveError="存档读取失败，原文件保留。请检查备份："+ex.Message;}
  }
  void Save(){
   if(saveError!="")throw new IOException(saveError);
   profile.Validate();Directory.CreateDirectory(Path.GetDirectoryName(savePath));string tmp=savePath+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(profile,true));
   if(File.Exists(savePath))File.Replace(tmp,savePath,savePath+".bak");else File.Move(tmp,savePath);
  }
  bool Change(Action change){
   string before=JsonUtility.ToJson(profile);
   try{change();Save();galleryDirty=true;return true;}catch(Exception ex){profile=JsonUtility.FromJson<Profile>(before);notice="保存失败，本次操作已撤回："+ex.Message;return false;}
  }
  void StartBattle(){
   if(saveError!=""){notice=saveError;return;}
   battle=storyMode?profile.NewStoryBattle(profile.StorySelected,++seed):profile.NewBattle(profile.SelectedStage,++seed);log.Clear();last=null;reward="";settled=false;running=true;paused=false;clock=0;page="挑战";repeatTimer=0;
  }
  void Advance(){
   last=battle.Step();if(last!=null){log.Insert(0,last.Text);if(log.Count>80)log.RemoveAt(log.Count-1);fx=1;}
   if(battle.Result!=0){
    running=false;
    if(!settled){
     if(battle.Result==1){string r="";if(Change(()=>{r=storyMode?profile.RewardStory(profile.StorySelected,seed+9000):profile.Reward(profile.SelectedStage,seed+9000);profile.LastClaimTicks=DateTime.UtcNow.Ticks;})){reward=r;settled=true;}else{reward="奖励保存失败。请返回后重试；未扣除资源。";autoRepeat=false;}}
     else{reward="本次未通过。调整阵位、策略和装备后再挑战。\n失败不损失资源。";settled=true;autoRepeat=false;}
    }
   }
  }
  void Update(){
   UpdateGallery();UpdateMenuAudio();
   saveClock+=Time.unscaledDeltaTime;if(saveClock>60&&saveError==""){saveClock=0;Change(()=>profile.LastClaimTicks=DateTime.UtcNow.Ticks);}
   fx=Math.Max(0,fx-Time.unscaledDeltaTime*rate*1.4f);
   if(running&&!paused){clock+=Time.unscaledDeltaTime*rate;if(clock>=.85f){clock-=.85f;Advance();}}
   if(!running&&settled&&autoRepeat&&battle!=null&&battle.Result==1){repeatTimer+=Time.unscaledDeltaTime;if(repeatTimer>3)StartBattle();}
   if(Input.GetKeyDown(KeyCode.Space)&&running)paused=!paused;
   if(Input.GetKeyDown(KeyCode.Escape)&&running)paused=!paused;
  }
  void OnApplicationQuit(){FlushAudioSettings();if(profile!=null&&saveError==""){try{profile.LastClaimTicks=DateTime.UtcNow.Ticks;Save();}catch(Exception ex){Debug.LogWarning(ex.Message);}}}
  void Styles(){
   if(text!=null)return;
   text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true,richText=false};
   button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,alignment=TextAnchor.MiddleCenter};
   button.normal.background=Texture2D.whiteTexture;button.hover.background=Texture2D.whiteTexture;button.active.background=Texture2D.whiteTexture;
   button.normal.textColor=ink;button.hover.textColor=Color.white;button.active.textColor=Color.white;
  }
  void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
  void Circle(float x,float y,float size,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,size,size),disc);GUI.color=Color.white;}
  void Label(float x,float y,float w,float h,string s,int size=18,Color? c=null){text.fontSize=size;text.normal.textColor=c??ink;GUI.Label(new Rect(x,y,w,h),s,text);}
  bool Button(float x,float y,float w,float h,string s,bool active=false,bool enabled=true){
   bool old=GUI.enabled;GUI.enabled=old&&enabled;GUI.backgroundColor=active?new Color(.63f,.36f,.10f):new Color(.13f,.19f,.27f);
   bool clicked=GUI.Button(new Rect(x,y,w,h),s,button);GUI.enabled=old;GUI.backgroundColor=Color.white;if(clicked)PlayUISound(0);return clicked;
  }
  void Bar(float x,float y,float w,float h,double value,Color c){Box(x,y,w,h,new Color(.02f,.035f,.055f));Box(x,y,w*(float)Math.Max(0,Math.Min(1,value)),h,c);}
  Color CardColor(int i){Color c;ColorUtility.TryParseHtmlString("#"+Data.Cards[i].Color,out c);return c;}
  string RoleText(string role){switch(role){case "tank":return "守护";case "healer":return "治疗";case "support":return "支援";case "control":return "控制";case "sniper":return "后排突破";default:return "均衡输出";}}
  string SkillText(string id){return Data.SkillName(id);}
  string EffectText(string id){return id=="attack"?"攻击强化":id=="defense"?"防御强化":id=="damage"?"伤害强化":id=="speed"?"速度修正":id=="amplify"?"增幅":id;}
  void Line(Vector2 a,Vector2 b,float width,Color c){
   Matrix4x4 m=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;GUIUtility.RotateAroundPivot(angle,a);Box(a.x,a.y,(b-a).magnitude,width,c);GUI.matrix=m;
  }
  void OnGUI(){
   if(page=="角色")return;
   Styles();float scale=Math.Min(Screen.width/1600f,Screen.height/900f);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
   GUI.DrawTexture(new Rect(0,0,1600,900),gradient);
   Box(0,0,1600,90,bg);Box(0,90,190,810,new Color(.045f,.07f,.11f));Box(0,88,1600,2,new Color(.25f,.22f,.16f));
   Label(26,20,390,40,"龙珠 · 试炼前线",29,gold);Label(360,34,350,30,"技术 DEMO  /  v"+Data.Version,16,muted);
   Label(890,30,670,36,"素材  "+profile.Materials+"     晶石  "+profile.Crystals+"     珠子  "+profile.Gems+"     胜场  "+profile.Wins,19);
   string[] pages={"挑战","角色","编队","养成","装备","召唤","说明"};
   for(int i=0;i<pages.Length;i++)if(Button(18,126+i*68,154,50,pages[i],page==pages[i],!running||pages[i]=="挑战")){page=pages[i];notice="";if(page!="挑战")autoRepeat=false;}
   Label(22,620,144,130,"自动行动\n战前构筑\n离线单机",18,muted);
   Label(22,810,145,70,"临时角色表现\n非最终美术",14,gold);
   if(page=="挑战")DrawBattle();
   else if(page=="编队")DrawTeam();
   else if(page=="养成")DrawTraining();
   else if(page=="装备")DrawEquipment();
   else if(page=="召唤")DrawSummon();
   else DrawAbout();
   if(notice!=""){Box(220,815,1340,58,panel);Label(238,826,1290,45,notice,17,gold);}
   if(saveError!="")Label(220,760,1300,50,saveError,16,Color.red);
  }
  void Heading(string title,string subtitle){
   Label(224,112,1000,44,title,30);Label(226,162,1280,45,subtitle,17,muted);
  }
  void DrawBattle(){
   DrawChapterSelection();
   Box(224,280,1045,440,new Color(.045f,.08f,.12f));
   // Scene framing and ground lines.
   for(int i=0;i<7;i++)Line(new Vector2(260,400+i*44),new Vector2(1230,400+i*44),1,new Color(.12f,.19f,.22f));
   for(int i=0;i<9;i++)Line(new Vector2(740,300),new Vector2(240+i*125,715),1,new Color(.10f,.16f,.20f));
   Label(250,291,290,30,"我方  /  出战队伍",17,gold);Label(1015,291,230,30,storyMode?"敌方  /  剧情挑战":"敌方  /  镜像试炼",17,new Color(.95f,.5f,.48f));
   var units=battle!=null?battle.Units:profile.CreateTeam().Concat(storyMode?profile.StoryEnemies(profile.StorySelected):profile.CreateEnemies(profile.SelectedStage)).ToList();
   foreach(var u in units)DrawUnit(u);
   if(last!=null&&last.Target!=null&&fx>0){
    foreach(var hit in last.Hits){
     Vector2 a=Position(last.Actor)+new Vector2(65,30),b=Position(hit.Target)+new Vector2(65,30);
     Line(a,b,3+fx*5,last.Move==Move.Ultimate?gold:new Color(.4f,.8f,1,fx));
     Label(b.x-15,b.y-36-(1-fx)*30,150,50,hit.Dodged?"闪避":hit.Damage.ToString("0"),26,hit.Critical?gold:Color.white);
    }
   }
   if(running)DrawActionPortrait();
   Box(1285,280,280,440,panel);Label(1301,294,250,34,"行动记录",20,gold);
   for(int i=0;i<Math.Min(6,log.Count);i++)Label(1301,338+i*58,245,56,log[i],14,i==0?ink:muted);
   if(log.Count==0)Label(1301,345,240,130,"点击开始挑战。\n每次行动会记录招式、伤害和附加效果。",17,muted);
   // Shared action track, markers represent all actors.
   Box(224,734,1045,61,panel);Label(238,747,100,30,"行动进度",16,muted);
   Label(335,738,32,24,"我",13,gold);Label(335,764,32,24,"敌",13,new Color(.9f,.35f,.35f));
   Bar(366,751,860,2,1,new Color(.22f,.30f,.39f));Bar(366,777,860,2,1,new Color(.22f,.30f,.39f));
   foreach(var u in units.Where(x=>x.Alive)){float x=366+(float)u.Progress*850;float y=u.Side==0?742:768;Circle(x,y,19,u.Side==0?gold:new Color(.9f,.35f,.35f));Label(x+4,y-1,25,24,(u.Slot+1).ToString(),12,bg);}
   if(Button(1285,736,133,54,running?(paused?"继续":"暂停"):"开始挑战",true)){if(running)paused=!paused;else StartBattle();}
   if(Button(1430,736,135,54,"速度 ×"+rate)){rate=rate==1?2:rate==2?4:1;}
   if(running){if(Button(1285,804,133,46,"退出本场")){running=false;settled=false;autoRepeat=false;battle=null;notice="已退出，未消耗资源。";}}
   else if(Button(1285,804,280,46,autoRepeat?"自动重复：开":"自动重复：关"))autoRepeat=!autoRepeat;
   if(battle!=null&&battle.Result!=0){
    Box(460,403,600,206,new Color(.035f,.055f,.09f,.97f));Box(460,403,600,3,battle.Result==1?gold:Color.red);
    Label(485,419,550,43,battle.Result==1?"试炼通过":"挑战结束",30,gold);Label(485,472,550,105,reward+(autoRepeat?"\n3秒后自动继续挑战":""),16);
   }else if(last!=null)Label(242,804,1000,45,last.Text,18,gold);
  }
  Vector2 Position(Unit u){
   float x=u.Side==0?(u.Slot<3?480:275):(u.Slot<3?875:1080);
   float y=u.Slot<3?350+u.Slot*112:330+(u.Slot-3)*94;
   return new Vector2(x,y);
  }
  void DrawUnit(Unit u){
   Vector2 p=Position(u);int ci=Array.FindIndex(Data.Cards,c=>c.Id==u.CardId);Color color=CardColor(Math.Max(0,ci));
   Color cardBg=u.Alive?new Color(.10f,.15f,.21f):new Color(.06f,.07f,.09f);
   Box(p.x,p.y,160,80,cardBg);Box(p.x,p.y,3,80,u.Side==0?color:new Color(.7f,.25f,.29f));
   if(last!=null&&last.Actor==u&&fx>0)Box(p.x,p.y,160,3,gold);
   var portrait=ci>=0?PortraitTexture(ci):null;
   if(portrait!=null){GUI.color=u.Alive?Color.white:muted;GUI.DrawTexture(new Rect(p.x+5,p.y+2,38,49),portrait,ScaleMode.ScaleToFit,true);GUI.color=Color.white;Label(p.x+5,p.y+33,20,20,(u.Slot+1).ToString(),12,gold);}
   else {Circle(p.x+10,p.y+10,29,u.Alive?color:muted);Label(p.x+18,p.y+10,30,28,(u.Slot+1).ToString(),16,bg);}
   Label(p.x+46,p.y+5,112,26,u.Name.Replace("试炼·",""),16,u.Alive?ink:muted);
   Label(p.x+46,p.y+29,110,20,u.Alive?"CD "+u.Cooldown+"  怒 "+u.Rage.ToString("0"):"已退场",12,muted);
   Bar(p.x+10,p.y+54,140,5,u.HP/u.MaxHP,u.Side==0?new Color(.25f,.75f,.54f):new Color(.9f,.36f,.4f));
   Bar(p.x+10,p.y+65,140,3,u.Rage/100,gold);
   if(u.Shield>0)Label(p.x+12,p.y-17,150,20,"护盾 "+u.Shield.ToString("0"),12,new Color(.4f,.8f,1));
   if(u.Stunned)Label(p.x+80,p.y-18,80,20,"眩晕",12,gold);
   if(new Rect(p.x,p.y,160,80).Contains(Event.current.mousePosition)){
    string buffs=string.Join("\n",u.Effects.Select(e=>EffectText(e.Type)+" "+(e.Value*100).ToString("0.#")+"% / "+(e.Expires==int.MaxValue?"整场":(e.Expires-u.Opportunities)+"回合")).ToArray());
    Box(610,290,268,148,bg);Label(624,300,240,132,"生命 "+u.HP.ToString("0")+" / "+u.MaxHP.ToString("0")+"\n队长 生/攻/防 +"+(u.LeaderHealth*100).ToString("0")+"/"+(u.LeaderAttack*100).ToString("0")+"/"+(u.LeaderDefense*100).ToString("0")+"%\n"+buffs,13);
   }
  }
  void CardTabs(){
   var owned=Enumerable.Range(0,Data.Cards.Length).Where(i=>profile.Owned[i]).ToArray();
   if(selectedCard<0||selectedCard>=profile.Owned.Length||!profile.Owned[selectedCard])selectedCard=owned[0];int pos=Array.IndexOf(owned,selectedCard);
   if(Button(224,214,150,42,"上一位"))selectedCard=owned[(pos+owned.Length-1)%owned.Length];
   Label(395,220,700,36,Data.Cards[selectedCard].DisplayName+"  /  "+(pos+1)+" / "+owned.Length,22,gold);
   if(Button(1100,214,150,42,"下一位"))selectedCard=owned[(pos+1)%owned.Length];
   if(Button(1270,214,290,42,"返回角色与图鉴"))page="角色";
  }
  void DrawTraining(){
   Heading("角色养成","每次修炼提供细小积累；演示数值尚未完成长期平衡。所有角色均为机制测试卡。");CardTabs();
   int i=selectedCard;var c=Data.Cards[i];var u=Data.Make(c.Id,0,0,profile.Training[i],profile.Resonance[i],profile.Skill[i]=="pierce"||profile.Skill2[i]=="pierce"?"pierce":"",1);
   Box(224,280,410,480,panel);DrawPortrait(i,new Rect(315,292,225,275));
   Label(250,570,360,70,c.DisplayName,25);Label(250,635,360,40,c.Stage+" · "+c.Form,18,muted);
   Label(250,680,360,70,c.KeywordText+"\n完整队长条件见角色详情",16,gold);
   Box(655,280,910,480,panel);
   Label(680,305,840,45,"生命 "+u.MaxHP.ToString("0")+"    攻击 "+u.Attack.ToString("0.0")+"    防御 "+u.Defense.ToString("0.0")+"    速度 "+u.Speed.ToString("0"),23);
   Label(680,360,840,98,"绝招："+u.SpecialName+" / 冷却 "+u.CooldownLength+" 次正常出手\n必杀："+u.UltimateName+" / 消耗 100 怒气，额外行动\n被动："+c.PassiveText,19);
   if(Button(680,475,400,48,"修炼 "+profile.Training[i]+" · 消耗 "+(30+profile.Training[i]/10)+" 素材")){
    Change(()=>notice=profile.Train(i)?"修炼完成：基础属性 +0.1%。":"素材不足");
   }
   if(Button(1100,475,430,48,"共鸣 "+profile.Resonance[i]+" · 专属资源 "+profile.Shards[i]))Change(()=>notice=profile.ResonanceUp(i)?"共鸣提升：基础属性 +0.4%。":"需要 "+(3+profile.Resonance[i])+" 份本卡专属资源");
   Label(680,545,840,34,"战前必杀策略",20,gold);
   string[] policies={"怒气足够即释放","自身生命 ≤ 60%","目标生命 ≤ 50%"};
   for(int p=0;p<3;p++){int policy=p;if(Button(680+p*285,591,270,43,policies[p],profile.Policy[i]==p))Change(()=>profile.Policy[i]=policy);}
   Label(680,667,840,65,"修炼增加永久属性；战斗内强化按来源独立到期。\n完整分支成长、更多技能条件将在后续版本扩展。",17,muted);
  }
  void DrawEquipment(){
   Heading("装备工坊","3 个装备槽 · 珠子可无损拆卸 · 绝招替换不改变身份标签");CardTabs();
   int card=selectedCard;
   for(int slot=0;slot<3;slot++){
    int s=slot;float x=224+slot*445;var item=profile.Items.FirstOrDefault(a=>a.owner==card&&a.slot==s);
    Box(x,280,425,157,panel);Label(x+15,294,390,33,"装备 "+(slot+1)+" / "+(item==null?"空":item.Name),21,item!=null&&item.kind==2?gold:ink);
    if(item!=null){
     Label(x+15,332,390,28,(item.kind==1?"防御":"攻击强化")+" +"+(item.Bonus*100).ToString("0.#")+"% · 1 珠孔",16,muted);
     if(Button(x+15,378,190,40,item.gem?"拆下攻击珠":"镶嵌攻击珠",false,item.gem||profile.Gems>0))Change(()=>{if(item.gem){item.gem=false;profile.Gems++;}else{item.gem=true;profile.Gems--;}});
     if(Button(x+220,378,190,40,"卸下装备"))Change(()=>item.owner=-1);
    }
   }
   Label(224,457,480,34,"额外技能 · 最多 1 个绝招替换",20,gold);
   if(Button(750,451,390,36,"技能槽 1："+SkillText(profile.Skill[card]),selectedSkillSlot==0))selectedSkillSlot=0;
   if(Button(1160,451,390,36,"技能槽 2："+SkillText(profile.Skill2[card]),selectedSkillSlot==1))selectedSkillSlot=1;
   for(int k=0;k<4;k++){
    int skill=k;string id=Data.SkillIds[k];bool unlocked=profile.CanUseSkill(id);
    string title=k==0?"卸下技能":Data.SkillName(id)+(unlocked?" · 可装备":" · 残页 "+profile.SkillPages[k]+"/3 解锁");
    if(Button(224+k*337,498,321,40,title,(selectedSkillSlot==0?profile.Skill[card]:profile.Skill2[card])==id,unlocked||profile.SkillPages[k]>=3))Change(()=>{
     if(!unlocked){profile.UnlockSkill(skill);notice="已解锁 "+Data.SkillName(id)+"，点击可装备。";}
     else notice=profile.SetSkill(card,selectedSkillSlot,id)?Data.SkillDescription(id):"同一技能不能重复装备。";
    });
    if(new Rect(224+k*337,498,321,40).Contains(Event.current.mousePosition))Label(224,541,1330,26,Data.SkillDescription(id),15,gold);
   }
   DrawInventory(card);
  }
  void DrawSummon(){
   Heading("时空召唤","Demo 测试卡池 · 完全离线 · 无付费入口");
   Box(224,230,1340,520,panel);
   if(Button(270,248,340,44,"赛亚人篇 · 15卡",saiyanPool))saiyanPool=true;
   if(Button(1160,248,340,44,"基础常驻 · 10卡",!saiyanPool))saiyanPool=false;
   Circle(740,282,180,new Color(.8f,.45f,.16f));Circle(758,300,144,gold);Label(796,337,150,100,"★",67,bg);
   Label(290,510,1190,47,"每次 50 晶石 · R 72% / SR 20% / SSR 8% · 每第 20 次保证 SSR",24,gold);
   Label(290,570,1180,70,(saiyanPool?"赛亚人篇15卡":"基础常驻10卡")+"，同级均分；两池均仅含赛亚人篇；原两池保底分别保留，提前出SSR不重置。重复卡转本卡专属资源。\n本池召唤 "+(saiyanPool?profile.SaiyanPulls:profile.Pulls)+" 次；距保证SSR "+(20-(saiyanPool?profile.SaiyanPulls:profile.Pulls)%20)+" 次。详情见角色图鉴。",19);
   if(Button(655,658,480,58,"召唤一次",true,profile.Crystals>=50))Change(()=>notice=saiyanPool?profile.SummonSaiyan(++seed):profile.Summon(++seed));
  }
  void DrawAbout(){
   Heading("技术 Demo / 开发边界","先验证战斗规则和可玩循环，再迭代正式角色与美术");
   Box(224,225,1340,560,panel);
   Label(250,250,1270,220,"已开放：25 张原型卡、拉蒂兹来袭4关、角色图鉴、独立卡片身份、多标签队长、候补换人、3 组阵容预设、定向掉落、技能残页与收藏筛选。\n\n角色阶段、稀有度与技能数值是可调整默认。悟空、迎战拉蒂兹比克、拉蒂兹使用 C 风格 AI 立绘和短暂特写；其余为占位表现，尚无骨骼模型或招式动画。旧存档按 CardId 迁移，迁移前自动保留独立备份。",21);
   int offline=profile.OfflineBank;
   Label(250,510,1250,75,"离线素材：每小时 12，最多累计 12 小时；至少通关一次后开放。\n当前可领取 "+offline+"。本版本离线只提供素材，物品掉落后续扩展。",19,muted);
   if(Button(250,610,360,47,"领取离线素材",false,offline>0))Change(()=>{int count=profile.OfflineBank;profile.Materials+=count;profile.OfflineBank=0;notice="已领取离线素材 "+count;});
   if(Button(635,610,360,47,"首胜任务 · 领取 50 晶石",false,profile.Wins>0&&!profile.FirstMission))Change(()=>{profile.Crystals+=50;profile.FirstMission=true;notice="首胜任务奖励已领取";});
   if(Button(1020,610,360,47,"打开存档目录"))Application.OpenURL(Path.GetDirectoryName(savePath));
   Label(250,690,1270,65,"存档："+savePath+"\n操作：鼠标选择；战斗时空格暂停；可调 1/2/4 倍演出。",15,muted);
  }
  IEnumerator Smoke(string[] args){
   int o=Array.IndexOf(args,"-smokeOut");string dir=o>=0?args[o+1]:Path.Combine(Application.persistentDataPath,"smoke");Directory.CreateDirectory(dir);
   yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"01-menu.png"));yield return new WaitForSeconds(.5f);
   StartBattle();for(int i=0;i<12;i++)Advance();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02-battle.png"));yield return new WaitForSeconds(.5f);
   int events=0;while(running&&events++<2000)Advance();
   if(battle.Result!=1||!settled)throw new Exception("Smoke battle failed");
   int wins=profile.Wins;profile=JsonUtility.FromJson<Profile>(File.ReadAllText(savePath));if(profile.Wins!=wins)throw new Exception("Save roundtrip failed");
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"03-result.png"));yield return new WaitForSeconds(.5f);
   page="养成";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"04-training.png"));yield return new WaitForSeconds(.5f);
   if(!Change(()=>{profile.SetSkill(0,0,"focus");profile.SetSkill(0,1,"pierce");profile.Train(0);profile.Equip(profile.Items.Last().id,0,1);}))throw new Exception("Loadout save failed");
   profile=JsonUtility.FromJson<Profile>(File.ReadAllText(savePath));
   if(profile.Skill2[0]!="pierce"||profile.Skill[0]!="focus"||profile.Training[0]<1)throw new Exception("Loadout persistence failed");
   page="装备";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"05-equipment.png"));yield return new WaitForSeconds(.5f);
   page="编队";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"06-team.png"));yield return new WaitForSeconds(.5f);
   page="召唤";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"07-summon.png"));yield return new WaitForSeconds(.5f);
   page="说明";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"08-about.png"));yield return new WaitForSeconds(.5f);
   File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"version\":\""+Data.Version+"\",\"battleWon\":true,\"saveRoundTrip\":true,\"events\":"+events+"}");
   Application.Quit(0);
  }
 }
}
