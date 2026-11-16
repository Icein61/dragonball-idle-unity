using System;
using System.Linq;
namespace DragonIdle {
 public sealed class CatalogFilter {
  public bool OwnedOnly=true;
  public string Search="",Rarity="",Role="",Tag="";
  public int Sort;
  public int[] Results(Profile p){
   string term=(Search??"").Trim();
   var q=Enumerable.Range(0,Data.Cards.Length).Where(i=>{
    var c=Data.Cards[i];
    return (!OwnedOnly||p.Owned[i])&&(Rarity==""||c.Rarity==Rarity)&&(Role==""||c.Keywords.Contains(Role))&&(Tag==""||c.HasTag(Tag))&&
     (c.Name+" "+c.Title+" "+c.Stage+" "+c.Form+" "+string.Join(" ",c.Tags)+" "+string.Join(" ",c.Keywords)).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0;
   });
   if(Sort==1)return q.OrderByDescending(i=>Array.IndexOf(Data.Rarities,Data.Cards[i].Rarity)).ThenBy(i=>i).ToArray();
   if(Sort==2)return q.OrderBy(i=>Data.Cards[i].Name,StringComparer.Ordinal).ThenBy(i=>Data.Cards[i].Title,StringComparer.Ordinal).ThenBy(i=>i).ToArray();
   if(Sort==3)return q.OrderByDescending(i=>p.Training[i]).ThenByDescending(i=>p.Resonance[i]).ThenBy(i=>i).ToArray();
   return q.OrderBy(i=>Array.IndexOf(p.Team,i)<0?99:Array.IndexOf(p.Team,i)).ThenByDescending(i=>p.Owned[i]).ThenBy(i=>i).ToArray();
  }
  public static int Reconcile(int selected,int[] results){return results.Contains(selected)?selected:results.Length>0?results[0]:-1;}
 }
 public partial class Profile {
  // Shared with CreateTeam: UI and battle use the same equipment, skill and passive math.
  public Unit InspectCharacter(int i,bool includeCaptain=false){
   Validate();if(i<0||i>=Data.Cards.Length)throw new ArgumentOutOfRangeException("i");
   if(!Owned[i])return Data.Make(Data.Cards[i].Id,0,0,0,0,"",1);
   return BuildCharacter(i,0,Math.Max(0,Array.IndexOf(Team,i)),includeCaptain&&Team.Contains(i));
  }
  Unit BuildCharacter(int i,int side,int slot,bool includeCaptain){
   string replacement=Skill[i]=="pierce"||Skill2[i]=="pierce"?"pierce":"";
   var u=Data.Make(Data.Cards[i].Id,side,slot,Training[i],Resonance[i],replacement,1);
   if(Skill[i]=="focus"||Skill2[i]=="focus")u.Crit+=.035;
   var leader=Data.Cards[Team[Leader]];
   if(includeCaptain&&leader.Captain.Applies(Data.Cards[i])){
    u.LeaderAttack=leader.Captain.Attack;u.LeaderHealth=leader.Captain.Health;u.LeaderDefense=leader.Captain.Defense;
    u.MaxHP*=1+u.LeaderHealth;u.HP=u.MaxHP;u.Shield*=1+u.LeaderHealth;
   }
   if(Skill[i]=="guard"||Skill2[i]=="guard")u.Shield+=u.MaxHP*.04;
   u.UltimatePolicy=Policy[i];
   foreach(var item in Items.Where(x=>x.owner==i)){
    if(item.kind==1)u.Add("defense",item.Bonus,0,item.id);else u.Add("attack",item.Bonus,0,item.id);
    if(item.gem)u.Add("attack",.008,0,item.id+":gem");
   }
   return u;
  }
 }
}
