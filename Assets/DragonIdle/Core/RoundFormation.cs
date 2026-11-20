using System;
using System.Linq;
namespace DragonIdle {
 public partial class Profile {
  // Optional extension to v4: old team/presets remain intact for future systems and old saves.
  public string[] RoundFormationIds;
  public int[] RoundFormation(){
   if(RoundFormationIds!=null&&RoundFormationIds.Length>0){
    var chosen=RoundFormationIds.Select(id=>string.IsNullOrEmpty(id)?-1:Data.Index(id)>=0?Data.Index(id):-2).ToArray();
    if(!ValidRoundFormation(chosen))throw new InvalidOperationException("九宫格阵容存档损坏");
    return chosen;
   }
   var migrated=Enumerable.Repeat(-1,RoundBoard.Cells).ToArray();
   int kept=0;
   for(int n=0;n<Team.Length&&kept<RoundBoard.PlayerLimit;n++)if(Team[n]>=0){migrated[n]=Team[n];kept++;}
   return migrated;
  }
  bool ValidRoundFormation(int[] grid){
   return grid.Length==RoundBoard.Cells&&grid.All(i=>i==-1||Owns(i))&&
    grid.Count(i=>i>=0)>=1&&grid.Count(i=>i>=0)<=RoundBoard.PlayerLimit&&
    grid.Where(i=>i>=0).Distinct().Count()==grid.Count(i=>i>=0);
  }
  public bool SetRoundMember(int position,int card){
   if(position<1||position>RoundBoard.Cells||!Owns(card))return false;
   var grid=RoundFormation();int slot=position-1,from=Array.IndexOf(grid,card);
   if(from>=0)grid[from]=grid[slot];
   grid[slot]=card;
   if(!ValidRoundFormation(grid))return false;
   StoreRoundFormation(grid);return true;
  }
  public bool RemoveRoundMember(int position){
   if(position<1||position>RoundBoard.Cells)return false;
   var grid=RoundFormation();if(grid[position-1]<0)return false;
   grid[position-1]=-1;if(!ValidRoundFormation(grid))return false;
   StoreRoundFormation(grid);return true;
  }
  void StoreRoundFormation(int[] grid){RoundFormationIds=grid.Select(i=>i<0?"":Data.Cards[i].Id).ToArray();}
  public RoundCombat NewRoundBattle(RoundDefinitions rules,RoundEncounter enemy,int seed){
   var player=RoundFormation().Select((i,n)=>new{index=i,pos=n+1}).Where(s=>s.index>=0).Select(s=>{
    var c=Data.Cards[s.index];
    return new RoundFighter(c.Id,0,s.pos,c.HP,c.Attack,c.Defense,.08);
   });
   var rivals=enemy.positions.Select((id,n)=>new{id,pos=n+1}).Where(s=>!string.IsNullOrEmpty(s.id)).Select(s=>{
    var c=Data.Card(s.id);
    return new RoundFighter(c.Id,1,s.pos,c.HP*enemy.scale,c.Attack*enemy.scale,c.Defense*enemy.scale,.08);
   });
   return new RoundCombat(player.Concat(rivals),rules,seed);
  }
 }
}
