using System;

namespace DragonIdle {
 public partial class DemoGame {
  enum PageRoute {
   None,
   Home,
   Loading,
   Characters,
   Formation,
   RoundBattle,
   BattleDemo,
   LegacyChallenge,
   LegacyTraining,
   LegacyEquipment,
   LegacySummon,
   LegacyHelp
  }

  static readonly PageRoute[] FeatureRoutes={PageRoute.Characters,PageRoute.Formation,PageRoute.BattleDemo};

  static bool IsShellPage(PageRoute route)=>route==PageRoute.Home||route==PageRoute.Loading;
  static bool IsModernPage(PageRoute route)=>
   route==PageRoute.Characters||route==PageRoute.Formation||route==PageRoute.RoundBattle||IsShellPage(route);

  static string NavName(PageRoute route)=>"Nav"+route;
  static string HomeNavName(PageRoute route)=>"HomeNav"+route;

  static string RouteLabel(PageRoute route){
   switch(route){
    case PageRoute.Characters:return "角色";
    case PageRoute.Formation:return "编队";
    case PageRoute.BattleDemo:return "战斗 Demo";
    case PageRoute.RoundBattle:return "回合试阵";
    default:throw new ArgumentOutOfRangeException(nameof(route),route,"Route has no feature label.");
   }
  }
 }
}
