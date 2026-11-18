using System;
using System.IO;
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
   if(kind==0){var ranks=Data.Rarities.Where(rank=>Data.Cards.Any(card=>card.Rarity==rank));values=new[]{""}.Concat(ranks).ToArray();labels=new[]{"全部稀有度"}.Concat(ranks).ToArray();}
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
    },selected:active);
   }
  }
  void ShowFormationPicker(int i){
   var c=Data.Cards[i];int existing=Array.IndexOf(profile.Team,i);
   var panel=NewGalleryDialog("选择 "+c.Name+" 的阵位",existing>=0?"角色已在阵位"+(existing+1)+"；选择其他位置会交换两位角色。队长身份跟随阵位。":"选择位置以替换当前成员。被替换的角色回到候补，装备和成长保留。");
   for(int s=0;s<Data.TeamSlots;s++){
    int slot=s,index=profile.Team[s];
    string value=FormationSlotLabel(s)+(s==profile.Leader?" · 队长位":"")+"\n"+(index<0?"空位":Data.Cards[index].Name);
    UButton(panel,"Slot"+s,value,26+s%Data.FrontSlots*232,155+s/Data.FrontSlots*91,218,73,()=>{
     if(Change(()=>{if(!profile.SetMember(slot,i))throw new InvalidOperationException("角色不可上阵");battle=null;})){
      selectedSlot=slot;CloseGalleryDialog();GalleryNotice(c.Name+" 已进入 "+FormationSlotLabel(slot));RefreshGallery(false);
     }else GalleryNotice(notice,false);
    },enabled:saveError=="",selected:s==existing);
   }
  }
  void ShowAcquisition(int i){
   var c=Data.Cards[i];var panel=NewGalleryDialog("获取 · "+c.Name,c.Title+" / "+c.Stage+" / "+c.Rarity+"。重复获得将转为本张卡的专属成长资源。");
   float y=160;
   if(Data.InSaiyanPool(c)){UButton(panel,"GoSaiyanPool","前往赛亚人篇召唤 · 独立保底",26,y,682,46,()=>{saiyanPool=true;NavigateGallery("召唤");});y+=66;}
   if(Data.InClassicPool(c)){UButton(panel,"GoLegacyPool","前往基础常驻召唤 · 仅赛亚人篇",26,y,682,46,()=>{saiyanPool=false;NavigateGallery("召唤");});y+=66;}
   UText(panel,"PityInfo","每抽 50 晶石。概率和保底进度可在召唤页查看。",26,y+5,682,62,16,uiDim);
  }
  UI.Slider AudioSlider(Transform parent,string title,float y,float value,Action<float> update){
   var label=UText(parent,title+"Label",title+"  "+Mathf.RoundToInt(value*100)+"%",28,y,220,34,19,uiPaper);
   var track=UPanel(parent,title+"Slider",260,y+12,440,8,InkBorder,true);
   var handle=UPanel(track,"Handle",0,0,18,26,InkDark,true);handle.pivot=new Vector2(.5f,.5f);
   var slider=track.gameObject.AddComponent<UI.Slider>();slider.direction=UI.Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UI.Image>();slider.SetValueWithoutNotify(value);
   slider.onValueChanged.AddListener(v=>{update(v);label.text=title+"  "+Mathf.RoundToInt(v*100)+"%";ScheduleAudioSave();});return slider;
  }
  void ApplyGalleryMotion(){foreach(var b in galleryRoot.GetComponentsInChildren<UI.Button>()){var c=b.colors;c.fadeDuration=audioSettings.ReducedMotion?0:.12f;b.colors=c;}}
  void ShowAudioSettings(){
   var panel=NewGalleryDialog("声音与显示","设置自动保存。音量调为 0 即静音。");
   AudioSlider(panel,"总音量",156,audioSettings.Master,v=>audioSettings.Master=v);
   AudioSlider(panel,"音乐",218,audioSettings.Music,v=>audioSettings.Music=v);
   AudioSlider(panel,"音效",280,audioSettings.Effects,v=>audioSettings.Effects=v);
   UButton(panel,"FocusMute",audioSettings.MuteUnfocused?"失焦静音：开":"失焦静音：关",28,350,330,44,()=>{audioSettings.MuteUnfocused=!audioSettings.MuteUnfocused;ScheduleAudioSave();ShowAudioSettings();},selected:audioSettings.MuteUnfocused);
   UButton(panel,"Motion",audioSettings.ReducedMotion?"减少动效：开":"减少动效：关",382,350,330,44,()=>{audioSettings.ReducedMotion=!audioSettings.ReducedMotion;ApplyGalleryMotion();ScheduleAudioSave();ShowAudioSettings();},selected:audioSettings.ReducedMotion);
   UButton(panel,"PreviewSound","试听确认音",28,424,330,44,()=>PlayUISound(1));
   UButton(panel,"SaveSettings","完成",382,424,330,44,CloseGalleryDialog,true);
   UButton(panel,"OpenSaveFolder","打开存档目录",28,478,330,40,()=>Application.OpenURL(new Uri(Path.GetDirectoryName(savePath)).AbsoluteUri));
  }
 }
}
