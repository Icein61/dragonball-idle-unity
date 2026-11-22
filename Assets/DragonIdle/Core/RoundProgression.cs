using System;

namespace DragonIdle {
 internal static class RoundProgression {
  internal const int MaxLevel=100;
  static readonly double[] TierEdges={.70,.80,1.00,1.20,1.50};
  static readonly double[] GrowthAtCap={.25,.28,.31,.34};

  internal static int LevelFromTraining(int training){return Math.Min(MaxLevel-1,Math.Max(0,training))+1;}

  internal static double PersonalRoll(string cardId,string stat){
   // FNV-1a over stable ASCII CardId and field names. This also works on the older core test runtime.
   uint hash=2166136261;
   foreach(char c in cardId+"/"+stat)hash=unchecked((hash^c)*16777619);
   return .8+.4*((hash+.5)/(uint.MaxValue+1.0));
  }

  internal static double Stat(string cardId,string stat,double anchor,string rarity,string potential,int level){
   int tier=Rank(rarity),growth=Rank(potential);
   // Reserve the ends of each interval so even the strongest lower-rarity potential stays below the next tier.
   double position=.2+.6*(PersonalRoll(cardId,stat)-.8)/.4;
   double basis=anchor*(TierEdges[tier]+(TierEdges[tier+1]-TierEdges[tier])*position);
   double t=(Math.Max(1,Math.Min(MaxLevel,level))-1)/(double)(MaxLevel-1);
   // The linear term guarantees a positive increment near the cap.
   double curve=.42*t+.58*(1-Math.Exp(-2.5*t))/(1-Math.Exp(-2.5));
   return basis*(1+GrowthAtCap[growth]*curve);
  }

  static int Rank(string grade){
   if(grade==null||grade.Length!=1||"CBAS".IndexOf(grade[0])<0)throw new ArgumentException("无效属性档位");
   return "CBAS".IndexOf(grade[0]);
  }

  internal static RoundFighter Create(Card card,int level,int side,int position,double scale=1){
   RoundSheetCard metadata;
   bool authored=RoundSheetCatalog.Cards.TryGetValue(card.Id,out metadata);
   level=Math.Max(1,Math.Min(MaxLevel,level));
   var fighter=new RoundFighter(card.Id,side,position,
    Stat(card.Id,"hp",940,card.Rarity,authored?metadata.HpPotential:"B",level)*scale,
    Stat(card.Id,"attack",170,card.Rarity,authored?metadata.AttackPotential:"B",level)*scale,
    Stat(card.Id,"defense",46,card.Rarity,authored?metadata.DefensePotential:"B",level)*scale,.10,level);
   fighter.Tenacity=card.Type==CharacterType.Defense?.05:0;
   fighter.Dodge=card.Type==CharacterType.Defense?0:.05;
   fighter.Accuracy=card.Type==CharacterType.Output?.10:card.Type==CharacterType.Support?.05:0;
   fighter.Qi=card.Type==CharacterType.Support?.10:card.Type==CharacterType.Defense?.05:0;
   return fighter;
  }
 }
}
