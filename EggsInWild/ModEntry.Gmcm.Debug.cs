#if WEGG_DEV
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Content;
using EggsInWild.Infrastructure.Content.Editors;
using EggsInWild.Infrastructure.Commands;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Harmony;
using EggsInWild.Infrastructure.DesertTomb.Loot;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private partial void DefineGmcmDebugPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
                    // ============================
                    // Debug subpages
                    // ============================

                    layout.Page("debug.desertTomb", page =>
                    {
                        page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningGeneration.label", "Tuning – Desert Tomb Generation"));

        page.Separator(simple: true);
        page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.force.label", "Force – Desert Tomb Handler"));
        page.Bool(cfg => cfg.DebugForceDesertTombHandler, optionId: "debug.desertTomb.forceHandler", tooltip: true);
        page.Bool(cfg => cfg.DebugForceDesertTombUnrestricted, optionId: "debug.desertTomb.forceUnrestricted", tooltip: true);


                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingDarkChance, optionId: "debug.tuning.desertTomb.gen.lightingDarkChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingDarkAltChance, optionId: "debug.tuning.desertTomb.gen.lightingDarkAltChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightingSpecialDarkChance, optionId: "debug.tuning.desertTomb.gen.lightingSpecialDarkChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesBaseNextMaxExclusive, optionId: "debug.tuning.desertTomb.gen.breakablesBaseNextMaxExclusive", tooltip: true, min: 1, max: 50, interval: 1);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesLuckScalar, optionId: "debug.tuning.desertTomb.gen.breakablesLuckScalar", tooltip: true, min: 0f, max: 100f, interval: 1f);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesMapAreaDivisor, optionId: "debug.tuning.desertTomb.gen.breakablesMapAreaDivisor", tooltip: true, min: 5, max: 400, interval: 1);

                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesEdgeTopChance, optionId: "debug.tuning.desertTomb.gen.breakablesEdgeTopChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesPlacementGuardMax, optionId: "debug.tuning.desertTomb.gen.breakablesPlacementGuardMax", tooltip: true, min: 50, max: 10000, interval: 10);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesPlaceChance, optionId: "debug.tuning.desertTomb.gen.breakablesPlaceChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Generation.BreakablesClusterChance, optionId: "debug.tuning.desertTomb.gen.breakablesClusterChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsMapAreaDivisor, optionId: "debug.tuning.desertTomb.gen.lightsMapAreaDivisor", tooltip: true, min: 50, max: 2000, interval: 10);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsTargetMin, optionId: "debug.tuning.desertTomb.gen.lightsTargetMin", tooltip: true, min: 0, max: 200, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsTargetMax, optionId: "debug.tuning.desertTomb.gen.lightsTargetMax", tooltip: true, min: 0, max: 300, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Generation.LightsAttemptsMultiplier, optionId: "debug.tuning.desertTomb.gen.lightsAttemptsMultiplier", tooltip: true, min: 1, max: 500, interval: 1);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningSpawning.label", "Tuning – Desert Tomb Spawning"));
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Spawning.GuardFraction, optionId: "debug.tuning.desertTomb.spawn.guardFraction", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Spawning.WanderPackChance, optionId: "debug.tuning.desertTomb.spawn.wanderPackChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.LeashRadiusMin, optionId: "debug.tuning.desertTomb.spawn.leashRadiusMin", tooltip: true, min: 0, max: 80, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.LeashRadiusMax, optionId: "debug.tuning.desertTomb.spawn.leashRadiusMax", tooltip: true, min: 1, max: 120, interval: 1);

                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolPointsMinInclusive, optionId: "debug.tuning.desertTomb.spawn.patrolPointsMinInclusive", tooltip: true, min: 1, max: 20, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolPointsMaxExclusive, optionId: "debug.tuning.desertTomb.spawn.patrolPointsMaxExclusive", tooltip: true, min: 2, max: 40, interval: 1);

                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolMaxAttempts, optionId: "debug.tuning.desertTomb.spawn.patrolMaxAttempts", tooltip: true, min: 10, max: 5000, interval: 10);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolAttemptsPerCandidate, optionId: "debug.tuning.desertTomb.spawn.patrolAttemptsPerCandidate", tooltip: true, min: 1, max: 50, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Spawning.PatrolMinSpacingTiles, optionId: "debug.tuning.desertTomb.spawn.patrolMinSpacingTiles", tooltip: true, min: 0, max: 20, interval: 1);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningPopulation.label", "Tuning – Desert Tomb Population"));
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Population.InitialWaveFraction, optionId: "debug.tuning.desertTomb.pop.initialWaveFraction", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.ReconcileIntervalTicks, optionId: "debug.tuning.desertTomb.pop.reconcileIntervalTicks", tooltip: true, min: 1, max: 600, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.MaxSpawnsPerTick, optionId: "debug.tuning.desertTomb.pop.maxSpawnsPerTick", tooltip: true, min: 0, max: 50, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.DesertTomb.Population.MinDistanceToPlayerTiles, optionId: "debug.tuning.desertTomb.pop.minDistanceToPlayerTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Bool(cfg => cfg.DebugKnobs.DesertTomb.Population.AllowRespawnAfterInitialFill, optionId: "debug.tuning.desertTomb.pop.allowRespawnAfterInitialFill", tooltip: true);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.desertTomb.tuningLuckLoot.label", "Tuning – Desert Tomb Luck / Loot"));
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.UnluckyDelta, optionId: "debug.tuning.desertTomb.luck.unluckyDelta", tooltip: true, min: -0.05f, max: 0f, interval: 0.0005f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.LuckyDelta, optionId: "debug.tuning.desertTomb.luck.luckyDelta", tooltip: true, min: 0f, max: 0.05f, interval: 0.0005f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.SuperLuckyDelta, optionId: "debug.tuning.desertTomb.luck.superLuckyDelta", tooltip: true, min: 0f, max: 0.5f, interval: 0.001f);

                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.SuperUnluckyMax, optionId: "debug.tuning.desertTomb.luck.superUnluckyMax", tooltip: true, min: -0.2f, max: 0f, interval: 0.005f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.UnluckyMax, optionId: "debug.tuning.desertTomb.luck.unluckyMax", tooltip: true, min: -0.2f, max: 0f, interval: 0.005f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.NeutralMax, optionId: "debug.tuning.desertTomb.luck.neutralMax", tooltip: true, min: 0f, max: 0.2f, interval: 0.005f);
                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.LootLuck.LuckyMax, optionId: "debug.tuning.desertTomb.luck.luckyMax", tooltip: true, min: 0f, max: 0.2f, interval: 0.005f);

                        page.Float(cfg => cfg.DebugKnobs.DesertTomb.Breakables.DropRateMultiplierMax, optionId: "debug.tuning.desertTomb.breakables.dropRateMultiplierMax", tooltip: true, min: 0f, max: 100f, interval: 0.5f);
                    });

                    layout.Page("debug.spawning", page =>
                    {
                        _devJsonTuning!.BuildSpawnChanceModifiersPage(page);

                        page.Separator(simple: true);
                        _devJsonTuning.BuildSpawnParametersPage(page);
                    });

                    layout.Page("debug.weighting", page =>
                    {
                        page.SectionLabel(() => Translate(i18n, "config.section.debug.weighting.tuningEconomy.label", "Tuning – Economy Weighting"));
                        page.Int(cfg => cfg.DebugKnobs.Weighting.HorizonDays, optionId: "debug.tuning.weighting.horizonDays", tooltip: true, min: 1, max: 60, interval: 1);
                        page.Float(cfg => cfg.DebugKnobs.Weighting.DeluxeProbability, optionId: "debug.tuning.weighting.deluxeProbability", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                        page.Float(cfg => cfg.DebugKnobs.Weighting.K, optionId: "debug.tuning.weighting.k", tooltip: true, min: 0f, max: 100000f, interval: 50f);
                        page.Float(cfg => cfg.DebugKnobs.Weighting.Alpha, optionId: "debug.tuning.weighting.alpha", tooltip: true, min: 0f, max: 2f, interval: 0.01f);

                        page.Float(cfg => cfg.DebugKnobs.Weighting.CapHigh, optionId: "debug.tuning.weighting.capHigh", tooltip: true, min: 0f, max: 100f, interval: 0.5f);
                        page.Float(cfg => cfg.DebugKnobs.Weighting.CapLow, optionId: "debug.tuning.weighting.capLow", tooltip: true, min: 0f, max: 200f, interval: 0.5f);

                        page.Float(cfg => cfg.DebugKnobs.Weighting.UnlockedFactor, optionId: "debug.tuning.weighting.unlockedFactor", tooltip: true, min: 0f, max: 2f, interval: 0.01f);
                        page.Float(cfg => cfg.DebugKnobs.Weighting.HiddenNoAnchorPenalty, optionId: "debug.tuning.weighting.hiddenNoAnchorPenalty", tooltip: true, min: 0f, max: 2f, interval: 0.01f);
                    });

                    layout.Page("debug.monsters", page =>
                    {
                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningPharaoh.label", "Tuning – Monsters (Pharaoh)"));
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.VisionTiles, optionId: "debug.tuning.monsters.pharaoh.visionTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.CastRangeTiles, optionId: "debug.tuning.monsters.pharaoh.castRangeTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RewrapRangeTiles, optionId: "debug.tuning.monsters.pharaoh.rewrapRangeTiles", tooltip: true, min: 0, max: 30, interval: 1);

                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.BaseCastCooldownMs, optionId: "debug.tuning.monsters.pharaoh.baseCastCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.EnragedCastCooldownMs, optionId: "debug.tuning.monsters.pharaoh.enragedCastCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.CastWindupMs, optionId: "debug.tuning.monsters.pharaoh.castWindupMs", tooltip: true, min: 0, max: 10000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RecoverMs, optionId: "debug.tuning.monsters.pharaoh.recoverMs", tooltip: true, min: 0, max: 10000, interval: 50);

                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.MicroUnstuckAfterTicks, optionId: "debug.tuning.monsters.pharaoh.microUnstuckAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.PathfindUnstuckAfterTicks, optionId: "debug.tuning.monsters.pharaoh.pathfindUnstuckAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);

                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.SandStepAfterTicks, optionId: "debug.tuning.monsters.pharaoh.sandStepAfterTicks", tooltip: true, min: 0, max: 600, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.SandStepCooldownMs, optionId: "debug.tuning.monsters.pharaoh.sandStepCooldownMs", tooltip: true, min: 0, max: 30000, interval: 100);

                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.LeashRadiusTiles, optionId: "debug.tuning.monsters.pharaoh.leashRadiusTiles", tooltip: true, min: 0, max: 60, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.LeashTicksBeforeReturn, optionId: "debug.tuning.monsters.pharaoh.leashTicksBeforeReturn", tooltip: true, min: 0, max: 2000, interval: 5);

                        page.Float(cfg => cfg.DebugKnobs.Monsters.Pharaoh.RewrapChance, optionId: "debug.tuning.monsters.pharaoh.rewrapChance", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.EnrageCooldownCapMs, optionId: "debug.tuning.monsters.pharaoh.enrageCooldownCapMs", tooltip: true, min: 0, max: 5000, interval: 25);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.ReviveTimerMax, optionId: "debug.tuning.monsters.pharaoh.reviveTimerMax", tooltip: true, min: 0, max: 5000, interval: 25);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.HealEnraged, optionId: "debug.tuning.monsters.pharaoh.healEnraged", tooltip: true, min: 0, max: 1000, interval: 5);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Pharaoh.HealNormal, optionId: "debug.tuning.monsters.pharaoh.healNormal", tooltip: true, min: 0, max: 1000, interval: 5);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningSandi.label", "Tuning – Monsters (Sandi)"));
                        page.Float(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveChanceOnHit, optionId: "debug.tuning.monsters.sandi.diveChanceOnHit", tooltip: true, min: 0f, max: 1f, interval: 0.01f);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveDurationMs, optionId: "debug.tuning.monsters.sandi.diveDurationMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Sandi.DiveCooldownMs, optionId: "debug.tuning.monsters.sandi.diveCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningCrawler.label", "Tuning – Monsters (Crawler)"));
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlTriggerDistanceTiles, optionId: "debug.tuning.monsters.crawler.curlTriggerDistanceTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlDurationMs, optionId: "debug.tuning.monsters.crawler.curlDurationMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Crawler.CurlCooldownMs, optionId: "debug.tuning.monsters.crawler.curlCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningHurricane.label", "Tuning – Monsters (Hurricane)"));
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.AlignTiles, optionId: "debug.tuning.monsters.hurricane.alignTiles", tooltip: true, min: 0, max: 60, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SuckDistanceTiles, optionId: "debug.tuning.monsters.hurricane.suckDistanceTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SwirlSegmentMs, optionId: "debug.tuning.monsters.hurricane.swirlSegmentMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.ChargeMs, optionId: "debug.tuning.monsters.hurricane.chargeMs", tooltip: true, min: 0, max: 20000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Hurricane.SuckMs, optionId: "debug.tuning.monsters.hurricane.suckMs", tooltip: true, min: 0, max: 20000, interval: 50);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.tuningMatango.label", "Tuning – Monsters (Matango)"));
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.VisionTiles, optionId: "debug.tuning.monsters.matango.visionTiles", tooltip: true, min: 0, max: 30, interval: 1);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.CastWindupMs, optionId: "debug.tuning.monsters.matango.castWindupMs", tooltip: true, min: 0, max: 10000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.RecoverMs, optionId: "debug.tuning.monsters.matango.recoverMs", tooltip: true, min: 0, max: 10000, interval: 50);
                        page.Int(cfg => cfg.DebugKnobs.Monsters.Matango.ShotCooldownMs, optionId: "debug.tuning.monsters.matango.shotCooldownMs", tooltip: true, min: 0, max: 20000, interval: 50);

                        page.Separator(simple: true);

                        page.SectionLabel(() => Translate(i18n, "config.section.debug.monsters.jsonMonsterDefinitions.label", "JSON – MonsterDefinitions (Dev)"));
                        page.ParagraphDynamic(
                            text: () => Translate(i18n, "config.paragraph.debug.monsters.jsonMonsterDefinitions.notice.text", "Dev-only: edits assets/DesertTomb/MonsterDefinitions.json directly. Stats/Flags are written immediately. (Tip: restart or reload Tomb content to see changes.)"),
                            id: "json_monsters_notice"
                        );
                        page.ParagraphDynamic(
                            text: () => Translate(i18n, "config.paragraph.debug.monsters.jsonMonsterDefinitions.fields.text", "Fields: DifficultyAdd and SpeedAdd are additive adjustments; MaxHealthMultiplier/Add adjust max HP; DamageToFarmer sets base damage; ResilienceAdd adjusts resilience; IsBoss and HostAuthoritative are behavior flags."),
                            id: "json_monsters_fields"
                        );

                        foreach (var key in DevMonsterDefinitions.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                        {
                            var monsterKey = key;
                            var def = DevMonsterDefinitions[monsterKey] ?? new TombMonsterDefinitionJson();
                            def.Stats ??= new TombMonsterStatProfileJson();
                            def.Flags ??= new TombMonsterBehaviorFlagsJson();
                            DevMonsterDefinitions[monsterKey] = def;

                            page.Separator(simple: true, fieldId: $"json_monsters_sep_{NormalizeDevJsonKey(monsterKey)}");
                            page.SectionLabel(() => monsterKey);

                            page.Int(
                                optionId: $"JSON {monsterKey}: DifficultyAdd",
                                getValue: () => def.Stats!.DifficultyAdd ?? 0,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.DifficultyAdd = v),
                                tooltip: false,
                                min: -10,
                                max: 50,
                                interval: 1,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_difficultyAdd"
                            );

                            page.Int(
                                optionId: $"JSON {monsterKey}: SpeedAdd",
                                getValue: () => def.Stats!.SpeedAdd ?? 0,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.SpeedAdd = v),
                                tooltip: false,
                                min: -10,
                                max: 20,
                                interval: 1,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_speedAdd"
                            );

                            page.Float(
                                optionId: $"JSON {monsterKey}: MaxHealthMultiplier",
                                getValue: () => def.Stats!.MaxHealthMultiplier ?? 1f,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.MaxHealthMultiplier = Math.Max(0f, v)),
                                tooltip: false,
                                min: 0f,
                                max: 10f,
                                interval: 0.05f,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_maxHealthMultiplier"
                            );

                            page.Int(
                                optionId: $"JSON {monsterKey}: MaxHealthAdd",
                                getValue: () => def.Stats!.MaxHealthAdd ?? 0,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.MaxHealthAdd = v),
                                tooltip: false,
                                min: -500,
                                max: 5000,
                                interval: 10,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_maxHealthAdd"
                            );

                            page.Int(
                                optionId: $"JSON {monsterKey}: DamageToFarmer",
                                getValue: () => def.Stats!.DamageToFarmer ?? 0,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.DamageToFarmer = Math.Max(0, v)),
                                tooltip: false,
                                min: 0,
                                max: 500,
                                interval: 1,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_damageToFarmer"
                            );

                            page.Int(
                                optionId: $"JSON {monsterKey}: ResilienceAdd",
                                getValue: () => def.Stats!.ResilienceAdd ?? 0,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Stats!.ResilienceAdd = v),
                                tooltip: false,
                                min: -50,
                                max: 100,
                                interval: 1,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_resilienceAdd"
                            );

                            page.Bool(
                                optionId: $"JSON {monsterKey}: IsBoss",
                                getValue: () => def.Flags!.IsBoss ?? false,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Flags!.IsBoss = v),
                                tooltip: false,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_isBoss"
                            );

                            page.Bool(
                                optionId: $"JSON {monsterKey}: HostAuthoritative",
                                getValue: () => def.Flags!.HostAuthoritative ?? true,
                                setValue: v => MutateDevMonsterDefinitions(() => def.Flags!.HostAuthoritative = v),
                                tooltip: false,
                                fieldId: $"json_monsters_{NormalizeDevJsonKey(monsterKey)}_hostAuthoritative"
                            );
                        }
                    });

                    // Debug hub page
                    layout.Page("debug", page =>
                    {
                        page.Bool(cfg => cfg.DesertTombPharaohTrackerEnabled, optionId: "debug.desertTomb.pharaohTracker", tooltip: true);
                        page.Bool(cfg => cfg.DesertTombDebugLogging, optionId: "debug.desertTomb.traceLogging", tooltip: true);
                        page.Bool(cfg => cfg.DebugForceDesertTombHandler, optionId: "debug.desertTomb.forceHandler", tooltip: true);
                        page.Bool(cfg => cfg.DebugForceDesertTombUnrestricted, optionId: "debug.desertTomb.forceUnrestricted", tooltip: true);
                        page.Bool(cfg => cfg.AnimalNotesDebugMode, optionId: "debug.animalNotes.debugMode", tooltip: true);

                        page.Separator(simple: true);
                        page.Link("debug.desertTomb", tooltip: true);
                        page.Link("debug.spawning", tooltip: true);
                        page.Link("debug.weighting", tooltip: true);
                        page.Link("debug.monsters", tooltip: true);
                    });
    }
}
#endif
