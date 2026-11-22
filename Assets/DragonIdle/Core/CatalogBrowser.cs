using System;
using System.Linq;
namespace DragonIdle {
 public sealed class CatalogFilter {
  public bool OwnedOnly=true;
  public bool RoundFormat;
  public string Search="",Rarity="",Role="",Tag="";
  public int Sort;
  public int[] Results(Profile p){
   string term=(Search??"").Trim();
   var q=Enumerable.Range(0,Data.Cards.Length).Where(i=>{
    var c=Data.Cards[i];
    string identity=c.Name+" "+c.Title+" "+c.Stage+" "+c.Form+" "+string.Join(" ",c.Tags);
    if(!RoundFormat)identity+=" "+string.Join(" ",c.Keywords);
    return (!RoundFormat||Data.IsRoundCard(c.Id))&&(!OwnedOnly||p.Owned[i])&&(Rarity==""||c.Rarity==Rarity)&&(RoundFormat||Role==""||c.Keywords.Contains(Role))&&
     (Tag==""||c.HasTag(Tag))&&identity.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0;
   });
   var pinned=q.OrderByDescending(p.IsFavoriteCard);
   if(Sort==1)return pinned.ThenByDescending(i=>Array.IndexOf(Data.Rarities,Data.Cards[i].Rarity)).ThenBy(i=>i).ToArray();
   if(Sort==2)return pinned.ThenBy(i=>Data.Cards[i].Name,StringComparer.Ordinal).ThenBy(i=>Data.Cards[i].Title,StringComparer.Ordinal).ThenBy(i=>i).ToArray();
   if(RoundFormat&&Sort==4)return pinned.ThenBy(i=>Data.Cards[i].CatalogNumber).ThenBy(i=>i).ToArray();
   if(Sort==3)return RoundFormat?pinned.ThenBy(i=>Data.Cards[i].Type).ThenBy(i=>i).ToArray():pinned.ThenByDescending(i=>p.Training[i]).ThenByDescending(i=>p.Resonance[i]).ThenBy(i=>i).ToArray();
   var team=RoundFormat?p.RoundFormation():p.Team;
   return pinned.ThenBy(i=>Array.IndexOf(team,i)<0?99:Array.IndexOf(team,i)).ThenByDescending(i=>p.Owned[i]).ThenBy(i=>i).ToArray();
  }
  public static int Reconcile(int selected,int[] results){return results.Contains(selected)?selected:results.Length>0?results[0]:-1;}
 }
 public partial class Profile {
  public string[] FavoriteCardIds=Array.Empty<string>();
  public bool IsFavoriteCard(int index){
   return index>=0&&index<Data.Cards.Length&&FavoriteCardIds!=null&&FavoriteCardIds.Contains(Data.Cards[index].Id);
  }
  public void ToggleFavoriteCard(int index){
   if(index<0||index>=Data.Cards.Length)throw new ArgumentOutOfRangeException("index");
   var id=Data.Cards[index].Id;
   var favorites=FavoriteCardIds??Array.Empty<string>();
   FavoriteCardIds=IsFavoriteCard(index)?favorites.Where(card=>card!=id).ToArray():favorites.Concat(new[]{id}).ToArray();
  }
  // Shared with CreateTeam: UI and battle use the same equipment, skill and passive math.
  public Unit InspectCharacter(int i,bool includeCaptain=false){
   Validate();if(i<0||i>=Data.Cards.Length)throw new ArgumentOutOfRangeException("i");
   if(!Owned[i])return Data.Make(Data.Cards[i].Id,0,0,0,0,"",1);
   return BuildCharacter(i,0,Math.Max(0,Array.IndexOf(Team,i)),includeCaptain&&Team.Contains(i));
  }
  public int CharacterPower(int i){
   Validate();if(i<0||i>=Data.Cards.Length)throw new ArgumentOutOfRangeException("i");
   var unit=Owned[i]?BuildCharacter(i,0,0,false,false):Data.Make(Data.Cards[i].Id,0,0,0,0,"",1);
   // Effective personal stats only. The asymptote is a display budget, never a combat multiplier.
   double pressure=unit.AttackValue*unit.SpeedValue/100*(1+.5*unit.Crit);
   double endurance=.065*unit.MaxHP+.64*unit.DefenseValue+.04*unit.Shield;
   double raw=Math.Max(0,pressure+endurance);
   return Math.Min(1499,(int)Math.Round(1500*raw/(400+raw),MidpointRounding.AwayFromZero));
  }
  Unit BuildCharacter(int i,int side,int slot,bool includeCaptain,bool includeTeamDesign=true){
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
   if(includeTeamDesign&&Team.Contains(i))Data.ApplyTeamDesign(u,Team.Where(n=>n>=0).Select(n=>Data.Cards[n].Id));
   foreach(var item in Items.Where(x=>x.owner==i)){
    if(item.kind==1)u.Add("defense",item.Bonus,0,item.id);else u.Add("attack",item.Bonus,0,item.id);
    if(item.gem)u.Add("attack",.008,0,item.id+":gem");
   }
   return u;
  }
 }
}
