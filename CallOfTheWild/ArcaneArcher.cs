// Copyright (c) 2019 Alisdair Smith
// This code is licensed under MIT license (see LICENSE for details)

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using System;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using System.Collections.Generic;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using UnityEngine;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;


namespace CallOfTheWild
{
    static class ArcaneArcherClass
    {
        static LibraryScriptableObject library => Main.library;
        internal static BlueprintCharacterClass arcanearcher;
        internal static BlueprintCharacterClass[] arcanearcherArray;

        static internal ActivatableAbilityGroup enhance_arrows_elemental_group = ActivatableAbilityGroupExtension.EnhanceArrowsElemental.ToActivatableAbilityGroup();//ActivatableAbilityGroup.TrueMagus;

        internal static void Load()
        {
            var library = Main.library;
            if (ArcaneArcherClass.arcanearcher != null) return;

            // TODO: prestigious spellcaster needs to recognize Arcane Archer.
            var arcanearcher = ArcaneArcherClass.arcanearcher = Helpers.Create<BlueprintCharacterClass>();
            arcanearcherArray = new BlueprintCharacterClass[] { arcanearcher };
            arcanearcher.name = "ArcaneArcherClass";
            library.AddAsset(arcanearcher, "0fbf5e3fe02f4db19492659dc8a3c411");
            arcanearcher.LocalizedName = Helpers.CreateString("Arcanearcanearcher.Name", "Arcane Archer");
            arcanearcher.LocalizedDescription = Helpers.CreateString("Arcanearcanearcher.Description",
                "Many who seek to perfect the use of the bow sometimes pursue the path of the arcane archer. " +
                "Arcane archers are masters of ranged combat, as they possess the ability to strike at targets with unerring accuracy and can imbue their arrows with powerful spells. " +
                "Arrows fired by arcane archers fly at weird and uncanny angles to strike at foes around corners, and can pass through solid objects to hit enemies that cower behind such cover. " +
                "At the height of their power, arcane archers can fell even the most powerful foes with a single, deadly shot.. ");
            // Matched Druid skill progression
            arcanearcher.SkillPoints = 3;
            arcanearcher.HitDie = DiceType.D10;
            arcanearcher.PrestigeClass = true;

            var pointBlankShot = library.Get<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab"); // Point Blank Shot
            var preciseShot = library.Get<BlueprintFeature>("8f3d1e6b4be006f4d896081f2f889665"); // Precise Shot
            var weaponFocus = library.Get<BlueprintParametrizedFeature>("1e1f627d26ad36f43bbd26cc2bf8ac7e"); // Weapon Focus;
            var sbow = WeaponCategory.Shortbow;
            var lbow = WeaponCategory.Longbow;
            arcanearcher.SetComponents(
                pointBlankShot.PrerequisiteFeature(),
                preciseShot.PrerequisiteFeature(),
                Common.createPrerequisiteParametrizedFeatureWeapon(weaponFocus, lbow, any: true),
                Common.createPrerequisiteParametrizedFeatureWeapon(weaponFocus, sbow, any: true),
                StatType.BaseAttackBonus.PrerequisiteStatValue(6),
                Helpers.Create<PrerequisiteCasterTypeSpellLevel>(p => { p.IsArcane = true; p.RequiredSpellLevel = 1; p.Group = Prerequisite.GroupType.All; }));

            var savesPrestigeLow = library.Get<BlueprintStatProgression>("dc5257e1100ad0d48b8f3b9798421c72");
            var savesPrestigeHigh = library.Get<BlueprintStatProgression>("1f309006cd2855e4e91a6c3707f3f700");
            arcanearcher.BaseAttackBonus = library.Get<BlueprintStatProgression>("0538081888b2d8c41893d25d098dee99"); // BAB high
            arcanearcher.FortitudeSave = savesPrestigeHigh;
            arcanearcher.ReflexSave = savesPrestigeHigh;
            arcanearcher.WillSave = savesPrestigeLow;
            arcanearcher.IsArcaneCaster = true;

            // Perception (Wis), Ride (Dex), Stealth (Dex), and Survival (Wis).
            // knowledge nature in place of survival, there is no replacement for Ride
            arcanearcher.ClassSkills = new StatType[] {
                StatType.SkillStealth,
                StatType.SkillLoreNature,
                StatType.SkillPerception
            };


            // Used ranger stats as they seem to fit the theme pretty well
            var ranger = library.Get<BlueprintCharacterClass>("cda0615668a6df14eb36ba19ee881af6");
            var wizard = Helpers.GetClass("ba34257984f4c41408ce1dc2004e342e");
            arcanearcher.StartingGold = ranger.StartingGold;
            arcanearcher.PrimaryColor = ranger.PrimaryColor;
            arcanearcher.SecondaryColor = ranger.SecondaryColor;

            arcanearcher.RecommendedAttributes = new StatType[] { StatType.Dexterity };
            arcanearcher.NotRecommendedAttributes = Array.Empty<StatType>();

            arcanearcher.EquipmentEntities = wizard.EquipmentEntities;
            arcanearcher.MaleEquipmentEntities = wizard.MaleEquipmentEntities;
            arcanearcher.FemaleEquipmentEntities = wizard.FemaleEquipmentEntities;

            arcanearcher.StartingItems = ranger.StartingItems;

            var progression = Helpers.CreateProgression("ArcaneArcherProgression",
                arcanearcher.Name,
                arcanearcher.Description,
                "780848b1fb1f4d73a4f1bf64ae5c21b2",
                arcanearcher.Icon, // Need an icon
                FeatureGroup.None);
            progression.Classes = arcanearcherArray;


            var proficiencies = library.CopyAndAdd<BlueprintFeature>(
                "c5e479367d07d62428f2fe92f39c0341", // ranger proficiencies
                "ArcaneArcherProficiencies",
                "85be49f802ec4156ad34a3b88dd64fb5");
            proficiencies.SetName("Arcane Archer Proficiencies");
            proficiencies.SetDescription("An arcane archer is proficient with all simple and martial weapons, light armor, medium armor, and shields");

            var allowed_weapons = new BlueprintWeaponType[4];
            allowed_weapons[0] = library.Get<BlueprintWeaponType>("99ce02fb54639b5439d07c99c55b8542"); // shortbow
            allowed_weapons[1] = library.Get<BlueprintWeaponType>("7a1211c05ec2c46428f41e3c0db9423f"); // longbow
            allowed_weapons[2] = library.Get<BlueprintWeaponType>("1ac79088a7e5dde46966636a3ac71c35"); // composite longbow
            allowed_weapons[3] = library.Get<BlueprintWeaponType>("011f6f86a0b16df4bbf7f40878c3e80b"); // composite shortbow

            // TODO: implement these.
            var entries = new List<LevelEntry> {
                Helpers.LevelEntry(1, proficiencies, CreateSpellbookChoice(), CreateEnhanceArrowsMagic(allowed_weapons),
                                   library.Get<BlueprintFeature>("d3e6275cfa6e7a04b9213b7b292a011c"), // ray calculate feature
                                   library.Get<BlueprintFeature>("62ef1cdb90f1d654d996556669caf7fa")),
                Helpers.LevelEntry(2, CreateImbueArrow(allowed_weapons)),
                Helpers.LevelEntry(3, CreateEnhanceArrowsElemental(allowed_weapons)),
                Helpers.LevelEntry(4),// , CreateSeekerArrow(allowed_weapons)),
                Helpers.LevelEntry(5, CreateArcheryFeatSelection()), // Distant arrows aren't possible, providing a feat for this level seems reasonable seeing as the class also doesn't get spellcasting here.
                Helpers.LevelEntry(6),// , CreatePhaseArrow(allowed_weapons)),
                Helpers.LevelEntry(7, CreateEnhanceArrowsBurst()),
                Helpers.LevelEntry(8),// , CreateHailOfArrows(allowed_weapons)),
                Helpers.LevelEntry(9, CreateEnhanceArrowsAligned(allowed_weapons)),
                Helpers.LevelEntry(10),//,  CreateArrowOfDeath(allowed_weapons)),
            };

            progression.UIDeterminatorsGroup = new BlueprintFeatureBase[] {
                proficiencies
                // TODO: 1st level stuff
            };

            progression.UIGroups = Helpers.CreateUIGroups(); // TODO
            progression.LevelEntries = entries.ToArray();

            arcanearcher.Progression = progression;

            // Arcane archers do not gets spells at levels 1,5 and 9, we handle level 1 by giving spellbook selection at level 2
            // we handle 5 and 9 by adding a skip levels for spell progression component to progressiom.
            var skipLevels = new List<int>();
            skipLevels.Add(5);
            skipLevels.Add(9);
            arcanearcher.AddComponent(Helpers.Create<SkipLevelsForSpellProgression>(s => s.Levels = skipLevels.ToArray()));

            arcanearcher.Archetypes = Array.Empty<BlueprintArchetype>();

            Helpers.RegisterClass(arcanearcher);
            Helpers.classes.Add(arcanearcher);
            addToPrestigeClasses();
        }
        static void addToPrestigeClasses()
        {
            var wizard = Helpers.GetClass("ba34257984f4c41408ce1dc2004e342e"); // wizard
            var sorcerer = Helpers.GetClass("b3a505fb61437dc4097f43c3f8f9a4cf"); // sorcerer
            var magus = Helpers.GetClass("45a4607686d96a1498891b3286121780"); // magus
            var bard = Helpers.GetClass("772c83a25e2268e448e841dcd548235f"); // bard

            Common.addReplaceSpellbook(library.Get<BlueprintFeatureSelection>("ea4c7c56d90d413886876152b03f9f5f"), wizard.Spellbook, "ArcaneArcherWizard",
                Common.createPrerequisiteClassSpellLevel(wizard, 1));
            Common.addReplaceSpellbook(library.Get<BlueprintFeatureSelection>("ea4c7c56d90d413886876152b03f9f5f"), sorcerer.Spellbook, "ArcaneArcherSorcerer",
                Common.createPrerequisiteClassSpellLevel(sorcerer, 1));
            Common.addReplaceSpellbook(library.Get<BlueprintFeatureSelection>("ea4c7c56d90d413886876152b03f9f5f"), magus.Spellbook, "ArcaneArcherMagus",
                Common.createPrerequisiteClassSpellLevel(magus, 1));
            Common.addReplaceSpellbook(library.Get<BlueprintFeatureSelection>("ea4c7c56d90d413886876152b03f9f5f"), bard.Spellbook, "ArcaneArcherBard",
                Common.createPrerequisiteClassSpellLevel(bard, 1));
        }
        static BlueprintFeature CreateEnhanceArrowsMagic(BlueprintWeaponType[] allowed_weapons)
        {

            return Helpers.CreateFeature("ArcaneArcherEnhanceArrowsMagic", "Enhance Arrows (Magic)",
                $"At 1st level, every nonmagical arrow an arcane archer nocks and lets fly becomes magical, gaining a +1 enhancement bonus. " +
                "Unlike magic weapons created by normal means, the archer need not spend gold pieces to accomplish this task. However, an archer’s " +
                "magic arrows only function for him.",
                "f64aa29727344ed9b7fa7918943d3038",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.Create<EnhanceArrowsMagic>(u => u.weapon_types = allowed_weapons));
        }

        static BlueprintFeature CreateEnhanceArrowsElemental(BlueprintWeaponType[] allowed_weapons)
        {
            var resource = Helpers.CreateAbilityResource("EnhanceArrowsElementalResource", "", "", "", library.Get<BlueprintFeature>("6aa84ca8918ac604685a3d39a13faecc").Icon);
            resource.SetFixedResource(1);
            var name = "EnhanceArrows";
            var displayName = "Enhance Arrows";
            var fireArrowBuff = Helpers.CreateBuff(name + "Fire" + "Buff", displayName + " (Fire)", $"Whilst active, your arrows deal 1d6 additional Fire damage.", "",
                library.Get<BlueprintActivatableAbility>("7902941ef70a0dc44bcfc174d6193386").Icon, null,
                Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Fire; }));
            var frostArrowBuff = Helpers.CreateBuff(name + "Frost" + "Buff", displayName + " (Frost)", $"Whilst active, your arrows deal 1d6 additional Frost damage.", "",
                library.Get<BlueprintActivatableAbility>("b338e43a8f81a2f43a73a4ae676353a5").Icon, null,
                Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Cold; }));
            var shockArrowBuff = Helpers.CreateBuff(name + "Shock" + "Buff", displayName + " (Shock)", $"Whilst active, your arrows deal 1d6 additional Shock damage.", "",
                library.Get<BlueprintActivatableAbility>("a3a9e9a2f909cd74e9aee7788a7ec0c6").Icon, null,
                Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Electricity; }));

            var actionFire = Helpers.CreateRunActions(Common.createContextActionApplyBuff(fireArrowBuff,
                            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
                            Common.createContextActionRemoveBuff(shockArrowBuff), Common.createContextActionRemoveBuff(frostArrowBuff));
            var actionFrost = Helpers.CreateRunActions(Common.createContextActionApplyBuff(frostArrowBuff,
                            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
                            Common.createContextActionRemoveBuff(shockArrowBuff), Common.createContextActionRemoveBuff(fireArrowBuff));
            var actionShock = Helpers.CreateRunActions(Common.createContextActionApplyBuff(shockArrowBuff,
                            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
                            Common.createContextActionRemoveBuff(fireArrowBuff), Common.createContextActionRemoveBuff(frostArrowBuff));

            var abilityFire = Helpers.CreateAbility("EnhanceArrowsFireAbility",
                                fireArrowBuff.Name, fireArrowBuff.Description, "", fireArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                                AbilityRange.Weapon, "Permanent", "N/A", actionFire, Helpers.CreateResourceLogic(resource));
            var abilityFrost = Helpers.CreateAbility("EnhanceArrowsFrostAbility",
                                frostArrowBuff.Name, frostArrowBuff.Description, "", frostArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                                AbilityRange.Weapon, "Permanent", "N/A", actionFrost, Helpers.CreateResourceLogic(resource));
            var abilityShock = Helpers.CreateAbility("EnhanceArrowsShockAbility",
                                shockArrowBuff.Name, shockArrowBuff.Description, "", shockArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                                AbilityRange.Weapon, "Permanent", "N/A", actionShock, Helpers.CreateResourceLogic(resource));

            var feat = Helpers.CreateFeature("ArcaneArcherEnhanceArrowsElemental", "Enhance Arrows (Elemental)",
                $"At 3rd level, In addition, the arcane archer’s arrows gain a number of additional qualities as he gains additional " +
                "levels. The elemental, elemental burst, and aligned qualities can be changed once per day, when the arcane archer prepares " +
                "spells or, in the case of spontaneous spellcasters, after 8 hours of rest." +
                "\n At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.",
                "",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.CreateAddFact(abilityFire),
                Helpers.CreateAddFact(abilityFrost),
                Helpers.CreateAddFact(abilityShock),
                Helpers.CreateAddAbilityResource(resource));
            return feat;
        }

        static BlueprintFeature CreateEnhanceArrowsAligned(BlueprintWeaponType[] allowed_weapons)
        {
            var resource = Helpers.CreateAbilityResource("EnhanceArrowsAlignedResource", "", "", "", library.Get<BlueprintFeature>("6aa84ca8918ac604685a3d39a13faecc").Icon);
            resource.SetFixedResource(1);
            var name = "EnhanceArrows";
            var displayName = "Enhance Arrows";

            //buffs
            var holyArrowBuff = Helpers.CreateBuff(name + "Holy" + "Buff", displayName + " (Holy)",
            $"Whilst active, your arrows deal 2d6 additional Holy damage against creatures of evil alignment", "",
            library.Get<BlueprintActivatableAbility>("ce0ece459ebed9941bb096f559f36fa8").Icon, null,
            Helpers.Create<EnhanceArrowsAligned>(u => { u.weapon_types = allowed_weapons; u.alignment = "Unoly"; u.damage_type = DamageEnergyType.Holy; }));
            var unholyArrowBuff = Helpers.CreateBuff(name + "Unoly" + "Buff", displayName + " (Unoly)",
            $"Whilst active, your arrows deal 2d6 additional Unholy damage against creatures of good alignment", "",
            library.Get<BlueprintActivatableAbility>("561803a819460f34ea1fe079edabecce").Icon, null,
            Helpers.Create<EnhanceArrowsAligned>(u => { u.weapon_types = allowed_weapons; u.alignment = "Unoly"; u.damage_type = DamageEnergyType.Unholy; }));
            var anarchicArrowBuff = Helpers.CreateBuff(name + "Anarchic" + "Buff", displayName + " (Anarchic)",
            $"Whilst active, your arrows deal 2d6 additional Unholy damage against creatures of lawful alignment", "",
            library.Get<BlueprintActivatableAbility>("8ed07b0cc56223c46953348f849f3309").Icon, null,
            Helpers.Create<EnhanceArrowsAligned>(u => { u.weapon_types = allowed_weapons; u.alignment = "Anarchic"; u.damage_type = DamageEnergyType.Unholy; }));
            var axiomaticArrowBuff = Helpers.CreateBuff(name + "Axiomic" + "Buff", displayName + " (Axiomic)",
            $"Whilst active, your arrows deal 2d6 additional Holy damage against creatures of chaotic alignment", "",
            library.Get<BlueprintActivatableAbility>("d76e8a80ab14ac942b6a9b8aaa5860b1").Icon, null,
            Helpers.Create<EnhanceArrowsAligned>(u => { u.weapon_types = allowed_weapons; u.alignment = "Axiomic"; u.damage_type = DamageEnergyType.Holy; }));

            //actions
            var actionHoly = Helpers.CreateRunActions(Common.createContextActionApplyBuff(holyArrowBuff,
            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
            Common.createContextActionRemoveBuff(unholyArrowBuff), Common.createContextActionRemoveBuff(anarchicArrowBuff),
            Common.createContextActionRemoveBuff(axiomaticArrowBuff));
            var actionUnholy = Helpers.CreateRunActions(Common.createContextActionApplyBuff(unholyArrowBuff,
            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
            Common.createContextActionRemoveBuff(holyArrowBuff), Common.createContextActionRemoveBuff(anarchicArrowBuff),
            Common.createContextActionRemoveBuff(axiomaticArrowBuff));
            var actionAnarchic = Helpers.CreateRunActions(Common.createContextActionApplyBuff(anarchicArrowBuff,
            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
            Common.createContextActionRemoveBuff(holyArrowBuff), Common.createContextActionRemoveBuff(unholyArrowBuff),
            Common.createContextActionRemoveBuff(axiomaticArrowBuff));
            var actionAxiomatic = Helpers.CreateRunActions(Common.createContextActionApplyBuff(axiomaticArrowBuff,
            Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), is_permanent: true, dispellable: false),
            Common.createContextActionRemoveBuff(holyArrowBuff), Common.createContextActionRemoveBuff(unholyArrowBuff),
            Common.createContextActionRemoveBuff(anarchicArrowBuff));

            //abilities
            var abilityHoly = Helpers.CreateAbility("EnhanceArrowsHolyAbility",
                            holyArrowBuff.Name,
                            holyArrowBuff.Description, "", holyArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                            AbilityRange.Weapon, "Permanent", "N/A", actionHoly, Helpers.CreateResourceLogic(resource),
                            Helpers.Create<AbilityCasterAlignment>(c => c.Alignment = AlignmentMaskType.Any & ~AlignmentMaskType.Evil));
            var abilityUnoly = Helpers.CreateAbility("EnhanceArrowsUnholyAbility",
                            unholyArrowBuff.Name,
                            unholyArrowBuff.Description, "", unholyArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                            AbilityRange.Weapon, "Permanent", "N/A", actionUnholy, Helpers.CreateResourceLogic(resource),
                            Helpers.Create<AbilityCasterAlignment>(c => c.Alignment = AlignmentMaskType.Any & ~AlignmentMaskType.Good));
            var abilityAnarchic = Helpers.CreateAbility("EnhanceArrowsAnarchicAbility",
                            anarchicArrowBuff.Name,
                            anarchicArrowBuff.Description, "", anarchicArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                            AbilityRange.Weapon, "Permanent", "N/A", actionAnarchic, Helpers.CreateResourceLogic(resource),
                            Helpers.Create<AbilityCasterAlignment>(c => c.Alignment = AlignmentMaskType.Any & ~AlignmentMaskType.Lawful));
            var abilityAxiomatic = Helpers.CreateAbility("EnhanceArrowsAxiomaticAbility",
                            axiomaticArrowBuff.Name,
                            axiomaticArrowBuff.Description, "", axiomaticArrowBuff.Icon, AbilityType.Special, CommandType.Free,
                            AbilityRange.Weapon, "Permanent", "N/A", actionAxiomatic, Helpers.CreateResourceLogic(resource),
                            Helpers.Create<AbilityCasterAlignment>(c => c.Alignment = AlignmentMaskType.Any & ~AlignmentMaskType.Chaotic));

            //feature
            var feat = Helpers.CreateFeature("ArcaneArcherEnhanceArrowsAligned", "Enhance Arrows (Aligned)",
                $"At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: " +
                "anarchic, axiomatic, holy, or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment " +
                "(for example, a lawful good arcane archer could not choose anarchic or unholy as his weapon quality).",
                "",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.CreateAddFact(abilityHoly),
                Helpers.CreateAddFact(abilityUnoly),
                Helpers.CreateAddFact(abilityAnarchic),
                Helpers.CreateAddFact(abilityAxiomatic),
                Helpers.CreateAddAbilityResource(resource));
            return feat;
        }

        static BlueprintFeature CreateEnhanceArrowsBurst()
        {
            return Helpers.CreateFeature("ArcaneArcherEnhanceArrowsBurst", "Enhance Arrows (Burst)",
                $"At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: " +
                "flaming burst, icy burst, or shocking burst. This ability replaces the ability gained at 3rd level.",
                "",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None);
        }

        // Not currently using this feature, if we can find a way to get it to work, then it may get added.
        static BlueprintFeature CreateEnhanceArrowsDistance()
        {
            return Helpers.CreateFeature("ArcaneArcherEnhanceArrowsDistance", "Enhance Arrows (Distance)",
                $"At 5th level, every non-magical arrow fired by an arcane archer gains the distance weapon quality.",
                "",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None);
        }

        static BlueprintFeatureSelection CreateSpellbookChoice()
        {
            var comps = new List<BlueprintComponent>();
            var compsArray = comps.ToArray();
            var aa_progression = Helpers.CreateFeatureSelection("ArcaneArcherSpellbookSelection",
            "Arcane Spellcasting",
            $"At 2nd level, and at every level thereafter, with an exception for 5th and 9th levels, " +
                                       "an Arcane Archer  gains new spells per day as if he had also gained a level in an arcane spellcasting " +
                                       "class he belonged to before adding the prestige class. He does not, however, gain any other benefit a " +
                                       "character of that class would have gained, except for additional spells per day, spells known, and an " +
                                       "increased effective level of spellcasting. If a character had more than one arcane spellcasting class " +
                                       "before becoming an Arcane Archer, he must decide to which class he adds the new level for purposes of " +
                                       "determining spells per day.",
                                       "ea4c7c56d90d413886876152b03f9f5f",
                                       LoadIcons.Image2Sprite.Create(@"FeatIcons/Icon_Casting_Combat.png"),
                                       FeatureGroup.None, compsArray);
            aa_progression.IsClassFeature = true;
            return aa_progression;
        }

        static BlueprintFeature CreateImbueArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 2nd level, an arcane archer gains the ability to place an area spell upon an arrow. When the arrow is fired, 
            the spell’s area is centered where the arrow lands, even if the spell could normally be centered only on the caster. 
            This ability allows the archer to use the bow’s range rather than the spell’s range. A spell cast in this way uses its 
            standard casting time and the arcane archer can fire the arrow as part of the casting. The arrow must be fired during 
            the round that the casting is completed or the spell is wasted. If the arrow misses, the spell is wasted. */

            var buff = Helpers.CreateBuff("ImbueArrowsBuff", "Imbue Arrows", $"Whilst active, you use the range of your bow and make a ranged weapon attack as part of casting area of effect spells", "",
                        Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), null, Helpers.Create<ImbueArrows>(u => { u.weapon_types = allowed_weapons; }));
            var ability = Helpers.CreateActivatableAbility("ImbueArrowsAbility",
                                                                    buff.Name,
                                                                    buff.Description,
                                                                    "",
                                                                    buff.Icon,
                                                                    buff,
                                                                    AbilityActivationType.Immediately,
                                                                    CommandType.Free,
                                                                    null,
                                                                    Helpers.Create<NewMechanics.ActivatableAbilityMainWeaponTypeAllowed>(c => c.weapon_types = allowed_weapons));


            var feat = Helpers.CreateFeature("ImbueArrowsFeature", "Imbue Arrows",
                $"At 2nd level, an arcane archer gains the ability to place an area spell upon an arrow. When the arrow is fired, " +
            "the spell’s area is centered where the arrow lands, even if the spell could normally be centered only on the caster. " +
            "This ability allows the archer to use the bow’s range rather than the spell’s range. A spell cast in this way uses its " +
            "standard casting time and the arcane archer can fire the arrow as part of the casting. The arrow must be fired during " +
            "the round that the casting is completed or the spell is wasted. If the arrow misses, the spell is wasted.",
                "",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.CreateAddFact(ability));


            return feat;
        }
        static BlueprintFeature CreateSeekerArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 4th level, an arcane archer can launch an arrow at a target known to him within range, and the arrow travels 
            to the target, even around corners. Only an unavoidable obstacle or the limit of the arrow’s range prevents the arrow’s flight.
             This ability negates cover and concealment modifiers, but otherwise the attack is rolled normally. Using this ability is a 
             standard action (and shooting the arrow is part of the action). An arcane archer can use this ability once per day at 4th level, 
             and one additional time per day for every two levels beyond 4th, to a maximum of four times per day at 10th level.
            */

            // targetted ability
            // Should apply this fact
            // Kingmaker.Designers.Mechanics.Facts.IgnoreConcealment
            throw new NotImplementedException();
        }

        static BlueprintFeatureSelection CreateArcheryFeatSelection()
        {
            var aa_feat = library.CopyAndAdd<BlueprintFeatureSelection>("6c799d09d5b93f344b9ade0e0c765c2d", "ArcaneArcherFeat", "c7179c618cc84a9283ceb95f2f4fcc46");//archery feat 6
            aa_feat.SetDescription("At 5th level an arcane archer gains an additional archery feat.");
            return aa_feat;
        }
        static BlueprintFeature CreatePhaseArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 6th level, an arcane archer can launch an arrow once per day at a target known to him within range, and the arrow travels 
            to the target in a straight path, passing through any nonmagical barrier or wall in its way. (Any magical barrier stops the arrow.) 
            This ability negates cover, concealment, armor, and shield modifiers, but otherwise the attack is rolled normally. Using this ability 
            is a standard action (and shooting the arrow is part of the action). An arcane archer can use this ability once per day at 6th level, 
            and one additional time per day for every two levels beyond 6th, to a maximum of three times per day at 10th level.
            */

            // targetted ability
            // treat weapon as being brilliant energy for the attack
            // Should apply this fact
            // Kingmaker.Designers.Mechanics.Facts.IgnoreConcealment
            throw new NotImplementedException();
        }
        static BlueprintFeature CreateHailOfArrows(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: In lieu of his regular attacks, once per day an arcane archer of 8th level or higher can fire an arrow at each and every 
            target within range, to a maximum of one target for every arcane archer level she has earned. Each attack uses the archer’s primary 
            attack bonus, and each enemy may only be targeted by a single arrow
            */
            throw new NotImplementedException();
        }
        static BlueprintFeature CreateArrowOfDeath(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 10th level, an arcane archer can create a special type of slaying arrow that forces the target, if damaged by the arrow’s 
            attack, to make a Fortitude save or be slain immediately. The DC of this save is equal to 20 + the arcane archer’s Charisma modifier. 
            It takes 1 day to make a slaying arrow, and the arrow only functions for the arcane archer who created it. The slaying arrow lasts no 
            longer than 1 year, and the archer can only have one such arrow in existence at a time.*/
            throw new NotImplementedException();
        }
    }

    public class EnhanceArrowsMagic : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        public BlueprintWeaponType[] weapon_types;

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (!Array.Exists(weapon_types, t => t == evt.Weapon.Blueprint.Type))
            {
                return;
            }

            foreach (var e in evt.Weapon.Enchantments)
            {
                if (e.Blueprint.GetComponent<WeaponEnhancementBonus>() != null) { return; }
            }
            evt.AddBonusDamage(1);
            evt.Enhancement = 1;
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (!Array.Exists(weapon_types, t => t == evt.Weapon.Blueprint.Type))
            {
                return;
            }

            foreach (var e in evt.Weapon.Enchantments)
            {
                if (e.Blueprint.GetComponent<WeaponEnhancementBonus>() != null || e.Blueprint.GetComponent<WeaponMasterwork>() != null) { return; }
            }
            evt.AddBonus(1, Fact);
        }


        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }

    public class EnhanceArrowsElemental : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public BlueprintWeaponType[] weapon_types;
        public DamageEnergyType damage_type;
        static LibraryScriptableObject library => Main.library;
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (!Array.Exists(weapon_types, t => t == evt.Weapon.Blueprint.Type))
            {
                return;
            }

            foreach (var e in evt.Weapon.Enchantments)
            {
                if (e.Blueprint.GetComponent<WeaponEnergyDamageDice>() != null) { if (e.Blueprint.GetComponent<WeaponEnergyDamageDice>().Element == damage_type) { return; } }
            }

            DamageDescription damageDescription = new DamageDescription()
            {
                TypeDescription = new DamageTypeDescription()
                {
                    Type = DamageType.Energy,
                    Energy = damage_type
                },
                Dice = new DiceFormula(1, DiceType.D6)
            };

            evt.DamageDescription.Add(damageDescription);

        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            var weapon = Owner.Body.PrimaryHand.HasWeapon ? Owner.Body.PrimaryHand.MaybeWeapon : Owner.Body.EmptyHandWeapon;
            if (!Array.Exists(weapon_types, t => t == weapon.Blueprint.Type))
            {
                return;
            }
            if (Owner.Progression.GetClassLevel(library.Get<BlueprintCharacterClass>("0fbf5e3fe02f4db19492659dc8a3c411")) >= 7)
            {
                RuleAttackRoll attackRoll = evt.AttackRoll;
                if (base.Owner == null || attackRoll == null || !attackRoll.IsCriticalConfirmed || attackRoll.FortificationNegatesCriticalHit)
                {
                    return;
                }
                RuleCalculateWeaponStats ruleCalculateWeaponStats = Rulebook.Trigger<RuleCalculateWeaponStats>(new RuleCalculateWeaponStats(evt.Initiator, weapon, null));
                DiceFormula dice = new DiceFormula(Math.Max(ruleCalculateWeaponStats.CriticalMultiplier - 1, 1), DiceType.D10);
                evt.DamageBundle.Add(new EnergyDamage(dice, damage_type));

            }
        }

        public void OnEventDidTrigger(RuleDealDamage evt) { }

    }

    public class EnhanceArrowsAligned : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public BlueprintWeaponType[] weapon_types;
        public string alignment;
        public DamageEnergyType damage_type;
        static LibraryScriptableObject library => Main.library;
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            var weapon = Owner.Body.PrimaryHand.HasWeapon ? Owner.Body.PrimaryHand.MaybeWeapon : Owner.Body.EmptyHandWeapon;
            if (!Array.Exists(weapon_types, t => t == weapon.Blueprint.Type))
            {
                return;
            }
            if (alignment == "Holy")
            {
                if (evt.Target.Blueprint.Alignment != Alignment.ChaoticEvil
                || evt.Target.Blueprint.Alignment != Alignment.LawfulEvil
                || evt.Target.Blueprint.Alignment != Alignment.NeutralEvil)
                {
                    { return; }
                }
            }
            if (alignment == "Unholy")
            {
                if (evt.Target.Blueprint.Alignment != Alignment.ChaoticGood
                || evt.Target.Blueprint.Alignment != Alignment.LawfulGood
                || evt.Target.Blueprint.Alignment != Alignment.NeutralGood)
                {
                    { return; }
                }
            }
            if (alignment == "Anarchic")
            {
                if (evt.Target.Blueprint.Alignment != Alignment.LawfulGood
                || evt.Target.Blueprint.Alignment != Alignment.LawfulNeutral
                || evt.Target.Blueprint.Alignment != Alignment.LawfulEvil)
                {
                    { return; }
                }
            }
            if (alignment == "Axiomatic")
            {
                if (evt.Target.Blueprint.Alignment != Alignment.ChaoticGood
                || evt.Target.Blueprint.Alignment != Alignment.ChaoticNeutral
                || evt.Target.Blueprint.Alignment != Alignment.ChaoticEvil)
                {
                    { return; }
                }
            }

            RuleCalculateWeaponStats ruleCalculateWeaponStats = Rulebook.Trigger<RuleCalculateWeaponStats>(new RuleCalculateWeaponStats(evt.Initiator, weapon, null));
            var dice = new DiceFormula(2, DiceType.D6);
            evt.DamageBundle.Add(new EnergyDamage(dice, damage_type));
        }

        public void OnEventDidTrigger(RuleDealDamage evt) { }
    }


    public class ImbueArrows : OwnedGameLogicComponent<UnitDescriptor>, IAbilityTargetSelectionUIHandler, IGlobalSubscriber

    {
        public BlueprintWeaponType[] weapon_types;
        Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange range = Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Custom;
        Feet oldCustomRange = new Kingmaker.Utility.Feet(0);
        Feet newCustomRange = new Kingmaker.Utility.Feet(0);

        public static ImbueArrows Instance { get; private set; }
        public void Initialize()
        {
            this.CursorTarget.SetActive(false);
            EventBus.Subscribe(this);
            ImbueArrows.Instance = this;
        }
        public void HandleAbilityTargetSelectionStart(AbilityData evt)
        {
            if (evt.Blueprint.IsSpell)
            {
                var weapon = Owner.Body.PrimaryHand.HasWeapon ? Owner.Body.PrimaryHand.MaybeWeapon : Owner.Body.EmptyHandWeapon;
                var spellrange = evt.Blueprint.Range;
                oldCustomRange = evt.Blueprint.CustomRange;
                if (evt.Blueprint.HasAreaEffect())
                {
                    range = evt.Blueprint.Range;
                    evt.Blueprint.Range = Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Weapon;
                    evt.Blueprint.CustomRange = weapon.Blueprint.AttackRange;
                    newCustomRange = evt.Blueprint.CustomRange;
                }
                EventBus.Subscribe(this);
            }
        }
        public void HandleAbilityTargetSelectionEnd(AbilityData evt)
        {
            if (evt.Blueprint.IsSpell)
            {
                if (range != Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Custom)
                {
                    evt.Blueprint.Range = range;
                    range = Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Custom;
                }
                if (oldCustomRange != newCustomRange)
                {
                    evt.Blueprint.CustomRange = oldCustomRange;
                }
            }

        }

        public GameObject CursorTarget;
    }


}


