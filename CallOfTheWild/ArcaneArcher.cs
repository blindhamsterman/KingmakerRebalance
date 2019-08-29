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
using Kingmaker.Enums.Damage;

using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Mechanics.Components;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace CallOfTheWild
{
    static class ArcaneArcherClass
    {
        static LibraryScriptableObject library => Main.library;
        internal static BlueprintCharacterClass arcanearcher;
        internal static BlueprintCharacterClass[] arcanearcherArray;

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
            var entries = new List<LevelEntry> {
                Helpers.LevelEntry(1, proficiencies, CreateSpellbookChoice(), CreateEnhanceArrowsMagic(allowed_weapons),
                                   library.Get<BlueprintFeature>("d3e6275cfa6e7a04b9213b7b292a011c"), // ray calculate feature
                                   library.Get<BlueprintFeature>("62ef1cdb90f1d654d996556669caf7fa")),
                Helpers.LevelEntry(2, library.Get<BlueprintFeature>("6aa84ca8918ac604685a3d39a13faecc")), // Eldritch Archer Ranged Spellstrike //CreateImbueArrow(allowed_weapons)),
                Helpers.LevelEntry(3, CreateEnhanceArrowsElemental(allowed_weapons)),
                Helpers.LevelEntry(4),// , CreateSeekerArrow(allowed_weapons)),
                Helpers.LevelEntry(5, CreateArcheryFeatSelection()), // Distant arrows aren't possible, providing a feat for this level seems reasonable seeing as the class also doesn't get spellcasting here.
                Helpers.LevelEntry(6),// , CreatePhaseArrow(allowed_weapons)),
                Helpers.LevelEntry(7),// , CreateEnhanceArrows(allowed_weapons)),
                Helpers.LevelEntry(8),// , CreateHailOfArrows(allowed_weapons)),
                Helpers.LevelEntry(9),// , CreateEnhanceArrows(allowed_weapons)),
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
            var ability = Helpers.CreateAbility("EnhanceArrowsFireAbility", "Enhance Arrows (Fire)",
                $"Whilst active, your arrows deal 1d6 additional Fire damage.",
                "5e5f8a964e21460399018c65da9f26e7", Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), AbilityType.Special, CommandType.Free,
                AbilityRange.Weapon, "Permenant", "N/A", Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Fire; }));
            var ability2 = Helpers.CreateAbility("EnhanceArrowsFrostAbility", "Enhance Arrows (Frost)",
                $"Whilst active, your arrows deal 1d6 additional Frost damage.",
                "1394d9a00c9b493286f07fc5e038753a", Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), AbilityType.Special, CommandType.Free,
                AbilityRange.Weapon, "Permenant", "N/A", Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Cold; }));
            var ability3 = Helpers.CreateAbility("EnhanceArrowsShockAbility", "Enhance Arrows (Shock)",
                $"Whilst active, your arrows deal 1d6 additional Shock damage.",
                "6f6ae2b441f54c8b984633825f80e11f", Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), AbilityType.Special, CommandType.Free,
                AbilityRange.Weapon, "Permenant", "N/A", Helpers.Create<EnhanceArrowsElemental>(u => { u.weapon_types = allowed_weapons; u.damage_type = DamageEnergyType.Electricity; }));
            return Helpers.CreateFeature("ArcaneArcherEnhanceArrowsElemental", "Enhance Arrows (Elemental)",
                $"At 3rd level, In addition, the arcane archer’s arrows gain a number of additional qualities as he gains additional " +
                "levels. The elemental, elemental burst, and aligned qualities can be changed once per day, when the arcane archer prepares " +
                "spells or, in the case of spontaneous spellcasters, after 8 hours of rest." +
                "\n At 3rd level, every non-magical arrow fired by an arcane archer gains one of the following elemental themed weapon qualities: flaming, frost, or shock.",
                "dc982851404b45388eca6fc8deacebcb",
                Helpers.GetIcon("6aa84ca8918ac604685a3d39a13faecc"), // spellstrike
                FeatureGroup.None,
                Helpers.CreateAddFact(ability),
                Helpers.CreateAddFact(ability2),
                Helpers.CreateAddFact(ability3),
                Helpers.CreateAddAbilityResource(resource));
        }

        static BlueprintFeature CreateSpellbookChoice()
        {
            var comps = new List<BlueprintComponent>();
            var compsArray = comps.ToArray();
            var aa_progression = Helpers.CreateFeature("ArcaneArcherSpellbookSelection",
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

            // targetted ability
            // Should apply this fact
            // Kingmaker.Designers.Mechanics.Facts.IgnoreConcealment
            throw new NotImplementedException();
        }

        static BlueprintFeatureSelection CreateArcheryFeatSelection()
        {
            var comps = new List<BlueprintComponent>();
            var compsArray = comps.ToArray();
            var aa_feat = Helpers.CreateFeatureSelection("ArcaneArcherFeat",
            "Arcane Archery Feat",
            $"At 5th level an arcane archer gains an additional archery or spellcasting feat. ",
                                       "c7179c618cc84a9283ceb95f2f4fcc46",
                                       LoadIcons.Image2Sprite.Create(@"FeatIcons/Icon_Casting_Combat.png"),
                                       FeatureGroup.Feat, compsArray);
            aa_feat.IsClassFeature = true;
            
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("9c928dc570bb9e54a9649b3ebfe47a41")); //RapidShotFeature
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("05a3b543b0a0a0346a5061e90f293f0b")); //PointBlankMaster
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("adf54af2a681792489826f7fd1b62889")); //Manyshot
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("f4201c85a991369408740c6888362e20")); //ImprovedCritical
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("46f970a6b9b5d2346b10892673fe6e74")); //ImprovedPreciseShot
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("38155ca9e4055bb48a89240a2055dcc3")); //AugmentSummoning
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("06964d468fde1dc4aa71a92ea04d930d")); //CombatCasting
            aa_feat.Features.AddToArray(library.Get<BlueprintFeatureSelection>("1c17446a3eb744f438488711b792ca4d")); //GreaterElementalFocusSelection z
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("1978c3f91cfbbc24b9c9b0d017f4beec")); //GreaterSpellPenetration
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("a1de1e4f92195b442adb946f0e2b9d4e")); //EmpowerSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("f180e72e4a9cbaa4da8be9bc958132ef")); //ExtendSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("2f5d1e705c7967546b72ad8218ccf99c")); //HeightenSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("7f2b282626862e345935bbea5e66424b")); //MaximizeSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("ef7ece7bb5bb66a41b256976b27f424e")); //QuickenSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("46fad72f54a33dc4692d3b62eca7bb78")); //ReachSpellFeat
            aa_feat.Features.AddToArray(library.Get<BlueprintFeatureSelection>("bb24cc01319528849b09a3ae8eec0b31")); //ElementalFocusSelection z
            aa_feat.Features.AddToArray(library.Get<BlueprintParametrizedFeature>("16fa59cc9a72a6043b566b49184f53fe")); //SpellFocus
            aa_feat.Features.AddToArray(library.Get<BlueprintFeature>("ee7dc126939e4d9438357fbd5980d459")); //SpellPenetration
            aa_feat.Features.AddToArray(library.Get<BlueprintParametrizedFeature>("f327a765a4353d04f872482ef3e48c35")); //SpellSpecializationFirst z

            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("9c928dc570bb9e54a9649b3ebfe47a41")); //RapidShotFeature
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("05a3b543b0a0a0346a5061e90f293f0b")); //PointBlankMaster
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("adf54af2a681792489826f7fd1b62889")); //Manyshot
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("f4201c85a991369408740c6888362e20")); //ImprovedCritical
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("46f970a6b9b5d2346b10892673fe6e74")); //ImprovedPreciseShot
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("38155ca9e4055bb48a89240a2055dcc3")); //AugmentSummoning
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("06964d468fde1dc4aa71a92ea04d930d")); //CombatCasting
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeatureSelection>("1c17446a3eb744f438488711b792ca4d")); //GreaterElementalFocusSelection z
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("1978c3f91cfbbc24b9c9b0d017f4beec")); //GreaterSpellPenetration
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("a1de1e4f92195b442adb946f0e2b9d4e")); //EmpowerSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("f180e72e4a9cbaa4da8be9bc958132ef")); //ExtendSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("2f5d1e705c7967546b72ad8218ccf99c")); //HeightenSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("7f2b282626862e345935bbea5e66424b")); //MaximizeSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("ef7ece7bb5bb66a41b256976b27f424e")); //QuickenSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("46fad72f54a33dc4692d3b62eca7bb78")); //ReachSpellFeat
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeatureSelection>("bb24cc01319528849b09a3ae8eec0b31")); //ElementalFocusSelection z
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintParametrizedFeature>("16fa59cc9a72a6043b566b49184f53fe")); //SpellFocus
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintFeature>("ee7dc126939e4d9438357fbd5980d459")); //SpellPenetration
            aa_feat.AllFeatures.AddToArray(library.Get<BlueprintParametrizedFeature>("f327a765a4353d04f872482ef3e48c35")); //SpellSpecializationFirst z
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

    public class EnhanceArrowsElemental : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        public BlueprintWeaponType[] weapon_types;
        public DamageEnergyType damage_type;

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            var bonus_damage = Helpers.CreateActionDealDamage(DamageEnergyType.Fire, Helpers.CreateContextDiceValue(DiceType.D6, Helpers.CreateContextValue(AbilityRankType.DamageBonus)));
            var bonus_damage_action = Helpers.CreateActionList(bonus_damage);
            if (!Array.Exists(weapon_types, t => t == evt.Weapon.Blueprint.Type))
            {
                return;
            }

            foreach (var e in evt.Weapon.Enchantments)
            {
                if (e.Blueprint.GetComponent<WeaponEnergyDamageDice>() != null) { if (e.Blueprint.GetComponent<WeaponEnergyDamageDice>().Element == damage_type) { return; } }
            }
            Log.Write("applying elemental damage bonus");
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

    }

}


