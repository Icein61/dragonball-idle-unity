using System;
using System.Linq;
namespace DragonIdle {
 public partial class Profile {
  // Optional extension to v4: old team/presets remain intact for future systems and old saves.
  public string[] RoundFormationIds;
  public bool NeedsRoundRosterMigration {
   get {return RoundFormationIds!=null&&RoundFormationIds.Any(id=>Data.Index(id)>=0&&!Data.IsRoundCard(id));}
  }
  public void MigrateRoundRoster(){if(NeedsRoundRosterMigration)StoreRoundFormation(RoundFormation());}
  public int[] RoundFormation(){
   if(RoundFormationIds!=null&&RoundFormationIds.Length>0){
    var chosen=RoundFormationIds.Select(id=>string.IsNullOrEmpty(id)?-1:Data.Index(id)>=0?Data.Index(id):-2).ToArray();
    if(!ValidRoundFormation(chosen))throw new InvalidOperationException("九宫格阵容存档损坏");
    for(int i=0;i<chosen.Length;i++)
     if(chosen[i]>=0&&!Data.IsRoundCard(Data.Cards[chosen[i]].Id))chosen[i]=-1;
    return EnsureRoundMember(chosen);
   }
   var migrated=Enumerable.Repeat(-1,RoundBoard.Cells).ToArray();
   int kept=0;
   for(int n=0;n<Team.Length&&kept<RoundBoard.PlayerLimit;n++)if(Team[n]>=0&&Data.IsRoundCard(Data.Cards[Team[n]].Id)){migrated[n]=Team[n];kept++;}
   return EnsureRoundMember(migrated);
  }
  int[] EnsureRoundMember(int[] grid){
   if(grid.All(i=>i<0)){
    int starter=Data.RoundRoster.Select(Data.Index).Where(Owns).DefaultIfEmpty(-1).First();
    if(starter<0)throw new InvalidOperationException("没有可上阵的上线角色");
    grid[0]=starter;
   }
   return grid;
  }
  bool ValidRoundFormation(int[] grid){
   return grid.Length==RoundBoard.Cells&&grid.All(i=>i==-1||Owns(i))&&
    grid.Count(i=>i>=0)>=1&&grid.Count(i=>i>=0)<=RoundBoard.PlayerLimit&&
    grid.Where(i=>i>=0).Distinct().Count()==grid.Count(i=>i>=0);
  }
  public bool SetRoundMember(int position,int card){
   var grid=RoundFormation();
   return TryPlaceRoundMember(grid,position,card)&&TrySaveRoundFormation(grid);
  }
  public bool TryPlaceRoundMember(int[] draft,int position,int card){
   if(position<1||position>RoundBoard.Cells||!Owns(card)||!Data.IsRoundCard(Data.Cards[card].Id))return false;
   if(draft==null||draft.Length!=RoundBoard.Cells)return false;
   var next=(int[])draft.Clone();
   int slot=position-1,from=Array.IndexOf(next,card);
   if(from>=0)next[from]=next[slot];
   next[slot]=card;
   if(!ValidRoundFormation(next))return false;
   Array.Copy(next,draft,next.Length);
   return true;
  }
  public bool TrySaveRoundFormation(int[] draft){
   if(draft==null||!ValidRoundFormation(draft)||draft.Any(i=>i>=0&&!Data.IsRoundCard(Data.Cards[i].Id)))return false;
   StoreRoundFormation(draft);
   return true;
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
    return RoundProgression.Create(c,RoundProgression.LevelFromTraining(Training[s.index]),0,s.pos);
   });
   var rivals=enemy.positions.Select((id,n)=>new{id,pos=n+1}).Where(s=>!string.IsNullOrEmpty(s.id)).Select(s=>{
    var c=Data.Card(s.id);
    return RoundProgression.Create(c,1,1,s.pos,enemy.scale);
   });
   return new RoundCombat(player.Concat(rivals),rules,seed);
  }
 }
}
