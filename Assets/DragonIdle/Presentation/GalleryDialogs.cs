using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  GameObject dialogReturnFocus;
  readonly Dictionary<UI.Selectable,bool> modalSelectables=new Dictionary<UI.Selectable,bool>();
  RectTransform NewGalleryDialog(string title,string subtitle){
   var restore=dialogRoot!=null?dialogReturnFocus:EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   CloseGalleryDialog();dialogReturnFocus=restore;
   foreach(var control in galleryRoot.GetComponentsInChildren<UI.Selectable>()){modalSelectables[control]=control.interactable;control.interactable=false;}
   dialogRoot=UPanel(galleryRoot,"ModalBackdrop",0,0,1600,900,new Color(0,0,0,.78f),true);
   var dismiss=dialogRoot.gameObject.AddComponent<UI.Button>();dismiss.transition=UI.Selectable.Transition.None;dismiss.navigation=new UI.Navigation{mode=UI.Navigation.Mode.None};dismiss.onClick.AddListener(CloseGalleryDialog);
   var card=UPanel(dialogRoot,"Modal",430,180,740,540,uiPanel,true);
   var swallow=card.gameObject.AddComponent<UI.Button>();swallow.transition=UI.Selectable.Transition.None;swallow.navigation=new UI.Navigation{mode=UI.Navigation.Mode.None};
   UPanel(card,"Rule",0,0,740,4,uiAccent);
   UText(card,"Title",title,26,22,620,40,27,uiPaper);
   UText(card,"Subtitle",subtitle,26,73,684,65,17,uiDim);
   var close=UButton(card,"Close","×",672,16,45,40,CloseGalleryDialog);
   if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(close.gameObject);
   return card;
  }
  void CloseGalleryDialog(){
   foreach(var pair in modalSelectables)if(pair.Key!=null)pair.Key.interactable=pair.Value;modalSelectables.Clear();
   if(dialogRoot!=null){dialogRoot.gameObject.SetActive(false);Destroy(dialogRoot.gameObject);dialogRoot=null;FlushAudioSettings();}
   var focus=dialogReturnFocus;dialogReturnFocus=null;
   if(EventSystem.current!=null){var control=focus==null?null:focus.GetComponent<UI.Selectable>();EventSystem.current.SetSelectedGameObject(control!=null&&control.isActiveAndEnabled&&control.interactable?focus:null);}
  }
  void ShowFilter(int kind){
   string[] labels,values;
   if(kind==0){values=new[]{""}.Concat(Data.Rarities).ToArray();labels=new[]{"全部稀有度"}.Concat(Data.Rarities).ToArray();}
   else if(kind==1){values=new[]{""}.Concat(Data.Cards.SelectMany(c=>c.Keywords).Distinct()).ToArray();labels=values.Select(v=>v==""?"全部定位":v).ToArray();}
   else if(kind==2){values=new[]{""}.Concat(Data.Cards.SelectMany(c=>c.Tags).Distinct()).ToArray();labels=values.Select(v=>v==""?"全部身份":v).ToArray();}
   else{values=new[]{"0","1","2","3"};labels=SortLabels;}
   var panel=NewGalleryDialog(new[]{"选择稀有度","选择战斗定位","选择身份标签","选择排序方式"}[kind],"直接选择一个条件；不同类别同时生效。筛选不会改变角色身份或队伍。");
   RectTransform body;UScroll(panel,"Options",26,151,685,355,out body);
   body.sizeDelta=new Vector2(671,Math.Max(355,((values.Length+1)/2)*58));
   for(int n=0;n<values.Length;n++){
    string value=values[n];int index=n;bool active=kind==0?catalog.Rarity==value:kind==1?catalog.Role==value:kind==2?catalog.Tag==value:catalog.Sort==n;
    UButton(body,"Option"+n,labels[n],n%2*337,n/2*58,324,46,()=>{
     if(kind==0)catalog.Rarity=value;else if(kind==1)catalog.Role=value;else if(kind==2)catalog.Tag=value;else catalog.Sort=index;
     CloseGalleryDialog();RefreshGallery(true);
    },active);
   }
  }
  void ShowFormationPicker(int i){
   var c=Data.Cards[i];int existing=Array.IndexOf(profile.Team,i);
   var panel=NewGalleryDialog("选择 "+c.Name+" 的阵位",existing>=0?"角色已在阵位"+(existing+1)+"；选择其他位置会交换两位角色。队长身份跟随阵位。":"选择位置以替换当前成员。被替换的角色回到候补，装备和成长保留。");
   for(int s=0;s<7;s++){
    int slot=s;var current=Data.Cards[profile.Team[s]];
    string value=(s<3?"前排 ":"后排 ")+(s+1)+(s==profile.Leader?" · 队长位":"")+"\n"+current.Name+" · "+current.Title;
    UButton(panel,"Slot"+s,value,26+s%2*345,155+s/2*82,332,68,()=>{
     string old=Data.Cards[profile.Team[slot]].Name;
     if(Change(()=>{if(!profile.SetMember(slot,i))throw new InvalidOperationException("角色不可上阵");battle=null;})){
      selectedSlot=slot;CloseGalleryDialog();GalleryNotice(c.Name+" 已进入阵位 "+(slot+1)+(existing>=0?"；已与 "+old+" 交换位置":"；"+old+" 回到候补"));RefreshGallery(false);
     }else GalleryNotice(notice,false);
    },s==existing,saveError=="");
   }
  }
  void ShowAcquisition(int i){
   var c=Data.Cards[i];var panel=NewGalleryDialog("获取 · "+c.Name,c.Title+" / "+c.Stage+" / "+c.Rarity+"。重复获得将转为本张卡的专属成长资源。");
   float y=160;
   if(Data.InSaiyanPool(c)){UButton(panel,"GoSaiyanPool","前往赛亚人篇召唤 · 独立保底",26,y,682,46,()=>{saiyanPool=true;NavigateGallery("召唤");});y+=66;}
   if(Data.InClassicPool(c)){UButton(panel,"GoLegacyPool","前往基础常驻召唤 · 仅赛亚人篇",26,y,682,46,()=>{saiyanPool=false;NavigateGallery("召唤");});y+=66;}
   UText(panel,"PityInfo","每抽50晶石；C72% / B20% / S8%，同级均分。每累计第20抽保证S，提前获得S不重置。",26,y+5,682,82,16,uiDim);
  }
  UI.Slider AudioSlider(Transform parent,string title,float y,float value,Action<float> update){
   var label=UText(parent,title+"Label",title+"  "+Mathf.RoundToInt(value*100)+"%",28,y,220,34,19,uiPaper);
   var track=UPanel(parent,title+"Slider",260,y+12,440,8,new Color(.22f,.27f,.30f),true);
   var handle=UPanel(track,"Handle",0,0,18,26,uiAccent,true);handle.pivot=new Vector2(.5f,.5f);
   var slider=track.gameObject.AddComponent<UI.Slider>();slider.direction=UI.Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UI.Image>();slider.SetValueWithoutNotify(value);
   slider.onValueChanged.AddListener(v=>{update(v);label.text=title+"  "+Mathf.RoundToInt(v*100)+"%";ScheduleAudioSave();});return slider;
  }
  void ApplyGalleryMotion(){foreach(var b in galleryRoot.GetComponentsInChildren<UI.Button>()){var c=b.colors;c.fadeDuration=audioSettings.ReducedMotion?0:.12f;b.colors=c;}}
  void ShowAudioSettings(){
   var panel=NewGalleryDialog("声音与显示","浏览角色时保持轻量反馈。设置自动保存；音乐切页不断播。当前配乐为原创合成氛围样曲。");
   AudioSlider(panel,"总音量",156,audioSettings.Master,v=>audioSettings.Master=v);
   AudioSlider(panel,"音乐",218,audioSettings.Music,v=>audioSettings.Music=v);
   AudioSlider(panel,"音效",280,audioSettings.Effects,v=>audioSettings.Effects=v);
   UButton(panel,"FocusMute",audioSettings.MuteUnfocused?"失焦静音：开":"失焦静音：关",28,350,330,44,()=>{audioSettings.MuteUnfocused=!audioSettings.MuteUnfocused;ScheduleAudioSave();ShowAudioSettings();},audioSettings.MuteUnfocused);
   UButton(panel,"Motion",audioSettings.ReducedMotion?"减少动效：开":"减少动效：关",382,350,330,44,()=>{audioSettings.ReducedMotion=!audioSettings.ReducedMotion;ApplyGalleryMotion();ScheduleAudioSave();ShowAudioSettings();},audioSettings.ReducedMotion);
   UButton(panel,"PreviewSound","试听确认音",28,424,330,44,()=>PlayUISound(1));
   UButton(panel,"SaveSettings","完成",382,424,330,44,CloseGalleryDialog,true);
   UText(panel,"AudioHint","音量为0即静音；切换到其他应用时默认静音。",28,487,684,28,15,uiDim);
  }
 }
}
