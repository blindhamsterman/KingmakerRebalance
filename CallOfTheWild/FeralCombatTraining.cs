﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Enums;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace CallOfTheWild
{
    public class FeralCombatTraining
    {
        static LibraryScriptableObject library => Main.library;
        static internal BlueprintFeatureSelection feral_combat_training;
        static internal Dictionary<WeaponCategory, BlueprintUnitFact> natural_weapon_type_fact_map = new Dictionary<WeaponCategory, BlueprintUnitFact>();

        class NaturalWeapoonEntry
        {
            public readonly string name;
            public readonly WeaponCategory category;

            public NaturalWeapoonEntry(string display_name, WeaponCategory weapon_category)
            {
                name = display_name;
                category = weapon_category;
            }
        }

        static readonly NaturalWeapoonEntry[] natural_weapon_entries = { new NaturalWeapoonEntry("Gore", WeaponCategory.Gore),
                                                            new NaturalWeapoonEntry("Claw", WeaponCategory.Claw),
                                                            new NaturalWeapoonEntry("Bite", WeaponCategory.Bite),
                                                            new NaturalWeapoonEntry("Slam", WeaponCategory.OtherNaturalWeapons)
                                                          };

        static internal void load()
        {
            var improved_unarmed_strike = library.Get<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167");
            var weapon_focus = library.Get<BlueprintParametrizedFeature>("1e1f627d26ad36f43bbd26cc2bf8ac7e");
            feral_combat_training = Helpers.CreateFeatureSelection("FeralCombatTrainingSelection",
                                                                   "Feral Combat Training",
                                                                   "Choose one of your natural weapons. While using the selected natural weapon, you can apply the effects of feats that have Improved Unarmed Strike as a prerequisite.\n"
                                                                   + "Special: If you are a monk, you can use the selected natural weapon with your flurry of blows class feature.",
                                                                   "",
                                                                   LoadIcons.Image2Sprite.Create(@"FeatIcons/Icon_Feral_Combat.png"),
                                                                   FeatureGroup.CombatFeat,
                                                                   Helpers.PrerequisiteFeature(improved_unarmed_strike)
                                                                   );
            feral_combat_training.HideInCharacterSheetAndLevelUp = true;

            foreach (var natural_weapon_entry in natural_weapon_entries)
            {
                feral_combat_training.AddComponent(Common.createPrerequisiteParametrizedFeatureWeapon(weapon_focus, natural_weapon_entry.category, any: true));
                var feral_combat_entry = Helpers.CreateFeature("FeralCombat" + natural_weapon_entry.category.ToString() + "Feature",
                                                               feral_combat_training.Name + $" ({natural_weapon_entry.name})",
                                                               feral_combat_training.Description,
                                                               "",
                                                               feral_combat_training.Icon,
                                                               FeatureGroup.CombatFeat,
                                                               Helpers.PrerequisiteFeature(improved_unarmed_strike),
                                                               Common.createPrerequisiteParametrizedFeatureWeapon(weapon_focus, natural_weapon_entry.category, any: true)
                                                               );
                natural_weapon_type_fact_map.Add(natural_weapon_entry.category, feral_combat_entry);
                feral_combat_training.AllFeatures = feral_combat_training.AllFeatures.AddToArray(feral_combat_entry);
                feral_combat_entry.Groups = feral_combat_entry.Groups.AddToArray(FeatureGroup.Feat);
            }

            library.AddCombatFeats(feral_combat_training);
            fixAbilities();
        }


        public static bool checkHasFeralCombat(UnitEntityData unit, ItemEntityWeapon weapon)
        {
            if (weapon == null || unit == null)
            {
                return false;
            }
            if (weapon.Blueprint.Category == WeaponCategory.UnarmedStrike)
            {
                return true;
            }
            if (!natural_weapon_type_fact_map.Keys.Contains(weapon.Blueprint.Category))
            {
                return false;
            }
            return unit.Descriptor.Progression.Features.HasFact(natural_weapon_type_fact_map[weapon.Blueprint.Category]);
        }


        static void fixAbilities()
        {
            BlueprintFeature[] flurry = new BlueprintFeature[] { library.Get<BlueprintFeature>("fd99770e6bd240a4aab70f7af103e56a"), //lvl1
                                                                 library.Get<BlueprintFeature>("a34b8a9fcc9024b42bacfd5e6b614bfa") //lvl11
                                                                };
            foreach (var b in flurry)
            {
                foreach (var c in b.GetComponents<MonkNoArmorAndMonkWeaponFeatureUnlock>().ToArray())
                {
                    b.ReplaceComponent(c, MonkNoArmorAndMonkWeaponOrFeralCombatFeatureUnlock.fromMonkNoArmorAndMonkWeaponFeatureUnlock(c));
                }
            }

            BlueprintBuff[] dmg_boni = new BlueprintBuff[] {library.Get<BlueprintBuff>("cc03b87fd3a797f4e953773db424cee7"), //dragon style
                                                            library.Get<BlueprintBuff>("8709a00782de26d4a8524732879000fa") //dragon ferocity
                                                           };
            foreach (var b in dmg_boni)
            {
                foreach (var c in b.GetComponents<AdditionalStatBonusOnAttackDamage>().ToArray())
                {
                    b.ReplaceComponent(c, AdditionalStatBonusOnAttackDamageOrFeralCombat.fromAdditionalStatBonusOnAttackDamage(c));
                }
            }

            BlueprintBuff[] attack_triggers = new BlueprintBuff[] {library.Get<BlueprintBuff>("8709a00782de26d4a8524732879000fa"), //dragon ferocity
                                                                   library.Get<BlueprintBuff>("d9eaeba5690a7704da8bbf626456a50e"), //stunning fist buff
                                                                   library.Get<BlueprintBuff>("696b29374599d4141be64e46a91bd09b"), //stunning fist fatigue buff
                                                                   library.Get<BlueprintBuff>("4d7da6df5cb3b3940a9d96311a2dc311") //stunning fist sickened buff
                                                                  };
            foreach (var b in attack_triggers)
            {
                foreach (var c in b.GetComponents<AddInitiatorAttackWithWeaponTrigger>().ToArray())
                {
                    b.ReplaceComponent(c, AddInitiatorAttackWithWeaponTriggerOrFeralTraining.fromAddInitiatorAttackWithWeaponTrigger(c));
                }
            }

            BlueprintAbility[] main_hand_chackers = new BlueprintAbility[] {library.Get<BlueprintAbility>("732ae7773baf15447a6737ae6547fc1e"), //stunning fist
                                                                            library.Get<BlueprintAbility>("32f92fea1ab81c843a436a49f522bfa1"), //stunning fist fatigue
                                                                            library.Get<BlueprintAbility>("c81906c75821cbe4c897fa11bdaeee01"), //stunning fist sickened
                                                                            library.Get<BlueprintAbility>("957a10e303269324dbf1a70513f37559") //crushing blow
                                                                           };
            foreach (var m in main_hand_chackers)
            {
                foreach (var c in m.GetComponents<AbilityCasterMainWeaponCheck>().ToArray())
                {
                    m.ReplaceComponent(c, AbilityCasterMainWeaponCheckOrFeralCombat.fromAbilityCasterMainWeaponCheck(c));
                }
            }
        }

        [AllowMultipleComponents]
        public class AddInitiatorAttackWithWeaponTriggerOrFeralTraining : GameLogicComponent, IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookHandler<RuleAttackWithWeaponResolve>, IRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookSubscriber, IRulebookHandler<RuleAttackWithWeaponResolve>
        {
            [HideIf("CriticalHit")]
            public bool OnlyHit = true;
            public bool WaitForAttackResolve;
            public bool OnlyOnFullAttack;
            public bool OnlyOnFirstAttack;
            public bool OnlyOnFirstHit;
            public bool CriticalHit;
            public bool OnlySneakAttack;
            public BlueprintWeaponType WeaponType;
            public bool CheckWeaponCategory;
            [ShowIf("CheckWeaponCategory")]
            public WeaponCategory Category;
            [ShowIf("CheckWeaponRangeType")]
            public AttackTypeAttackBonus.WeaponRangeType RangeType;
            public bool ActionsOnInitiator;
            [Tooltip("For melee attacks only")]
            public bool ReduceHPToZero;
            [ShowIf("CheckDistance")]
            public Feet DistanceLessEqual;
            public bool DuelistWeapon;
            public ActionList Action;
            private bool m_HadHit;

            public static AddInitiatorAttackWithWeaponTriggerOrFeralTraining fromAddInitiatorAttackWithWeaponTrigger(AddInitiatorAttackWithWeaponTrigger prototype)
            {
                var a = Helpers.Create<AddInitiatorAttackWithWeaponTriggerOrFeralTraining>();
                a.OnlyHit = prototype.OnlyHit;
                a.WaitForAttackResolve = prototype.WaitForAttackResolve;
                a.OnlyOnFullAttack = prototype.OnlyOnFullAttack;
                a.OnlyOnFirstAttack = prototype.OnlyOnFirstAttack;
                a.OnlyOnFirstHit = prototype.OnlyOnFirstHit;
                a.CriticalHit = prototype.CriticalHit;
                a.OnlySneakAttack = prototype.OnlySneakAttack;
                a.WeaponType = prototype.WeaponType;
                a.Category = prototype.Category;
                a.CheckWeaponCategory = prototype.CheckWeaponCategory;
                a.RangeType = prototype.RangeType;
                a.ActionsOnInitiator = prototype.ActionsOnInitiator;
                a.ReduceHPToZero = prototype.ReduceHPToZero;
                a.DistanceLessEqual = prototype.DistanceLessEqual;
                a.DuelistWeapon = prototype.DuelistWeapon;
                a.Action = prototype.Action;
                return a;
            }

            public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
            {
            }

            public void OnEventDidTrigger(RuleAttackWithWeapon evt)
            {
                if (this.WaitForAttackResolve)
                    return;
                this.TryRunActions(evt);
            }

            public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt)
            {
            }

            public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
            {
                if (!this.WaitForAttackResolve)
                    return;
                this.TryRunActions(evt.AttackWithWeapon);
            }

            private void TryRunActions(RuleAttackWithWeapon rule)
            {
                if (!this.CheckCondition(rule))
                    return;
                if (!this.ActionsOnInitiator)
                {
                    using (new ContextAttackData(rule.AttackRoll, (Projectile)null))
                        (this.Fact as IFactContextOwner)?.RunActionInContext(this.Action, (TargetWrapper)rule.Target);
                }
                else
                {
                    using (new ContextAttackData(rule.AttackRoll, (Projectile)null))
                        (this.Fact as IFactContextOwner)?.RunActionInContext(this.Action, (TargetWrapper)rule.Initiator);
                }
            }

            private bool CheckCondition(RuleAttackWithWeapon evt)
            {
                ItemEntity owner = (this.Fact as ItemEnchantment)?.Owner;
                var unit = evt.Initiator;
                if (owner != null && owner != evt.Weapon || this.OnlyHit && !evt.AttackRoll.IsHit 
                    || (((bool)(this.WeaponType) && this.WeaponType != evt.Weapon.Blueprint.Type || this.CheckWeaponCategory && this.Category != evt.Weapon.Blueprint.Category) && !checkHasFeralCombat(unit, evt.Weapon))
                    || (this.CriticalHit && (!evt.AttackRoll.IsCriticalConfirmed || evt.AttackRoll.FortificationNegatesCriticalHit) || (this.OnlyOnFullAttack && !evt.IsFullAttack || this.OnlyOnFirstAttack && !evt.IsFirstAttack)) 
                    || (this.OnlyOnFirstHit && !evt.IsFullAttack || this.OnlyOnFirstHit && !evt.AttackRoll.IsHit || this.OnlyOnFirstHit && !evt.IsFirstAttack && this.m_HadHit)
                    )
                    return false;
                if (this.ReduceHPToZero)
                {
                    if (evt.MeleeDamage == null || evt.MeleeDamage.IsFake || evt.Target.HPLeft > 0)
                        return false;
                    return evt.Target.HPLeft + evt.MeleeDamage.Damage > 0;
                }
                if (this.OnlyOnFirstHit && evt.IsFirstAttack)
                    this.m_HadHit = false;
                if (this.OnlyOnFirstHit && evt.AttackRoll.IsHit)
                    this.m_HadHit = true;
                if (this.OnlySneakAttack && (!evt.AttackRoll.IsSneakAttack || evt.AttackRoll.FortificationNegatesSneakAttack))
                    return false;
                bool flag = evt.Weapon.Blueprint.Category.HasSubCategory(WeaponSubCategory.Light) || evt.Weapon.Blueprint.Category.HasSubCategory(WeaponSubCategory.OneHandedPiercing) || (bool)evt.Initiator.Descriptor.State.Features.DuelingMastery && evt.Weapon.Blueprint.Category == WeaponCategory.DuelingSword || evt.Initiator.Descriptor.Ensure<DamageGracePart>().HasEntry(evt.Weapon.Blueprint.Category);
                return !this.DuelistWeapon || flag;
            }
        }



        [ComponentName("Add feature if owner has no armor or shield")]
        [AllowedOn(typeof(BlueprintUnitFact))]
        [AllowMultipleComponents]
        public class MonkNoArmorAndMonkWeaponOrFeralCombatFeatureUnlock : OwnedGameLogicComponent<UnitDescriptor>, IUnitActiveEquipmentSetHandler, IUnitEquipmentHandler, IGlobalSubscriber
        {
            public BlueprintUnitFact NewFact;
            [JsonProperty]
            private Fact m_AppliedFact;

            public static MonkNoArmorAndMonkWeaponOrFeralCombatFeatureUnlock fromMonkNoArmorAndMonkWeaponFeatureUnlock(MonkNoArmorAndMonkWeaponFeatureUnlock prototype)
            {
                var m = Helpers.Create<MonkNoArmorAndMonkWeaponOrFeralCombatFeatureUnlock>();
                m.NewFact = prototype.NewFact;
                return m;
            }

            public override void OnFactActivate()
            {
                this.CheckEligibility();
            }

            public override void OnFactDeactivate()
            {
                this.RemoveFact();
            }

            public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
            {
                this.CheckEligibility();
            }

            public void CheckEligibility()
            {
                if (!this.Owner.Body.SecondaryHand.HasShield
                    && (!this.Owner.Body.Armor.HasArmor || !this.Owner.Body.Armor.Armor.Blueprint.IsArmor)
                    && (this.Owner.Body.PrimaryHand.Weapon.Blueprint.IsMonk || checkHasFeralCombat(this.Owner.Unit, this.Owner.Body.PrimaryHand.Weapon))
                    )
                {
                    this.AddFact();
                }
                else
                {
                    this.RemoveFact();
                }
            }

            public void AddFact()
            {
                if (this.m_AppliedFact != null)
                    return;
                this.m_AppliedFact = this.Owner.AddFact(this.NewFact, (MechanicsContext)null, (FeatureParam)null);
            }

            public void RemoveFact()
            {
                if (this.m_AppliedFact == null)
                    return;
                this.Owner.RemoveFact(this.m_AppliedFact);
                this.m_AppliedFact = (Fact)null;
            }

            public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
            {
                if (slot.Owner != this.Owner)
                    return;
                this.CheckEligibility();
            }

            public new void OnTurnOn()
            {
                this.CheckEligibility();
            }
        }


        [AllowMultipleComponents]
        public class AdditionalStatBonusOnAttackDamageOrFeralCombat : GameLogicComponent, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber
        {
            public ConditionEnum FullAttack;
            public ConditionEnum FirstAttack;
            public bool CheckCategory;
            [ShowIf("CheckCategory")]
            public WeaponCategory Category;
            public bool CheckTwoHanded;
            public float Bonus;


            static public AdditionalStatBonusOnAttackDamageOrFeralCombat fromAdditionalStatBonusOnAttackDamage(AdditionalStatBonusOnAttackDamage prototype)
            {
                var a = Helpers.Create<AdditionalStatBonusOnAttackDamageOrFeralCombat>();
                a.FullAttack = prototype.FullAttack;
                a.FirstAttack = prototype.FirstAttack;
                a.CheckCategory = prototype.CheckCategory;
                a.Category = prototype.Category;
                a.CheckTwoHanded = prototype.CheckTwoHanded;
                a.Bonus = prototype.Bonus;
                return a;
            }

            public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
            {
                if (!this.CheckConditions(evt))
                    return;
                evt.AdditionalDamageBonusStatMultiplier += this.Bonus;
            }

            public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
            {
            }

            public bool CheckConditions(RuleCalculateWeaponStats evt)
            {
                if (this.FullAttack == ConditionEnum.Only && (evt.AttackWithWeapon == null || !evt.AttackWithWeapon.IsFullAttack)
                    || this.FullAttack == ConditionEnum.Not && (evt.AttackWithWeapon == null || evt.AttackWithWeapon.IsFullAttack)
                    || (this.FirstAttack == ConditionEnum.Only && (evt.AttackWithWeapon == null || !evt.AttackWithWeapon.IsFirstAttack)
                    || this.FirstAttack == ConditionEnum.Not && (evt.AttackWithWeapon == null || evt.AttackWithWeapon.IsFirstAttack)))
                {
                    return false;
                }
                if (this.CheckCategory)
                {
                    WeaponCategory? category = evt.Weapon?.Blueprint.Category;
                    if (((category.GetValueOrDefault() != this.Category ? 1 : (!category.HasValue ? 1 : 0)) != 0)
                        && !checkHasFeralCombat(evt.Initiator, evt.Weapon))
                    {
                        return false;
                    }
                }
                return !this.CheckTwoHanded || evt.Weapon != null && evt.Weapon.HoldInTwoHands && AttackTypeAttackBonus.CheckRangeType(evt.Weapon.Blueprint, AttackTypeAttackBonus.WeaponRangeType.Melee);
            }
        }


        [AllowedOn(typeof(BlueprintAbility))]
        [AllowMultipleComponents]
        public class AbilityCasterMainWeaponCheckOrFeralCombat : BlueprintComponent, IAbilityCasterChecker
        {
            public WeaponCategory[] Category;


            public static AbilityCasterMainWeaponCheckOrFeralCombat fromAbilityCasterMainWeaponCheck(AbilityCasterMainWeaponCheck prototype)
            {
                var a = Helpers.Create<AbilityCasterMainWeaponCheckOrFeralCombat>();
                a.Category = prototype.Category;
                return a;
            }

            public bool CorrectCaster(UnitEntityData caster)
            {
                if (caster.Body.PrimaryHand.HasWeapon)
                {
                    return ((IEnumerable<WeaponCategory>)this.Category).Contains<WeaponCategory>(caster.Body.PrimaryHand.Weapon.Blueprint.Type.Category) || checkHasFeralCombat(caster, caster.Body.PrimaryHand.Weapon);
                }
                return false;
            }

            public string GetReason()
            {
                return (string)LocalizedTexts.Instance.Reasons.SpecificWeaponRequired;
            }
        }
    }

    namespace FeralCombatTrainingPatches
    {
        //fix deflect arrows
        [Harmony12.HarmonyPatch(typeof(DeflectArrows))]
        [Harmony12.HarmonyPatch("CheckRestriction", Harmony12.MethodType.Normal)]
        public class Patch_DeflectArrows_CheckRestriction_Patch
        {
            static void Postfix(DeflectArrows __instance, ref bool __result)
            {
                if (__result)
                {
                    return;
                }

                if (!__instance.Owner.Body.HandsAreEnabled)
                {
                    return;
                }
                var tr = Harmony12.Traverse.Create(__instance);
                int restriction = (int)(tr.Field("m_Restriction").GetValue());

                if (restriction != 0)
                {
                    return;
                }

                if (!FeralCombatTraining.checkHasFeralCombat(__instance.Owner.Unit, __instance.Owner.Body.PrimaryHand.MaybeWeapon))
                {
                    __result = FeralCombatTraining.checkHasFeralCombat(__instance.Owner.Unit, __instance.Owner.Body.SecondaryHand.MaybeWeapon);
                }
                else
                {
                    __result = true;
                }
                return;
            }
        }
    }
}
