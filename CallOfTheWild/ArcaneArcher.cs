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
using Kingmaker.UI.Log;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints.Items.Ecnchantments;

using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Alignments;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace CallOfTheWild
{
    static class ArcaneArcherClass
    {
        static LibraryScriptableObject library => Main.library;
        internal static BlueprintCharacterClass arcanearcher;
        internal static BlueprintCharacterClass[] arcanearcherArray;
        static internal ActivatableAbilityGroup enhance_arrows_enchancement_group = ActivatableAbilityGroupExtension.EnhanceArrowsEnchantment.ToActivatableAbilityGroup();
        static internal BlueprintBuff enhance_arrows_buff;
        static internal BlueprintAbilityResource enhance_arrows_resource;
        static internal BlueprintFeature arcane_archer_enhance_arrow_elemental;
        static internal BlueprintFeature arcane_archer_enhance_arrow_distance;
        static internal BlueprintFeature arcane_archer_enhance_arrow_burst;
        static internal BlueprintFeature arcane_archer_enhance_arrow_aligned;

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
            arcanearcher.StartingGold = ranger.StartingGold;
            arcanearcher.PrimaryColor = ranger.PrimaryColor;
            arcanearcher.SecondaryColor = ranger.SecondaryColor;

            arcanearcher.RecommendedAttributes = new StatType[] { StatType.Dexterity };
            arcanearcher.NotRecommendedAttributes = Array.Empty<StatType>();

            arcanearcher.EquipmentEntities = ranger.EquipmentEntities;
            arcanearcher.MaleEquipmentEntities = ranger.MaleEquipmentEntities;
            arcanearcher.FemaleEquipmentEntities = ranger.FemaleEquipmentEntities;

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

            createEnhanceArrows(allowed_weapons);
            var entries = new List<LevelEntry> {
                Helpers.LevelEntry(1, proficiencies, CreateSpellbookChoice(), CreateEnhanceArrowsMagic(allowed_weapons),
                                   library.Get<BlueprintFeature>("d3e6275cfa6e7a04b9213b7b292a011c"), // ray calculate feature
                                   library.Get<BlueprintFeature>("62ef1cdb90f1d654d996556669caf7fa")),
                Helpers.LevelEntry(2), //CreateImbueArrow(allowed_weapons)),
                Helpers.LevelEntry(3, arcane_archer_enhance_arrow_elemental),// , CreateEnhanceArrows(allowed_weapons)),
                Helpers.LevelEntry(4),// , CreateSeekerArrow(allowed_weapons)),
                Helpers.LevelEntry(5),// , CreateEnhanceArrows(allowed_weapons)),
                Helpers.LevelEntry(6),// , CreatePhaseArrow(allowed_weapons)),
                Helpers.LevelEntry(7, arcane_archer_enhance_arrow_burst),// , CreateEnhanceArrows(allowed_weapons)),
                Helpers.LevelEntry(8),// , CreateHailOfArrows(allowed_weapons)),
                Helpers.LevelEntry(9, arcane_archer_enhance_arrow_aligned),// , CreateEnhanceArrows(allowed_weapons)),
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

            return Helpers.CreateFeature("ArcaneArcherEnhanceArrows", "Enhance Arrows (Magic)",
                $"At 1st level, every nonmagical arrow an arcane archer nocks and lets fly becomes magical, gaining a +1 enhancement bonus. " +
                "Unlike magic weapons created by normal means, the archer need not spend gold pieces to accomplish this task. However, an archer’s " +
                "magic arrows only function for him.",
                "f64aa29727344ed9b7fa7918943d3038",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.Create<EnhanceArrowsMagic>(u => u.weapon_types = allowed_weapons));
        }

        static BlueprintFeature CreateSpellbookChoice()
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
            // aa_progression.ComponentsArray.AddToArray(Helpers.Create<SkipLevelsForSpellProgression>(s => s.Levels = skipLevels.ToArray()));
            return aa_progression;
        }

        static BlueprintActivatableAbility createEnhanceArrowsFeature(string name_prefix, string display_name, string description, UnityEngine.Sprite icon, BlueprintBuff sacred_buff,
                                                                         BlueprintItemEnchantment enchantment, int group_size, ActivatableAbilityGroup group,
                                                                         AlignmentMaskType alignment = AlignmentMaskType.Any)
        {
            //create buff
            //create activatable ability that gives buff
            //on main buff in activate add corresponding enchantment
            //create feature that gives activatable ability

            BlueprintBuff buff;

            if (enchantment is BlueprintWeaponEnchantment)
            {
                buff = Helpers.CreateBuff(name_prefix + "Buff",
                                              display_name,
                                              description,
                                              "",
                                              icon,
                                              null,
                                              Common.createBuffContextEnchantPrimaryHandWeapon(Common.createSimpleContextValue(1), false, true,
                                                                                               new Kingmaker.Blueprints.Items.Weapons.BlueprintWeaponType[0],
                                                                                               (BlueprintWeaponEnchantment)enchantment)
                                                                                               );
            }
            else
            {
                buff = Helpers.CreateBuff(name_prefix + "Buff",
                                              display_name,
                                              description,
                                              "",
                                              icon,
                                              null,
                                              Common.createBuffContextEnchantArmor(Common.createSimpleContextValue(1), false, true,
                                                                                               (BlueprintArmorEnchantment)enchantment)
                                                                                               );
            }
            buff.SetBuffFlags(BuffFlags.HiddenInUi);
            var switch_buff = Helpers.CreateBuff(name_prefix + "SwitchBuff",
                                  display_name,
                                  description,
                                  "",
                                  icon,
                                  null);
            switch_buff.SetBuffFlags(BuffFlags.HiddenInUi);

            Common.addContextActionApplyBuffOnFactsToActivatedAbilityBuffNoRemove(sacred_buff, buff, switch_buff);

            var ability = Helpers.CreateActivatableAbility(name_prefix + "ToggleAbility",
                                                                        display_name,
                                                                        description,
                                                                        "",
                                                                        icon,
                                                                        switch_buff,
                                                                        AbilityActivationType.Immediately,
                                                                        CommandType.Free,
                                                                        null
                                                                        );
            ability.WeightInGroup = group_size;
            ability.Group = group;
            ability.DeactivateImmediately = true;

            if (alignment != AlignmentMaskType.Any)
            {
                ability.AddComponent(Helpers.Create<NewMechanics.ActivatableAbilityAlignmentRestriction>(c => c.Alignment = alignment));
            }
            return ability;
        }

        static void createEnhanceArrows(BlueprintWeaponType[] allowed_weapons)
        {

           /* In addition, the arcane archer’s arrows gain a number of additional qualities as he gains additional levels. The elemental, elemental burst, 
           and aligned qualities can be changed once per day, when the arcane archer prepares spells or, in the case of spontaneous spellcasters, after 8 hours of rest.

           At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.

           At 5th level, every non-magical arrow fired by an arcane archer gains the distance weapon quality.

           At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: flaming burst, icy burst, 
           or shocking burst. This ability replaces the ability gained at 3rd level.

           At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, or unholy. 
           The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose anarchic or 
           unholy as his weapon quality).

           The bonuses granted by a magic bow apply as normal to arrows that have been enhanced with this ability. Only the larger enhancement bonus applies. 
           Duplicate abilities do not stack.
            */
            var enchants = new BlueprintWeaponEnchantment[] {library.Get<BlueprintWeaponEnchantment>("d704f90f54f813043a525f304f6c0050"),
                                                             library.Get<BlueprintWeaponEnchantment>("9e9bab3020ec5f64499e007880b37e52"),
                                                             library.Get<BlueprintWeaponEnchantment>("d072b841ba0668846adeb007f623bd6c"),
                                                             library.Get<BlueprintWeaponEnchantment>("6a6a0901d799ceb49b33d4851ff72132"),
                                                             library.Get<BlueprintWeaponEnchantment>("746ee366e50611146821d61e391edf16") };

            var enhancement_buff = Helpers.CreateBuff("ArcaneArcherEnhanceArrowsBaseBuff",
                                         "",
                                         "",
                                         "",
                                         null,
                                         null,
                                         Common.createBuffRemainingGroupsSizeEnchantPrimaryHandWeapon(enhance_arrows_enchancement_group,
                                                                                                      false, true,
                                                                                                      enchants
                                                                                                      )
                                         );
            enhance_arrows_buff = Helpers.CreateBuff("ArcaneArcherEnhanceArrowsSwitchBuff",
                                                                 "At 3rd level, In addition, the arcane archer’s arrows gain a number of additional qualities as he gains additional "+
                                                                 "levels. The elemental, elemental burst, and aligned qualities can be changed once per day, when the arcane archer prepares "+
                                                                 "spells or, in the case of spontaneous spellcasters, after 8 hours of rest." +
                                                                 "\n At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock." +
                                                                 "\n At 5th level, every non-magical arrow fired by an arcane archer gains the distance weapon quality." +
                                                                 "\n At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: flaming burst, "+
                                                                 "icy burst, or shocking burst. This ability replaces the ability gained at 3rd level." +
                                                                 "\n At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, "+
                                                                 "or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose "+
                                                                 "anarchic or unholy as his weapon quality).",
                                                                 "",
                                                                 "",
                                                                 Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"),
                                                                 null,
                                                                 Helpers.CreateAddFactContextActions(activated: Common.createContextActionApplyBuff(enhancement_buff, Helpers.CreateContextDuration(),
                                                                                                                is_child: true, is_permanent: true, dispellable: false)
                                                                                                     )
                                                                 );
            enhance_arrows_buff.SetBuffFlags(BuffFlags.HiddenInUi);

            var flaming = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsFlaming",
                                                                "Enhance Arrows - Flaming",
                                                                "At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.",
                                                                library.Get<BlueprintActivatableAbility>("7902941ef70a0dc44bcfc174d6193386").Icon,
                                                                enhance_arrows_buff,
                                                                library.Get<BlueprintWeaponEnchantment>("30f90becaaac51f41bf56641966c4121"),
                                                                1, enhance_arrows_enchancement_group);

            var frost = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsFrost",
                                                            "Enhance Arrows - Frost",
                                                            "At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.",
                                                            library.Get<BlueprintActivatableAbility>("b338e43a8f81a2f43a73a4ae676353a5").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("421e54078b7719d40915ce0672511d0b"),
                                                            1, enhance_arrows_enchancement_group);

            var shock = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsShock",
                                                            "Enhance Arrows - Shock",
                                                            "At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.",
                                                            library.Get<BlueprintActivatableAbility>("a3a9e9a2f909cd74e9aee7788a7ec0c6").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("7bda5277d36ad114f9f9fd21d0dab658"),
                                                            1, enhance_arrows_enchancement_group);

            var flaming_burst = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsFlamingBurst",
                                                                      "Enhance Arrows - Flaming Burst",
                                                                      "At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: flaming burst, icy burst, or shocking burst. This ability replaces the ability gained at 3rd level.",
                                                                      library.Get<BlueprintActivatableAbility>("7902941ef70a0dc44bcfc174d6193386").Icon,
                                                                      enhance_arrows_buff,
                                                                      library.Get<BlueprintWeaponEnchantment>("3f032a3cd54e57649a0cdad0434bf221"),
                                                                      1, enhance_arrows_enchancement_group);

            var icy_burst = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsIcyBurst",
                                                            "Enhance Arrows - Icy Burst",
                                                            "At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: flaming burst, icy burst, or shocking burst. This ability replaces the ability gained at 3rd level.",
                                                            library.Get<BlueprintActivatableAbility>("b338e43a8f81a2f43a73a4ae676353a5").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("564a6924b246d254c920a7c44bf2a58b"),
                                                            1, enhance_arrows_enchancement_group);

            var shocking_burst = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsShockingBurst",
                                                            "Enhance Arrows - Shocking Burst",
                                                            "At 7th level, every non-magical arrow fired by an arcane archer gains one of the following elemental burst weapon qualities: flaming burst, icy burst, or shocking burst. This ability replaces the ability gained at 3rd level.",
                                                            library.Get<BlueprintActivatableAbility>("a3a9e9a2f909cd74e9aee7788a7ec0c6").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("914d7ee77fb09d846924ca08bccee0ff"),
                                                            1, enhance_arrows_enchancement_group,
                                                            AlignmentMaskType.Good);

            var holy = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsHoly",
                                                            "Enhance Arrows - Holy",
                                                            "At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose anarchic or unholy as his weapon quality).",
                                                            library.Get<BlueprintActivatableAbility>("ce0ece459ebed9941bb096f559f36fa8").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("28a9964d81fedae44bae3ca45710c140"),
                                                            2, enhance_arrows_enchancement_group,
                                                            AlignmentMaskType.Evil);

            var unholy = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsUnholy",
                                                            "Enhance Arrows - Unholy",
                                                            "At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose anarchic or unholy as his weapon quality).",
                                                            library.Get<BlueprintActivatableAbility>("561803a819460f34ea1fe079edabecce").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("d05753b8df780fc4bb55b318f06af453"),
                                                            2, enhance_arrows_enchancement_group);

            var axiomatic = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsAxiomatic",
                                                            "Enhance Arrows - Axiomatic",
                                                            "At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose anarchic or unholy as his weapon quality).",
                                                            library.Get<BlueprintActivatableAbility>("d76e8a80ab14ac942b6a9b8aaa5860b1").Icon,
                                                            enhance_arrows_buff,
                                                            library.Get<BlueprintWeaponEnchantment>("0ca43051edefcad4b9b2240aa36dc8d4"),
                                                            2, enhance_arrows_enchancement_group,
                                                            AlignmentMaskType.Lawful);

            var anarchic = createEnhanceArrowsFeature("ArcaneArcherEnhanceArrowsAnarchic",
                                                "Enhance Arrows - Anarchic",
                                                "At 9th level, every non-magical arrow fired by an arcane archer gains one of the following aligned weapon qualities: anarchic, axiomatic, holy, or unholy. The arcane archer cannot choose an ability that is the opposite of his alignment (for example, a lawful good arcane archer could not choose anarchic or unholy as his weapon quality).",
                                                library.Get<BlueprintActivatableAbility>("8ed07b0cc56223c46953348f849f3309").Icon,
                                                enhance_arrows_buff,
                                                library.Get<BlueprintWeaponEnchantment>("57315bc1e1f62a741be0efde688087e9"),
                                                2, enhance_arrows_enchancement_group,
                                                AlignmentMaskType.Chaotic);

            enhance_arrows_resource = Helpers.CreateAbilityResource("ArcaneArcherEnhanceArrowsResource", "", "", "", null);
            enhance_arrows_resource.SetIncreasedByLevel(0, 1, arcanearcherArray);

            var enhance_arrows_ability = Helpers.CreateActivatableAbility("ArcaneArcherEnhanceArrowsToggleAbility",
                                                                         enhance_arrows_buff.Name,
                                                                         enhance_arrows_buff.Description,
                                                                         "",
                                                                         enhance_arrows_buff.Icon,
                                                                         enhance_arrows_buff,
                                                                         AbilityActivationType.Immediately,
                                                                         CommandType.Swift,
                                                                         null,
                                                                         Helpers.CreateActivatableResourceLogic(enhance_arrows_resource, ResourceSpendType.TurnOn),
                                                                         Helpers.Create<NewMechanics.ActivatableAbilityMainWeaponTypeAllowed>(c => c.weapon_types = allowed_weapons));

            arcane_archer_enhance_arrow_elemental = Helpers.CreateFeature("ArcaneArcherEnhanceArrows3Feature",
                                                            "Enhance arrows (elemental)",
                                                            enhance_arrows_ability.Description,
                                                            "",
                                                            enhance_arrows_ability.Icon,
                                                            FeatureGroup.None,
                                                            Helpers.CreateAddAbilityResource(enhance_arrows_resource),
                                                            Helpers.CreateAddFacts(flaming, frost, shock)
                                                            );


            // arcane_archer_enhance_arrow_distance should be distance but that isn't a thing in the game.
            // arcane_archer_enhance_arrow_distance = Helpers.CreateFeature("ArcaneArcherEnhanceArrows5Feature",
            //                                                                 "Enhance arrows (distance)",
            //                                                                 enhance_arrows_ability.Description,
            //                                                                 "",
            //                                                                 enhance_arrows_ability.Icon,
            //                                                                 FeatureGroup.None,
            //                                                                 Common.createIncreaseActivatableAbilityGroupSize(enhance_arrows_enchancement_group)
            //                                                                 );

            arcane_archer_enhance_arrow_burst = Helpers.CreateFeature("ArcaneArcherEnhanceArrows7Feature",
                                                                            "Enhance arrows (elemental burst)",
                                                                            enhance_arrows_ability.Description,
                                                                            "",
                                                                            enhance_arrows_ability.Icon,
                                                                            FeatureGroup.None,
                                                                            Common.createIncreaseActivatableAbilityGroupSize(enhance_arrows_enchancement_group),
                                                                            Helpers.CreateAddFacts(flaming_burst, icy_burst, shocking_burst)
                                                                            );

            arcane_archer_enhance_arrow_aligned = Helpers.CreateFeature("ArcaneArcherEnhanceArrows9Feature",
                                                                            "Enhance arrows (aligned)",
                                                                            enhance_arrows_ability.Description,
                                                                            "",
                                                                            enhance_arrows_ability.Icon,
                                                                            FeatureGroup.None,
                                                                            Common.createIncreaseActivatableAbilityGroupSize(enhance_arrows_enchancement_group),
                                                                            Helpers.CreateAddFacts(holy, unholy, axiomatic, anarchic));
        }





        static BlueprintFeature CreateImbueArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 2nd level, an arcane archer gains the ability to place an area spell upon an arrow. When the arrow is fired, 
            the spell’s area is centered where the arrow lands, even if the spell could normally be centered only on the caster. 
            This ability allows the archer to use the bow’s range rather than the spell’s range. A spell cast in this way uses its 
            standard casting time and the arcane archer can fire the arrow as part of the casting. The arrow must be fired during 
            the round that the casting is completed or the spell is wasted. If the arrow misses, the spell is wasted. */
            throw new NotImplementedException();
        }
        static BlueprintFeature CreateSeekerArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 4th level, an arcane archer can launch an arrow at a target known to him within range, and the arrow travels 
            to the target, even around corners. Only an unavoidable obstacle or the limit of the arrow’s range prevents the arrow’s flight.
             This ability negates cover and concealment modifiers, but otherwise the attack is rolled normally. Using this ability is a 
             standard action (and shooting the arrow is part of the action). An arcane archer can use this ability once per day at 4th level, 
             and one additional time per day for every two levels beyond 4th, to a maximum of four times per day at 10th level.
            */
            throw new NotImplementedException();
        }
        static BlueprintFeature CreatePhaseArrow(BlueprintWeaponType[] allowed_weapons)
        {
            /* TODO: At 6th level, an arcane archer can launch an arrow once per day at a target known to him within range, and the arrow travels 
            to the target in a straight path, passing through any nonmagical barrier or wall in its way. (Any magical barrier stops the arrow.) 
            This ability negates cover, concealment, armor, and shield modifiers, but otherwise the attack is rolled normally. Using this ability 
            is a standard action (and shooting the arrow is part of the action). An arcane archer can use this ability once per day at 6th level, 
            and one additional time per day for every two levels beyond 6th, to a maximum of three times per day at 10th level.
            */
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
            Log.Write("applying damage bonus");
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
            Log.Write("applying attack bonus");
            evt.AddBonus(1, Fact);
        }


        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }
}
