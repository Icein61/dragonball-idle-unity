using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame:MonoBehaviour {
  Profile profile;Battle battle;BattleEvent last;
  string page="主页",notice="",reward="",savePath,saveError="";
  int selectedCard,selectedSlot,seed=1024,rate=1;bool running,settled,autoRepeat,paused;
  float clock,fx,repeatTimer,saveClock;Font font;Texture2D disc;
  List<string> log=new List<string>();
  GUIStyle text;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(FindObjectOfType<DemoGame>()==null)new GameObject("Dragon Idle").AddComponent<DemoGame>();}
  void Awake(){
   Application.targetFrameRate=60;Application.runInBackground=true;
   font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
   disc=new Texture2D(64,64);for(int y=0;y<64;y++)for(int x=0;x<64;x++)disc.SetPixel(x,y,Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))<31?Color.white:Color.clear);disc.Apply();
   string[] args=Environment.GetCommandLineArgs();
   int saveArg=Array.IndexOf(args,"-savePath");savePath=saveArg>=0&&saveArg+1<args.Length?args[saveArg+1]:Data.DefaultSavePath(Application.persistentDataPath);
   if(args.Contains("-logicSmoke")&&saveArg<0){Debug.LogError("Smoke requires isolated -savePath");Application.Quit(1);return;}
   Load();
   if(saveError=="")Change(()=>{profile.OfflineBank+=profile.Offline(DateTime.UtcNow.Ticks);profile.LastClaimTicks=DateTime.UtcNow.Ticks;});
   InitializeMenuAudio(args.Contains("-logicSmoke")||Application.isBatchMode);
   if(args.Contains("-logicSmoke")){RunLogicSmoke(args);return;}
   EnsureGallery();BeginShellLoad("主页");
   if(args.Contains("-smoke"))StartCoroutine(Smoke(args));
  }
  void Load(){
   try{if(File.Exists(savePath)){
    profile=JsonUtility.FromJson<Profile>(File.ReadAllText(savePath));if(profile==null)throw new Exception("空存档");
    bool migrating=profile.NeedsRosterMigration;
    if(migrating)File.Copy(savePath,savePath+(profile.schemaVersion<3?".pre-v041-":".pre-v080-")+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".bak",false);
    profile.Normalize();profile.CreateTeam();
    if(migrating)notice="已更新角色目录。退役卡成长已归档，装备已退回背包，阵容已补齐。";
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
   ClearBattlePresentation();battle=storyMode?profile.NewStoryBattle(profile.StorySelected,++seed):profile.NewBattle(profile.SelectedStage,++seed);log.Clear();last=null;reward="";settled=false;running=true;paused=false;clock=0;page="挑战";repeatTimer=0;
  }
  void Advance(){
   if(battle==null)return;
   var before=ActionPresentation.Capture(battle.Units);
   last=battle.Step();if(last!=null){log.Insert(0,PreparingAction(last));if(log.Count>80)log.RemoveAt(log.Count-1);fx=1;}
   action=last==null?null:new ActionPresentation(last,before,battle.Units);playedImpacts=0;
   if(last==null){SettleBattle();return;}
   if(last!=null&&last.Move!=Move.Stunned)PlayBattleSound(0);
   if(Application.isBatchMode){if(action!=null){action.Tick(action.Duration,false);log[0]=last.Text;}SettleBattle();}
  }
  void SettleBattle(){
   if(battle!=null&&battle.Result!=0){
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
   UpdateBattlePresentation(Time.unscaledDeltaTime);
   if(!running&&settled&&autoRepeat&&battle!=null&&battle.Result==1){repeatTimer+=Time.unscaledDeltaTime;if(repeatTimer>3)StartBattle();}
   if(Input.GetKeyDown(KeyCode.Space)&&running)paused=!paused;
   if(Input.GetKeyDown(KeyCode.Escape)&&running)paused=!paused;
  }
  void OnApplicationQuit(){FlushAudioSettings();if(profile!=null&&saveError==""){try{profile.LastClaimTicks=DateTime.UtcNow.Ticks;Save();}catch(Exception ex){Debug.LogWarning(ex.Message);}}}
  void Styles(){
   if(text!=null)return;
   text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true,richText=false};
  }
  void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
  void Circle(float x,float y,float size,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,size,size),disc);GUI.color=Color.white;}
  void Label(float x,float y,float w,float h,string s,int size=18,Color? c=null){text.fontSize=size;text.normal.textColor=c??InkIvory;GUI.Label(new Rect(x,y,w,h),s,text);}
  Color CardColor(int i){Color c;ColorUtility.TryParseHtmlString("#"+Data.Cards[i].Color,out c);return c;}
  string RoleText(string role){switch(role){case "tank":return "守护";case "healer":return "治疗";case "support":return "支援";case "control":return "控制";case "sniper":return "后排突破";default:return "均衡输出";}}
  string EffectText(string id){return id=="attack"?"攻击强化":id=="defense"?"防御强化":id=="damage"?"伤害强化":id=="speed"?"速度修正":id=="amplify"?"增幅":id;}
  void Line(Vector2 a,Vector2 b,float width,Color c){
   Matrix4x4 m=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;GUIUtility.RotateAroundPivot(angle,a);Box(a.x,a.y,(b-a).magnitude,width,c);GUI.matrix=m;
  }
  void OnGUI(){
   if(page!="挑战")return;
   Styles();float scale=Math.Min(Screen.width/1600f,Screen.height/900f);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
   DrawInkBattlePage();
  }
  void DrawInkBattlePage(){
   Box(0,0,1600,900,InkMist);
   Box(0,0,1600,90,InkDark);
   Box(0,90,190,810,InkPaper);
   Box(0,88,1600,2,InkBorder);
   Label(26,20,355,42,Data.GameTitle,29,InkIvory);
   Label(955,28,610,38,"素材  "+profile.Materials+"     晶石  "+profile.Crystals+"     珠子  "+profile.Gems,19,InkOnDark);

   for(int i=0;i<FeatureRoutes.Length;i++){
    string target=FeatureRoutes[i];
    if(InkGuiButton(18,126+i*62,154,45,target,enabled:!running||target=="挑战",selected:target=="挑战")&&target!="挑战"){
     NavigateGallery(target);
     return;
    }
   }
   if(InkGuiButton(18,819,154,47,"返回主页",enabled:!running)){NavigateGallery("主页");return;}
   DrawChapterSelection();
   InkGuiSurface(224,280,1045,440,InkPaper);
   // Side fields keep formation readable while the fight crosses a light, clear center.
   InkGuiSurface(234,284,254,432,InkLight);
   InkGuiSurface(1005,284,254,432,InkLight);
   Label(250,283,290,22,"我方  /  出战队伍",14,InkText);
   Label(1015,283,230,22,storyMode?"敌方  /  剧情挑战":"敌方  /  镜像试炼",14,InkLowHealth);
   var units=battle!=null?battle.Units:profile.CreateTeam().Concat(storyMode?profile.StoryEnemies(profile.StorySelected):profile.CreateEnemies(profile.SelectedStage)).ToList();
   DrawBattleActors(units);
   DrawInkBattleCharge(units);
   InkGuiSurface(1285,280,280,440,InkDark);Label(1301,294,250,34,"行动记录",20,InkIvory);
   for(int i=0;i<Math.Min(6,log.Count);i++)Label(1301,338+i*58,245,56,log[i],14,i==0?InkIvory:InkOnDark);
   if(log.Count==0)Label(1301,345,240,130,"点击开始挑战。\n每次行动会记录招式、伤害和附加效果。",17,InkOnDark);
   // Shared action track, markers represent all actors.
   InkGuiSurface(224,734,1045,61,InkPaper);Label(238,747,100,30,"行动进度",16,InkText);
   Label(335,738,32,24,"我",13,InkCharge);Label(335,764,32,24,"敌",13,InkLowHealth);
   Box(366,751,860,2,InkBorder);Box(366,777,860,2,InkBorder);
   foreach(var u in units.Where(x=>ShownVitals(x).HP>0)){
    float x=366+(float)u.Progress*850,y=u.Side==0?742:768;
    Circle(x,y,19,u.Side==0?InkGold:InkLowHealth);
    Label(x+4,y-1,25,24,(u.Slot+1).ToString(),12,u.Side==0?InkWarmText:InkIvory);
   }
   if(InkGuiButton(1285,736,133,54,running?(paused?"继续":"暂停"):"开始挑战",primary:true,enabled:running||saveError=="")){if(running)paused=!paused;else StartBattle();}
   if(InkGuiButton(1430,736,135,54,"速度 ×"+rate)){rate=rate==1?2:rate==2?4:1;}
   if(running){if(InkGuiButton(1285,804,133,46,"退出本场")){running=false;settled=false;autoRepeat=false;battle=null;ClearBattlePresentation();notice="已退出，未消耗资源。";}}
   else if(InkGuiButton(1285,804,280,46,autoRepeat?"自动重复：开":"自动重复：关",enabled:saveError=="",selected:autoRepeat))autoRepeat=!autoRepeat;
   if(battle!=null&&battle.Result!=0&&!running){
    InkGuiSurface(460,403,600,206,InkDark);
    Box(485,415,550,2,battle.Result==1?InkGold:InkLowHealth);
    Label(485,425,550,43,battle.Result==1?"试炼通过":"挑战结束",30,InkIvory);
    Label(485,477,550,105,reward+(autoRepeat?"\n3秒后自动继续挑战":""),16,InkOnDark);
   }
   string feedback=saveError!=""?saveError:notice!=""?notice:last==null?"":log.Count>0?log[0]:last.Text;
   if(feedback!=""){
    InkGuiSurface(224,803,1045,47,InkDark);
    Label(241,808,1010,39,feedback,17,InkIvory);
   }
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
   File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"version\":\""+Data.Version+"\",\"battleWon\":true,\"saveRoundTrip\":true,\"events\":"+events+"}");
   Application.Quit(0);
  }
 }
}
