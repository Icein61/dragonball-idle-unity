using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;

namespace DragonIdle {
 internal enum RoundStatusIcon { Training, Elite, DefenseUp, DefenseDown, Charge, TeamAttack, DodgeUp }

 internal sealed class RoundStatusHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  internal Action<PointerEventData> Enter;
  internal Action Exit;
  public void OnPointerEnter(PointerEventData pointer) => Enter?.Invoke(pointer);
  public void OnPointerExit(PointerEventData pointer) => Exit?.Invoke();
 }

 public partial class DemoGame {
  struct DisplayRoundStatus {
   internal RoundStatusIcon Icon;
   internal RoundSkillIcon SkillIcon;
   internal string Title,Description;
  }

  RectTransform roundStatusTooltip;

  IEnumerable<DisplayRoundStatus> DisplayRoundStatuses(RoundFighter fighter,RoundVitals vitals){
   foreach(var skill in RoundRules().OpeningSkills(fighter.CardId))
    yield return new DisplayRoundStatus{
     Icon=skill.name==RoundMotionArt.OpeningSkill?RoundStatusIcon.Training:RoundStatusIcon.Elite,
     SkillIcon=skill.icon,Title=skill.name,Description=FormatGallerySkillText(skill.text)
    };

   foreach(var aura in roundBattle.ActiveTeamAttackSkills(fighter,source=>VisibleRoundVitals(source).HP>0).GroupBy(skill=>skill.name)){
    var skill=aura.First();
    yield return new DisplayRoundStatus{Icon=RoundStatusIcon.TeamAttack,SkillIcon=skill.icon,
     Title=skill.name,Description=FormatGallerySkillText(skill.text)};
   }
   int round=roundBattle.Round;
   foreach(var phase in RoundRules().For(fighter.CardId).skills.Where(skill=>skill.trigger=="roundStart")){
    string current=round<phase.phaseRound?"当前攻击提升["+StatNumber(phase.value)+"]。":
     "当前攻击降低["+StatNumber(phase.lateAttackPenalty)+"]，防御提升["+StatNumber(phase.lateDefense)+"]。";
    yield return new DisplayRoundStatus{Icon=RoundStatusIcon.TeamAttack,SkillIcon=phase.icon,Title=phase.name,
     Description=FormatGallerySkillText(current+"\n"+phase.text)};
   }
   foreach(var group in vitals.AttackChanges.Where(effect=>effect.Source!=null&&round<effect.Expiry).GroupBy(effect=>effect.Source)){
    var status=StatStatus(RoundStatusIcon.TeamAttack,"攻击提升",group,round);
    status.SkillIcon=group.Key.icon;
    yield return status;
   }
   var dodge=vitals.DodgeChanges.Where(effect=>round<effect.Expiry).ToArray();
   if(dodge.Length>0)
    yield return StatStatus(RoundStatusIcon.DodgeUp,"闪避提升",dodge,round);

   foreach(var group in vitals.DefenseChanges.Where(effect=>round<effect.Expiry).GroupBy(effect=>effect.Fraction>0)
    .OrderByDescending(group=>group.Key)){
    string change=group.Key?"防御提升":"防御降低";
    yield return StatStatus(group.Key?RoundStatusIcon.DefenseUp:RoundStatusIcon.DefenseDown,change,group,round);
   }
   if(fighter.IsCharging)
    yield return new DisplayRoundStatus{
     Icon=RoundStatusIcon.Charge,SkillIcon=RoundRules().For(fighter.CardId).ultimateIcon,Title="蓄力",
     Description=FormatGallerySkillText("【蓄力】期间伤害减免["+StatNumber(RoundRules().For(fighter.CardId).techniques.ultimateChargeReduction)+"]，到原手位时释放技能。")
    };
  }

  static DisplayRoundStatus StatStatus(RoundStatusIcon icon,string name,IEnumerable<RoundFighter.StatChange> effects,int round){
   string total=StatNumber(Math.Abs(effects.Sum(effect=>effect.Fraction)));
   var entries=effects.Select(effect=>name+"["+StatNumber(Math.Abs(effect.Fraction))+"]，"+
    (effect.Expiry==int.MaxValue?"持续整场":"剩余["+(effect.Expiry-round)+"]回合"));
   return new DisplayRoundStatus{Icon=icon,Title=FormatGallerySkillText(name+" ["+total+"]"),
    Description=FormatGallerySkillText(string.Join("\n",entries))};
  }

  static string StatNumber(double fraction)=>(fraction*100).ToString("0.##");

  void DrawRoundStatuses(RectTransform frame,DisplayRoundStatus[] statuses,float width,float y,float size){
   const float gap=2;
   float span=size*statuses.Length+gap*(statuses.Length-1);
   var row=UNode(frame,"Buffs",(width-span)*.5f,y,span,size);
   for(int i=0;i<statuses.Length;i++){
    var status=statuses[i];
    var item=UNode(row,status.Icon.ToString(),i*(size+gap),0,size,size);
    if(status.SkillIcon!=null){
     var image=item.gameObject.AddComponent<UI.RawImage>();
     BindRoundSkillIcon(image,status.SkillIcon);
    }else if(status.Icon==RoundStatusIcon.DefenseUp||status.Icon==RoundStatusIcon.DefenseDown||status.Icon==RoundStatusIcon.DodgeUp){
     item.gameObject.AddComponent<UI.Image>().sprite=RoundStatusArt.Icon(status.Icon);
    }else continue;
    var hover=item.gameObject.AddComponent<RoundStatusHover>();
    hover.Enter=pointer=>ShowRoundStatusTooltip(status,pointer);
    hover.Exit=HideRoundStatusTooltip;
   }
  }

  void ShowRoundStatusTooltip(DisplayRoundStatus status,PointerEventData pointer){
   if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(roundPreviewRoot,pointer.position,
      pointer.enterEventCamera,out var local))return;
   HideRoundStatusTooltip();
   const float width=266;
   float x=Mathf.Clamp(local.x+13,0,roundPreviewRoot.rect.width-width);
   var panel=UPanel(roundPreviewRoot,"BuffTooltip",x,0,width,72,InkPaper);
   roundStatusTooltip=panel;
   UText(panel,"Name",status.Title,11,6,width-22,24,17,InkWarmText).supportRichText=true;
   var text=UText(panel,"Description",status.Description,11,32,width-22,30,14,InkText);
   text.supportRichText=true;
   float height=Mathf.Max(64,38+text.preferredHeight);
   panel.sizeDelta=new Vector2(width,height);
   text.rectTransform.sizeDelta=new Vector2(width-22,height-35);
   panel.anchoredPosition=new Vector2(x,-Mathf.Clamp(-local.y+12,0,roundPreviewRoot.rect.height-height));
  }

  void HideRoundStatusTooltip(){
   if(roundStatusTooltip==null)return;
   roundStatusTooltip.gameObject.SetActive(false);
   Destroy(roundStatusTooltip.gameObject);
   roundStatusTooltip=null;
  }
 }
}
