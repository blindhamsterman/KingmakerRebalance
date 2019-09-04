﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallOfTheWild
{
    class ChannelEnergyEngine
    {
        [Flags]
        public enum ChannelType
        {
            PositiveHeal = 1,
            PositiveHarm = 2,
            NegativeHarm = 4,
            NegativeHeal = 8
        }

        public class ChannelEntry
        {
            public BlueprintAbility ability;
            public BlueprintFeature parent_feature;

            public ChannelEntry(string ability_guid, string parent_feature_guid)
            {
                ability = library.Get<BlueprintAbility>(ability_guid);
                parent_feature = library.Get<BlueprintFeature>(parent_feature_guid);
            }


            public ChannelEntry(BlueprintAbility channel_ability, BlueprintFeature channel_parent_feature)
            {
                ability = channel_ability;
                parent_feature = channel_parent_feature;
            }
        }



        static internal LibraryScriptableObject library => Main.library;
        static List<ChannelEntry> positive_heal = new List<ChannelEntry>{new ChannelEntry("574cf074e8b65e84d9b69a8c6f1af27b","7d49d7f590dc9a948b3bd1c8b7979854"), //empyreal heal
                                                                         new ChannelEntry("6670f0f21a1d7f04db2b8b115e8e6abf", "cb6d55dda5ab906459d18a435994a760"), //paladin heal
                                                                         new ChannelEntry("0c0cf7fcb356d2448b7d57f2c4db3c0c", "a9ab1bbc79ecb174d9a04699986ce8d5"), //hospitalier heal
                                                                         new ChannelEntry("f5fc9a1a2a3c1a946a31b320d1dd31b2", "a79013ff4bcd4864cb669622a29ddafb") }; //cleric heal

        static List<ChannelEntry> positive_harm = new List<ChannelEntry>{new ChannelEntry("e1536ee240c5d4141bf9f9485a665128","7d49d7f590dc9a948b3bd1c8b7979854"), //empyreal_harm
                                                                         new ChannelEntry("4937473d1cfd7774a979b625fb833b47", "cb6d55dda5ab906459d18a435994a760"), //paladin harm
                                                                         new ChannelEntry("cc17243b2185f814aa909ac6b6599eaa", "a9ab1bbc79ecb174d9a04699986ce8d5"), //hospitalier harm
                                                                         new ChannelEntry("279447a6bf2d3544d93a0a39c3b8e91d", "a79013ff4bcd4864cb669622a29ddafb") }; //cleric harm
        
        static List<ChannelEntry> negative_heal = new List<ChannelEntry> { new ChannelEntry("9be3aa47a13d5654cbcb8dbd40c325f2", "3adb2c906e031ee41a01bfc1d5fb7eea") };
        static List<ChannelEntry> negative_harm = new List<ChannelEntry> { new ChannelEntry("89df18039ef22174b81052e2e419c728", "3adb2c906e031ee41a01bfc1d5fb7eea") };

        static BlueprintFeature selective_channel = library.Get<BlueprintFeature>("fd30c69417b434d47b6b03b9c1f568ff");

        static Dictionary<string, string> normal_quick_channel_map = new Dictionary<string, string>();


        static public BlueprintFeature quick_channel = null;
        static public BlueprintFeature channel_smite = null;


        public static void createChannelSmite()
        {
            channel_smite = Helpers.CreateFeature("ChannelSmiteFeature",
                                      "Channel Smite",
                                      "Before you make a melee attack roll, you can choose to spend one use of your channel energy ability as a swift action. If you channel positive energy and you hit an undead creature, that creature takes an amount of additional damage equal to the damage dealt by your channel positive energy ability. If you channel negative energy and you hit a living creature, that creature takes an amount of additional damage equal to the damage dealt by your channel negative energy ability. Your target can make a Will save, as normal, to halve this additional damage. If your attack misses, the channel energy ability is still expended with no effect.",
                                      "",
                                      null,
                                      FeatureGroup.Feat);
            channel_smite.Groups = channel_smite.Groups.AddToArray(FeatureGroup.CombatFeat);

            foreach (var e in positive_harm.ToArray())
            {
                addToChannelSmite(e.ability, e.parent_feature, ChannelType.PositiveHarm);
            }

            foreach (var e in negative_harm.ToArray())
            {
                addToChannelSmite(e.ability, e.parent_feature, ChannelType.NegativeHarm);
            }


            library.AddCombatFeats(channel_smite);
        }

        static void addToChannelSmite(BlueprintAbility channel, BlueprintFeature parent_feature, ChannelType channel_type)
        {
            if (channel_smite == null)
            {
                return;
            }

            if (channel_type == ChannelType.NegativeHeal || channel_type == ChannelType.PositiveHeal)
            {
                return;
            }

            Common.addFeaturePrerequisiteOr(channel_smite, parent_feature);

            var smite_evil = library.Get<BlueprintAbility>("7bb9eb2042e67bf489ccd1374423cdec");
            var buff = Helpers.CreateBuff("ChannelSmite" + channel.name + "Buff",
                                          $"Channel Smite ({channel.Name})",
                                          channel_smite.Description,
                                          Helpers.MergeIds(channel.AssetGuid, "0d406cf592524c85b796216ed4ee3ab3"),
                                          channel.Icon,
                                          null,
                                          Common.createAddInitiatorAttackWithWeaponTrigger(channel.GetComponent<AbilityEffectRunAction>().Actions,
                                                                                           check_weapon_range_type: true),
                                          Common.createAddInitiatorAttackWithWeaponTrigger(Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>()),
                                                                                           check_weapon_range_type: true,
                                                                                           only_hit: false,
                                                                                           on_initiator: true),
                                          channel.GetComponent<ContextRankConfig>()
                                          );

            var apply_buff = Common.createContextActionApplyBuff(buff,
                                                                 Helpers.CreateContextDuration(Common.createSimpleContextValue(1), Kingmaker.UnitLogic.Mechanics.DurationRate.Rounds),
                                                                 dispellable: false
                                                                 );
            var ability = Helpers.CreateAbility("ChannelSmite" + channel.name,
                                                buff.Name,
                                                buff.Description,
                                                Helpers.MergeIds(channel.AssetGuid, "81e5fc81f1a644d5898a9fdbda752e95"),
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift,
                                                AbilityRange.Personal,
                                                Helpers.oneRoundDuration,
                                                channel.LocalizedSavingThrow,
                                                smite_evil.GetComponent<AbilitySpawnFx>(),
                                                channel.GetComponent<AbilityResourceLogic>(),
                                                Helpers.CreateRunActions(apply_buff)
                                                );
            ability.setMiscAbilityParametersSelfOnly();
            updateItemsForChannelDerivative(channel, ability);

            var caster_alignment = channel.GetComponent<AbilityCasterAlignment>();
            if (caster_alignment != null)
            {
                ability.AddComponent(caster_alignment);
            }

            channel_smite.AddComponent(Common.createAddFeatureIfHasFact(parent_feature, ability));
            parent_feature.AddComponent(Common.createAddFeatureIfHasFact(channel_smite, ability));
        }



        public static void createQuickChannel()
        {
            quick_channel = Helpers.CreateFeature("QuickChannelFeature",
                                                  "Quick Channel",
                                                  "You may channel energy as a move action by spending 2 daily uses of that ability.",
                                                  "",
                                                  LoadIcons.Image2Sprite.Create(@"FeatIcons/Icon_Channel_Quick.png"),
                                                  FeatureGroup.Feat,
                                                  Helpers.PrerequisiteStatValue(Kingmaker.EntitySystem.Stats.StatType.SkillLoreReligion, 5));
            foreach (var e in positive_heal.ToArray())
            {
                addToQuickChannel(e.ability, e.parent_feature, ChannelType.PositiveHeal);
            }

            foreach (var e in positive_harm.ToArray())
            {
                addToQuickChannel(e.ability, e.parent_feature, ChannelType.PositiveHarm);
            }

            foreach (var e in negative_harm.ToArray())
            {
                addToQuickChannel(e.ability, e.parent_feature, ChannelType.NegativeHarm);
            }

            foreach (var e in negative_heal.ToArray())
            {
                addToQuickChannel(e.ability, e.parent_feature, ChannelType.NegativeHeal);
            }

            library.AddFeats(quick_channel);
        }


        static void addToQuickChannel(BlueprintAbility channel, BlueprintFeature parent_feature, ChannelType channel_type)
        {
            if (quick_channel == null)
            {
                return;
            }

            Common.addFeaturePrerequisiteOr(quick_channel, parent_feature);

            var quicken_ability = library.CopyAndAdd<BlueprintAbility>(channel.AssetGuid, "Quick" + channel.name, channel.AssetGuid, "e936d73a1dfe42efb1765b980c80e113");
            quicken_ability.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move;
            quicken_ability.SetName(quicken_ability.Name + $" ({quick_channel.Name})");
            var resource_logic = quicken_ability.GetComponent<AbilityResourceLogic>();
            var amount = resource_logic.Amount;
            quicken_ability.ReplaceComponent<AbilityResourceLogic>(c => { c.Amount = amount * 2;});
            updateItemsForChannelDerivative(channel, quicken_ability);

            var quicken_feature = Common.AbilityToFeature(quicken_ability, guid: Helpers.MergeIds(quicken_ability.AssetGuid, parent_feature.AssetGuid));
            quick_channel.AddComponent(Common.createAddFeatureIfHasFact(parent_feature, quicken_feature));
            parent_feature.AddComponent(Common.createAddFeatureIfHasFact(quick_channel, quicken_feature));

            storeChannel(quicken_ability, parent_feature, channel_type);

            normal_quick_channel_map.Add(channel.AssetGuid, quicken_ability.AssetGuid);
        }


        public static BlueprintAbility getQuickChannelVariant(BlueprintAbility normal_channel_ability)
        {
            if (quick_channel == null)
            {
                return null;
            }
            return library.Get<BlueprintAbility>(normal_quick_channel_map[normal_channel_ability.AssetGuid]);
        }

        public static BlueprintAbility createChannelEnergy(ChannelType channel_type, string name, string guid, BlueprintFeature parent_feature, 
                                                           ContextRankConfig rank_config = null, AbilityResourceLogic resource_logic = null, bool update_items = false)
        {
            string original_guid = "";
            BlueprintAbility prototype = null;
            switch (channel_type)
            {
                case ChannelType.PositiveHeal:
                    original_guid = "f5fc9a1a2a3c1a946a31b320d1dd31b2";
                    prototype = positive_heal[0].ability;
                    break;
                case ChannelType.PositiveHarm:
                    original_guid = "279447a6bf2d3544d93a0a39c3b8e91d";
                    prototype = positive_harm[0].ability;
                    break;
                case ChannelType.NegativeHarm:
                    original_guid = "89df18039ef22174b81052e2e419c728";
                    prototype = negative_harm[0].ability;
                    break;
                case ChannelType.NegativeHeal:
                    original_guid = "9be3aa47a13d5654cbcb8dbd40c325f2";
                    prototype = negative_heal[0].ability;
                    break;
            }

            var ability = library.CopyAndAdd<BlueprintAbility>(original_guid, name, guid);

            if (rank_config != null)
            {
                ability.ReplaceComponent<ContextRankConfig>(rank_config);
            }

            if (resource_logic != null)
            {
                ability.ReplaceComponent<AbilityResourceLogic>(resource_logic);
            }

            if (update_items)
            {
                updateItemsForChannelDerivative(ability, prototype);
            }


            storeChannel(ability, parent_feature, channel_type);
            addToChannelSmite(ability, parent_feature, channel_type);
            addToQuickChannel(ability, parent_feature, channel_type);
            addToSelectiveChannel(parent_feature);

            return ability;
        }


        static void storeChannel(BlueprintAbility ability, BlueprintFeature parent_feature, ChannelType channel_type)
        {
            switch (channel_type)
            {
                case ChannelType.PositiveHeal:
                    positive_heal.Add(new ChannelEntry(ability, parent_feature));
                    break;
                case ChannelType.PositiveHarm:
                    positive_harm.Add(new ChannelEntry(ability, parent_feature));
                    break;
                case ChannelType.NegativeHarm:
                    negative_harm.Add(new ChannelEntry(ability, parent_feature));
                    break;
                case ChannelType.NegativeHeal:
                    negative_heal.Add(new ChannelEntry(ability, parent_feature));
                    break;
            }
        }


        static void addToSelectiveChannel(BlueprintFeature parent_feature)
        {
            selective_channel.AddComponent(Helpers.PrerequisiteFeature(parent_feature, true));
        }


        public static BlueprintFeature createExtraChannelFeat(BlueprintAbility ability, BlueprintFeature parent_feature, string name, string display_name, string guid)
        {
            var extra_channel = library.CopyAndAdd<BlueprintFeature>("cd9f19775bd9d3343a31a065e93f0c47", name, guid);
            extra_channel.ReplaceComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature>(Helpers.PrerequisiteFeature(parent_feature));
            extra_channel.SetName(display_name);

            var resource_logic = ability.GetComponent<AbilityResourceLogic>();
            var resource = resource_logic.RequiredResource;
            var amount = resource_logic.Amount;
            extra_channel.ReplaceComponent<IncreaseResourceAmount>(c => { c.Value = amount * 2; c.Resource = resource; });
            extra_channel.ReplaceComponent<PrerequisiteFeature>(c => { c.Feature = parent_feature; });

            library.AddFeats(extra_channel);
            return extra_channel;
        }


        static public void updateItemsFeature(ChannelType channel_type, BlueprintFeature feature)
        {
            //phylacteries bonuses
            var negative_bonus1 = library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("60f06749fa4729c49bc3eb2eb7e3b316");
            var positive_bonus1 = library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("f5d0bf8c1b4574848acb8d1fbb544807");
            var negative_bonus2 = library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("cb4a39044b59f5e47ad5bc08ff9d6669");
            var positive_bonus2 = library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("e988cf802d403d941b2ed8b6016de68f");

            var linnorm_buff = library.Get<BlueprintBuff>("b5ebb94df76531c4ca4f13bfd91efd4e");

            if ((channel_type | ChannelType.PositiveHeal) >0 || (channel_type | ChannelType.PositiveHarm) >0)
            {
                Common.addFeatureToEnchantment(positive_bonus1, feature);
                Common.addFeatureToEnchantment(positive_bonus2, feature);
                Common.addFeatureToEnchantment(positive_bonus2, feature);
                linnorm_buff.AddComponent(Helpers.CreateAddFact(feature));
                linnorm_buff.AddComponent(Helpers.CreateAddFact(feature));
            }
            else if ((channel_type | ChannelType.NegativeHarm) > 0 || (channel_type | ChannelType.NegativeHeal) > 0)
            {
                Common.addFeatureToEnchantment(negative_bonus1, feature);
                Common.addFeatureToEnchantment(negative_bonus2, feature);
                Common.addFeatureToEnchantment(negative_bonus2, feature);
            }
        }


        


        static internal void updateItemsForChannelDerivative(BlueprintAbility original_ability, BlueprintAbility derived_ability)
        {
            //phylacteries bonuses
            BlueprintEquipmentEnchantment[] enchants = new BlueprintEquipmentEnchantment[]{library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("60f06749fa4729c49bc3eb2eb7e3b316"),
                                                                                  library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("f5d0bf8c1b4574848acb8d1fbb544807"),
                                                                                  library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("cb4a39044b59f5e47ad5bc08ff9d6669"),
                                                                                  library.Get<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment>("e988cf802d403d941b2ed8b6016de68f"),
                                                                                 };

            foreach (var e in enchants)
            {
                var boni = e.GetComponents<AddCasterLevelEquipment>().ToArray();
                foreach (var b in boni)
                {
                    if (b.Spell == original_ability)
                    {
                        var b2 = b.CreateCopy();
                        b2.Spell = derived_ability;
                        e.AddComponent(b2);
                    }
                }
            }


            BlueprintBuff[] buffs = new BlueprintBuff[] { library.Get<BlueprintBuff>("b5ebb94df76531c4ca4f13bfd91efd4e") };

            foreach (var buff in buffs)
            {
                var boni = buff.GetComponents<AddCasterLevelForAbility>().ToArray();
                foreach (var b in boni)
                {
                    if (b.Spell == original_ability)
                    {
                        var b2 = b.CreateCopy();
                        b2.Spell = derived_ability;
                        buff.AddComponent(b2);
                    }
                }
            }

        }

    }
}
