using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  void RunWorkshopSmoke(string dir){
   savePath=Path.Combine(dir,"workshop-save.json");profile=new Profile();profile.Shards[0]=3;profile.SkillPages[3]=3;Save();
   page="角色";selectedCard=0;catalog.OwnedOnly=true;ResetGalleryFilters();OpenGalleryCard(0);
   galleryScroll.verticalNormalizedPosition=.37f;float scroll=galleryScroll.verticalNormalizedPosition;
   NavigateGallery("养成");AssertSmoke(workshopRoot.gameObject.activeSelf&&!detailRoot.gameObject.activeSelf&&!galleryListRoot.gameObject.activeSelf,"Workshop did not isolate page");
   int material=profile.Materials;GalleryTestButton("WorkTrain").onClick.Invoke();GalleryTestButton("WorkResonance").onClick.Invoke();GalleryTestButton("WorkPolicy2").onClick.Invoke();Load();
   AssertSmoke(profile.Training[0]==1&&profile.Materials==material-30&&profile.Resonance[0]==1&&profile.Shards[0]==0&&profile.Policy[0]==2,"Training and policy UI persistence failed");
   profile.Materials=0;DrawWorkshop();AssertSmoke(!GalleryTestButton("WorkTrain").interactable&&!GalleryTestButton("WorkResonance").interactable,"Insufficient training resources enabled");
   NavigateGallery("装备");GalleryTestButton("Gem0").onClick.Invoke();AssertSmoke(profile.Items[0].gem&&profile.Gems==2,"Gem insert failed");GalleryTestButton("Gem0").onClick.Invoke();AssertSmoke(!profile.Items[0].gem&&profile.Gems==3,"Gem removal lost resources");
   GalleryTestButton("NextCharacter").onClick.Invoke();AssertSmoke(selectedCard==1,"Character cycle failed");
   string snapshot=JsonUtility.ToJson(profile);GalleryTestButton("Equip0Slot1").onClick.Invoke();AssertSmoke(dialogRoot!=null&&!GalleryTestButton("WorkshopBack").interactable,"Comparison modal failed input isolation");GalleryTestButton("CancelEquip").onClick.Invoke();AssertSmoke(JsonUtility.ToJson(profile)==snapshot,"Comparison changed live profile");
   GalleryTestButton("Equip0Slot1").onClick.Invoke();GalleryTestButton("ConfirmEquip").onClick.Invoke();Load();AssertSmoke(profile.Items[0].owner==1&&profile.Items[0].slot==1,"Cross-character equipment transfer failed");
   GalleryTestButton("Favorite0").onClick.Invoke();GalleryTestButton("BagKind").onClick.Invoke();GalleryTestButton("BagOption2").onClick.Invoke();AssertSmoke(workBody.GetComponentsInChildren<UI.Text>().Any(t=>t.name=="EmptyInventory"),"Empty bag filter missing");
   GalleryTestButton("BagKind").onClick.Invoke();GalleryTestButton("BagOption0").onClick.Invoke();
   GalleryTestButton("EquipmentTab1").onClick.Invoke();GalleryTestButton("SetWorkSkill3").onClick.Invoke();AssertSmoke(profile.CanUseSkill(Data.SkillIds[3])&&profile.SkillPages[3]==0,"Skill unlock failed");GalleryTestButton("SetWorkSkill3").onClick.Invoke();GalleryTestButton("SkillSlot1").onClick.Invoke();AssertSmoke(!GalleryTestButton("SetWorkSkill3").interactable,"Duplicate skill still enabled");GalleryTestButton("SetWorkSkill1").onClick.Invoke();Load();AssertSmoke(profile.Skill[1]==Data.SkillIds[3]&&profile.Skill2[1]==Data.SkillIds[1],"Two skill slots failed save");
   workScroll.verticalNormalizedPosition=.25f;float skillScroll=workScroll.verticalNormalizedPosition;NavigateGallery("养成");AssertSmoke(selectedCard==1,"Switching pages lost current character");NavigateGallery("装备");AssertSmoke(Math.Abs(workScroll.verticalNormalizedPosition-skillScroll)<.01,"Workshop lost page scroll");
   NavigateGallery("编队");GalleryTestButton("SelectPosition1").onClick.Invoke();GalleryTestButton("Reserve0").onClick.Invoke();GalleryTestButton("Captain1").onClick.Invoke();GalleryTestButton("SaveTeam0").onClick.Invoke();GalleryTestButton("Reserve7").onClick.Invoke();GalleryTestButton("LoadTeam0").onClick.Invoke();Load();AssertSmoke(profile.Team[1]==0&&profile.Team[0]==1&&profile.Leader==1&&profile.Team.Distinct().Count()==7,"Formation swap, leader or preset persistence failed");
   GalleryTestButton("WorkshopBack").onClick.Invoke();AssertSmoke(page=="角色"&&selectedCard==0&&galleryDetailOpen&&detailRoot.gameObject.activeSelf&&Math.Abs(galleryScroll.verticalNormalizedPosition-scroll)<.01,"Workshop return lost original gallery context");
   AssertSmoke(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==GalleryTestButton("BackToCards").gameObject,"Workshop return lost keyboard focus");
   NavigateGallery("养成");saveError="测试只读存档";DrawWorkshop();AssertSmoke(!GalleryTestButton("WorkPolicy0").interactable,"Read-only save allowed policy mutation");saveError="";
   NavigateGallery("装备");equipmentTab=0;DrawWorkshop();Canvas.ForceUpdateCanvases();
   AssertSmoke(workBody.GetComponentsInChildren<UI.Text>().Where(t=>t.name.StartsWith("Item")||t.name.StartsWith("EquipmentSlot")).All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Equipment paragraphs clipped");
   AssertSmoke(workshopRoot.GetComponentsInChildren<UI.RawImage>().All(i=>i.uvRect==new Rect(0,0,1,1)),"Workshop cropped portraits");
   File.WriteAllText(Path.Combine(dir,"workshop-smoke.json"),"{\"training\":true,\"formation\":true,\"equipment\":true,\"skills\":true,\"contextReturn\":true,\"saveRoundTrip\":true,\"modalIsolation\":true,\"renderVerified\":false}");
   NavigateGallery("角色");Save();
  }
 }
}
