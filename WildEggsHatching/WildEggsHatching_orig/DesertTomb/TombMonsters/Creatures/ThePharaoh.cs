using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;
using WildEggsHatching.DesertTomb.Utilities;
using WildEggsHatching.Management;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb;

public class ThePharaoh : Monster
{
    private const int DIRECTION_UP = 0;
    private const int DIRECTION_DOWN = 2;
    private const int DIRECTION_LEFT = 3;
    private const int DIRECTION_RIGHT = 1;

    // Configuration-based properties (using all properties from ThePharaohConfiguration)
    private static ThePharaohConfiguration Config =>
        ModEntry.configManager?.SpawnConfiguration?.ThePharaoh ?? new ThePharaohConfiguration();

    // Healing properties
    private float PercentageHealAmount => Config.HealPercent;
    private float SelfCooldownValue => Config.SelfHealCooldown;
    private float SelfCooldownMultiplier => Config.CooldownMultiplier;
    private float MinHealthRatioToHeal => Config.MinHealHealth;
    private float HealthRatioToSelfHeal => Config.SelfHealHealth;
    private float HealingIncrementRate => Config.HealGrowthRate;
    private float InitialHealPercentage => Config.InitialHealPercent;

    // Combat properties
    private float CooldownValue => Config.AttackCooldown;
    private float RandomCastChance => Config.RandomCastChance;
    public float FadingTime => Config.FadeDuration;
    private float AttackInterval => Config.AttackInterval;
    private float FreezeChance => Config.FreezeChance;
    private float FreezeChanceProximityBonus => Config.CloseRangeFreezeBonus;

    // State transition properties
    private float HealthRatioToEnterMageMode => Config.MageModeHealth;
    private float HealthRatioToExitMageMode => Config.MageModeExitHealth;
    private float HealthBoostOnFirstMageMode => Config.FirstTransformHealthBoost;

    // Vision and movement properties
    public int VisionDistance => Config.VisionRange;
    private int MaxFleeAttempts => Config.MaxFleeAttempts;
    private float FleeDistanceMultiplier => Config.FleeDistanceScale;
    private float AddedSpeedFleeing => Config.FleeSpeedBonus;

    // Chance properties
    private double AttackStackMissPercentage => Config.AccuracyLossPerStack;
    private double ClosenessStackPercentage => Config.AggroStackRate;
    private double ClosenessFleeingChance => Config.ProximityEscapeChance;
    private double CorneredEscapeChance => Config.DesperateEscapeChance;
    private double ChanceDisappearAfterFleeing => Config.VanishChance;

    // Threshold properties
    private int MaxInterruptAttempts => Config.MaxInterruptions;
    private int MonsterSearchRadius => Config.AllyRange;
    private int MaxMissStacks => Config.MaxAccuracyStacks;
    private int MaxClosenessStacks => Config.MaxAggroStacks;

    // Miss chance properties
    private float MissChanceBase => Config.BaseMissChance;
    private float MissChanceSelfHealingMultiplier => Config.HealAccuracyPenalty;
    private float MissChanceFadingMultiplier => Config.FadeMissBonus;

    // Visual properties
    private float WarriorScale => Config.WarriorScale;
    private float MageScale => Config.MageScale;
    private readonly float CLOSENESS_INTERVAL = 1500f;

    private State currentState;
    private State previousState;
    private State? nextStateAfterDamage;
    private readonly State[] staticStates =
    {
        State.SelfHealing,
        State.FleeingCorner,
        State.Disappearing,
        State.Appearing,
    };
    private readonly State[] fadingStates =
    {
        State.FleeingCorner,
        State.Disappearing,
        State.Appearing,
    };

    private enum State
    {
        Default,
        Casting,
        EnteringMage,
        SelfHealing,
        Fleeing,
        FleeingCorner,
        ExitingMage,
        Disappearing,
        Appearing,
    }

    private bool isMage = false;
    private bool isFirstMageMode = true;
    private bool isHealing = false;
    private bool isInterruptedHealing = false;
    private readonly NetBool casting = new NetBool();
    private float rotationTimer = 0;
    public float fadingTimer = 0;
    private float lastAttackTime = 0;
    private (bool result, float time) lastClosenessCheck = (false, 0);
    private double percentageToHeal = 0;
    private int interruptedHealingCount = 0;
    private int FleeAttempts = 0;
    private int missAtackStacks = 0;
    private int closenessStacks = 0;
    private (int min, int max) playerThreshold;
    private Vector2? monsterPreviousPosition = null;

    private string fleeingCause = "";
    private string combatModeCause = "";
    private float selfCoolDown;
    private float coolDown;

    // Constructor for regular Pharaoh
    public ThePharaoh(Vector2 position)
        : base("The Pharaoh", position)
    {
        InitializePharaoh();
    }

    // Constructor for Tomb Pharaoh with difficulty manager
    internal ThePharaoh(
        MonsterMultipliers monsterMultipliers,
        Vector2 position,
        int difficulty = 4,
        string textureName = "ThePharaoh",
        int speedToAdd = 0
    )
        : base("The Pharaoh", position)
    {
        InitializePharaoh();
        SetupTombPharaoh(difficulty, speedToAdd);
    }

    private void InitializePharaoh()
    {
        InitializeSprite();
        missChance.Value = MissChanceBase;
        isMage = false;
        willDestroyObjectsUnderfoot = false;
        Scale = WarriorScale;
        coolDown = 0;
        selfCoolDown = 0;
    }

    private void SetupTombPharaoh(int difficulty, int speedToAdd)
    {
        // Use the TombMonsterBuilder for common setup
        TombMonsterBuilder.SetupBasicMonster(this, difficulty, "ThePharaoh", speedToAdd);

        // Set tomb-specific drop items
        objectsToDrop.Clear();
        var dropItems = MonsterItemHelper.GetModifiedMonsterItems(
            TombMonsterType.TheTombPharaoh,
            "261 .01 124 0.008 125 0.008 275 0.006 791 0.005 166 0.004 (H)MummyMask 0.002"
        );
        objectsToDrop.AddRange(dropItems);

        // Add Osiris Scroll if needed
        if (!ProgressionManager.HasScrollOrCompletedQuest)
        {
            objectsToDrop.Add(IDs.Objects.OsirisScroll);
        }

        Name = "The Pharaoh";
        displayName = "The Pharaoh";
    }

    public ThePharaoh() { }

    private void InitializeSprite()
    {
        Sprite.UpdateSourceRect();
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        base.NetFields.AddField(casting, "casting");
    }

    private float HealthPercentage() => this.Health / this.MaxHealth;

    /// <inheritdoc />
    public override void draw(SpriteBatch b)
    {
        base.draw(b);
        if (currentState == State.Casting)
        {
            Vector2 drawPosition = Game1.GlobalToLocal(Game1.viewport, getStandingPosition());
            Vector2 origin = new(8f, 8f);
            float scale = 3f;

            for (int i = 0; i < 8; i++)
            {
                float rotation = rotationTimer + (float)i * (float)Math.PI / 4f;
                Vector2 offset = new Vector2(
                    (float)Math.Cos(rotation) * 32f,
                    (float)Math.Sin(rotation) * 32f
                );

                b.Draw(
                    Game1.objectSpriteSheet,
                    drawPosition + offset,
                    new Rectangle(16, 592, 16, 16),
                    Color.White * 0.8f,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0.95f
                );
            }

            // Central glow effect
            b.Draw(
                Game1.objectSpriteSheet,
                drawPosition,
                new Rectangle(16, 592, 16, 16),
                Color.Purple * 0.6f,
                rotationTimer,
                origin,
                scale * 1.5f,
                SpriteEffects.None,
                0.96f
            );
        }
    }

    public override int takeDamage(
        int damage,
        int xTrajectory,
        int yTrajectory,
        bool isBomb,
        double addedPrecision,
        Farmer who
    )
    {
        LoggerHelper.VerboseLog(
            $"takeDamage called with parameters: damage={damage}, xTrajectory={xTrajectory}, yTrajectory={yTrajectory}, isBomb={isBomb}, addedPrecision={addedPrecision}",
            LogLevel.Debug,
            false
        );

        if (IsFleeingCorner())
        {
            LoggerHelper.VerboseLog("Fleeing corner detected, returning -1", LogLevel.Warn);
            return -1;
        }

        UpdateAttackTime(isBomb);
        LoggerHelper.VerboseLog(
            $"Attack time updated. Last attack time: {lastAttackTime}, Miss attack stacks: {missAtackStacks}",
            LogLevel.Debug
        );

        int actualDamage = CalculateActualDamage(damage);
        double missChance = CalculateMissChance();
        LoggerHelper.VerboseLog(
            $"Calculated actual damage: {actualDamage}, Miss chance: {missChance}",
            LogLevel.Debug
        );

        if (IsMiss(missChance, addedPrecision))
        {
            LoggerHelper.VerboseLog("Attack missed", LogLevel.Debug);
            HandleMiss(ref actualDamage);
        }
        else
        {
            LoggerHelper.VerboseLog("Attack hit", LogLevel.Debug);
            HandleHit(ref actualDamage, xTrajectory, yTrajectory);
        }

        LoggerHelper.VerboseLog($"Next State is set to: {nextStateAfterDamage}", LogLevel.Debug);

        return actualDamage;
    }

    protected override void sharedDeathAnimation()
    {
        Point standingPixel = base.StandingPixel;
        Game1.createRadialDebris(
            base.currentLocation,
            Sprite.textureName.Value,
            new Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, 16, 5),
            16,
            standingPixel.X,
            standingPixel.Y - 32,
            1,
            standingPixel.Y / 64,
            Color.White
        );
        Game1.createRadialDebris(
            base.currentLocation,
            Sprite.textureName.Value,
            new Rectangle(Sprite.SourceRect.X + 2, Sprite.SourceRect.Y + 5, 16, 5),
            10,
            standingPixel.X,
            standingPixel.Y - 32,
            1,
            standingPixel.Y / 64,
            Color.White
        );
        Game1.createRadialDebris(
            base.currentLocation,
            Sprite.textureName.Value,
            new Rectangle(0, 10, 16, 5),
            16,
            standingPixel.X,
            standingPixel.Y - 32,
            1,
            standingPixel.Y / 64,
            Color.White
        );
    }

    protected override void localDeathAnimation()
    {
        Utility.makeTemporarySpriteJuicier(
            new TemporaryAnimatedSprite(45, base.Position, Color.White, 10),
            base.currentLocation
        );
        for (int i = 1; i < 3; i++)
        {
            base.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    6,
                    base.Position + new Vector2(1f, 1f) * 64f * i,
                    Color.Gray * 0.75f,
                    10
                )
                {
                    delayBeforeAnimationStart = i * 159,
                }
            );
            base.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    6,
                    base.Position + new Vector2(1f, -1f) * 64f * i,
                    Color.Gray * 0.75f,
                    10
                )
                {
                    delayBeforeAnimationStart = i * 159,
                }
            );
            base.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    6,
                    base.Position + new Vector2(-1f, 1f) * 64f * i,
                    Color.Gray * 0.75f,
                    10
                )
                {
                    delayBeforeAnimationStart = i * 159,
                }
            );
            base.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    6,
                    base.Position + new Vector2(-1f, -1f) * 64f * i,
                    Color.Gray * 0.75f,
                    10
                )
                {
                    delayBeforeAnimationStart = i * 159,
                }
            );
        }
    }

    public override void update(GameTime time, GameLocation location)
    { /*
        if (
            currentState == State.SelfHealing
            && Sprite.CurrentAnimation == null
            && Sprite.currentFrame != 16
        )
        {
            Sprite.currentFrame = 16;
        }
        else if (
            currentState == State.Disappearing
            || currentState == State.FleeingCorner
                && Sprite.CurrentAnimation == null
                && Sprite.currentFrame != 32
        )
        {
            Sprite.currentFrame = 32;
        }
        else if (
            currentState == State.Appearing
            && Sprite.CurrentAnimation == null
            && Sprite.currentFrame != 41
        )
        {
            Sprite.currentFrame = 41;
        }*/
        base.update(time, location);
    }

    public override void defaultMovementBehavior(GameTime time)
    {
        if (!staticStates.Contains(currentState))
        {
            base.defaultMovementBehavior(time);
        }
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        TryToAnimateSprite(time);
        // AnimateBasedOnFacingDirection(time);
    }

    private void TryToAnimateSprite(GameTime time)
    {
        switch (currentState)
        {
            case State.Casting:
                AnimateCasting(time);
                return;
            case State.SelfHealing:
                if (!isMoving())
                {
                    AnimateSelfHealing(time);
                }
                return;
            case State.Disappearing:
            case State.Appearing:
            case State.FleeingCorner:
                if (!isMoving())
                {
                    AnimateFading(time);
                }
                return;
            case State.Default:
            case State.EnteringMage:
            case State.Fleeing:
            case State.ExitingMage:
            default:
                if (isMoving())
                {
                    switch (FacingDirection)
                    {
                        case DIRECTION_UP:
                            Sprite.AnimateUp(time);
                            break;
                        case DIRECTION_LEFT:
                            Sprite.AnimateLeft(time);
                            break;
                        case DIRECTION_RIGHT:
                            Sprite.AnimateRight(time);
                            break;
                        case DIRECTION_DOWN:
                            Sprite.AnimateDown(time);
                            break;
                    }
                }
                else
                {
                    Sprite.StopAnimation();
                }
                return;
        }
    }

    private void AnimateFading(GameTime time)
    {
        /* if (isMoving())
         {
             return;
         }*/
        var isFadingOut = currentState == State.Disappearing || currentState == State.FleeingCorner;
        if (fadingTimer >= FadingTime)
        {
            return;
        }
        var firstFrame = isFadingOut ? 32 : 41;
        var numberOfFrames = 10;
        Sprite.Animate(time, firstFrame, numberOfFrames, fadingTimer / numberOfFrames);
    }

    private void AnimateCasting(GameTime time)
    {
        /*   if (!casting.Value)
           {
               return;
           }*/
        var firstFrame = 16;
        var numberOfFrames = 4;
        Sprite.Animate(time, firstFrame, numberOfFrames, CooldownValue / numberOfFrames);
        rotationTimer = (float)(
            time.TotalGameTime.Milliseconds * Math.PI / 128f / 24f % (Math.PI * 1024f)
        );
    }

    private void AnimateSelfHealing(GameTime time)
    {
        var firstFrame = 16;
        var numberOfFrames = 16;
        Sprite.Animate(time, firstFrame, numberOfFrames, SelfCooldownValue / numberOfFrames);
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        if (isMage)
            MageBehaviorTick(time);
        else
            WarriorBehaviorTick(time);
    }

    private void RecordCloseness()
    {
        var currentClosenessCheck = (
            !isMoving() && this.withinPlayerThreshold(2),
            (float)Game1.currentGameTime.TotalGameTime.Milliseconds
        );
        if (lastClosenessCheck.time - currentClosenessCheck.Item2 >= CLOSENESS_INTERVAL)
        {
            var isPlayerStillHere = lastClosenessCheck.result && currentClosenessCheck.Item1;
            if (isPlayerStillHere)
            {
                closenessStacks++;
            }
            else
            {
                closenessStacks = 0;
            }
            closenessStacks = Math.Min(MaxClosenessStacks, closenessStacks + 1);

            var closenessChance = closenessStacks * ClosenessStackPercentage;
            LoggerHelper.TestingLog($"Closeness stacks: {closenessStacks}");
            LoggerHelper.TestingLog(
                $"Chance: {closenessStacks} * {ClosenessStackPercentage} = {closenessChance}"
            );
            if (Game1.random.NextDouble() < closenessStacks * ClosenessStackPercentage)
            {
                closenessStacks = 0;
                LoggerHelper.TestingLog("Close for too long", LogLevel.Warn);
            }
            lastClosenessCheck = currentClosenessCheck;
        }
    }

    private bool NeedsHealing()
    {
        bool needsHealing = isMage && HealthPercentage() < HealthRatioToSelfHeal;

        LoggerHelper.VerboseLog(
            $"[NEEDS-HEALING] Check: isMage={isMage}, HealthRatio={HealthPercentage():P2}, Threshold={MinHealthRatioToHeal:P2}, NeedsHealing={needsHealing}",
            LogLevel.Debug
        );

        return needsHealing;
    }

    private void ExecuteStateBehavior(
        GameTime time,
        Dictionary<State, Func<State>> behaviorMethods,
        Dictionary<State, Func<GameTime, State>> behaviorMethodsWithTime
    )
    {
        if (behaviorMethods.TryGetValue(currentState, out var behaviorMethod))
        {
            SetState(behaviorMethod());
        }
        else if (behaviorMethodsWithTime.TryGetValue(currentState, out var behaviorMethodWithTime))
        {
            if (currentState == State.SelfHealing && selfCoolDown > 0)
            {
                return;
            }
            if (currentState == State.Casting && coolDown > 0)
            {
                return;
            }
            SetState(behaviorMethodWithTime(time));
        }
        else
        {
            LoggerHelper.TestingLog(
                $"No behavior method found for state {currentState}",
                enabled: false
            );
        }
    }

    private State FadingBehavior(GameTime time)
    {
        if (IsANewFadingProcess())
        {
            fadingTimer = 0;
            string fadeReason = GetFadeReason();
            LoggerHelper.VerboseLog($"Pharaoh starting fade: {fadeReason}", LogLevel.Info);
        }

        IsWalkingTowardPlayer = false;
        fadingTimer = Math.Min(
            FadingTime,
            fadingTimer + (float)time.ElapsedGameTime.TotalMilliseconds
        );

        if (fadingTimer >= FadingTime)
        {
            fadingTimer = 0;
            string fadeCompletionReason = GetFadeCompletionReason();
            LoggerHelper.VerboseLog(
                $"Pharaoh fade complete: {fadeCompletionReason}",
                LogLevel.Info
            );

            if (currentState == State.Disappearing || currentState == State.FleeingCorner)
            {
                LoggerHelper.VerboseLog("Pharaoh relocating to new position", LogLevel.Info);
                _ = SetNewMonsterLocation();
                return State.Appearing;
            }
            else
            {
                var nextState = NeedsHealing() ? State.SelfHealing : State.Default;
                LoggerHelper.VerboseLog(
                    $"Pharaoh fade complete, transitioning to: {nextState}",
                    LogLevel.Info
                );
                return nextState;
            }
        }

        LoggerHelper.VerboseLog(
            $"Pharaoh fading in progress: {currentState}, timer: {fadingTimer}/{FadingTime}",
            LogLevel.Debug
        );
        return currentState;
    }

    private string GetFadeReason()
    {
        return currentState switch
        {
            State.Disappearing =>
                $"Disappearing - Health: {Health}/{MaxHealth} ({HealthPercentage():P1})",
            State.FleeingCorner =>
                $"Fleeing from corner - Health: {Health}/{MaxHealth} ({HealthPercentage():P1})",
            State.Appearing =>
                $"Reappearing after fade - Health: {Health}/{MaxHealth} ({HealthPercentage():P1})",
            _ =>
                $"Fading due to state: {currentState} - Health: {Health}/{MaxHealth} ({HealthPercentage():P1})",
        };
    }

    private string GetFadeCompletionReason()
    {
        return currentState switch
        {
            State.Disappearing => "Completed disappearing fade",
            State.FleeingCorner => "Completed fleeing corner fade",
            State.Appearing => "Completed reappearing fade",
            _ => $"Completed fade for state: {currentState}",
        };
    }

    private bool IsANewFadingProcess()
    {
        return previousState != currentState;
    }

    private bool SetNewMonsterLocation()
    {
        int iteration = 0;
        int totalOfTries = 100;
        while (iteration < totalOfTries) // add a maximum number of iterations
        {
            Vector2? chosenTile = null;

            for (int i = 0; i < totalOfTries; i++)
            {
                chosenTile = TileUtility.GetRandomTile(
                    this.currentLocation,
                    TileUtility.ValidMineTileIndexes
                );

                if (chosenTile != null)
                {
                    break;
                }
            }

            if (chosenTile.HasValue)
            {
                var location = (MineShaft)this.currentLocation;
                var addedMonster = TryToAddMonster(
                    this,
                    (Vector2)chosenTile,
                    location,
                    true,
                    playerThreshold
                );
                if (addedMonster)
                {
                    return true;
                }
            }
            iteration++;
        }
        return false;
    }

    private bool TryToAddMonster(
        Monster m,
        Vector2 tile,
        MineShaft _mineShaft,
        bool moving = false,
        (int minThreshold, int maxThreshold)? threshold = null
    )
    {
        var conditions =
            TileUtility.IsTileClearForMonsters(tile, _mineShaft)
            && !_mineShaft.IsTileOccupiedBy(tile);
        if (moving && threshold.HasValue)
        {
            conditions &= WithinPlayerThreshold(threshold.Value, tile);
        }
        if (conditions)
        {
            if (m is Duggy)
            {
                var validIndexes = new int[] { 165, 181, 183 };
                var backTile = _mineShaft.getTileIndexAt((int)tile.X, (int)tile.Y, "Back");
                if (!validIndexes.Contains(backTile))
                {
                    return false;
                }
            }
            if (moving)
            {
                m.setTileLocation(tile);
            }
            else
            {
                m.setTilePosition((int)tile.X, (int)tile.Y);
            }

            if (!moving)
            {
                _mineShaft.characters.Add(m);
            }
            return true;
        }

        return false;
    }

    private bool WithinPlayerThreshold((int min, int max) chosenThreshold, Vector2 monsterTile)
    {
        var playerTile = Game1.MasterPlayer.Tile;

        // Calculate the threshold (Euclidean distance between player and monster tiles)
        float threshold = (float)
            Math.Sqrt(
                Math.Pow(monsterTile.X - playerTile.X, 2)
                    + Math.Pow(monsterTile.Y - playerTile.Y, 2)
            );

        // Check if the threshold is within the given min and max parameters
        bool withinThreshold = threshold >= chosenThreshold.min && threshold <= chosenThreshold.max;

        return withinThreshold;
    }

    private bool CanSeePlayer()
    {
        return this.withinPlayerThreshold(VisionDistance)
            || (CanSeePlayerInPeripheralVision() && HasLineOfSightToPlayer());
    }

    private State ChangingCombatBehavior()
    {
        // Debug logging to track which methods are calling this method
        /* StackTrace stackTrace = new StackTrace();
        StackFrame? callingFrame = stackTrace.GetFrame(1);
        string callerMethod = callingFrame?.GetMethod()?.Name ?? "Unknown";
        string callerClass = callingFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

        LoggerHelper.VerboseLog(
            $"[DEBUG] ThePharaoh.ChangingCombatBehavior() called by {callerClass}.{callerMethod} | CurrentState: {currentState} | IsWarrior: {isWarrior}",
            LogLevel.Debug
        );
 */
        Halt();
        if (isMage)
        {
            Sprite = new AnimatedSprite("Characters\\Monsters\\ThePharaoh") { SpriteHeight = 24 };
            Sprite.UpdateSourceRect();
            Sprite.LoadTexture("Characters\\Monsters\\ThePharaoh");
        }
        else
        {
            Sprite = new AnimatedSprite("Characters\\Monsters\\ThePharaoh_CombatMode")
            {
                SpriteHeight = 24,
            };
            Sprite.UpdateSourceRect();
        }
        Game1.Multiplayer.broadcastSprites(
            base.currentLocation,
            new TemporaryAnimatedSprite(
                "TileSheets\\animations",
                new Rectangle(0, 2880, 64, 64),
                150f,
                10,
                2,
                new Vector2(this.Position.X, this.Position.Y - 2),
                false,
                false,
                0.5f,
                0,
                isMage ? Color.Purple : Color.Goldenrod,
                2,
                0,
                0,
                0
            )
        );
        base.currentLocation.playSound("shadowpeep");
        Scale = currentState == State.EnteringMage ? MageScale : WarriorScale;
        return State.Default;
    }

    public override bool ShouldActuallyMoveAwayFromPlayer()
    {
        return isMage && currentState != State.Fleeing;
    }

    #region Common Methods
    // Removed duplicate SetState method, keeping only one implementation
    private void SetState(State newState, bool shouldHalt = true)
    {
        if (shouldHalt && previousState != newState)
        {
            Halt();
        }
        var MageValidStates = new State[]
        {
            State.Appearing,
            State.Casting,
            State.Default,
            State.Disappearing,
            State.ExitingMage,
            State.Fleeing,
            State.FleeingCorner,
            State.SelfHealing,
        };
        var WarriorValidStates = new State[] { State.Default, State.EnteringMage };
        if (isMage && !MageValidStates.Contains(newState))
        {
            LoggerHelper.VerboseLog($"Invalid State for mage: {newState}", LogLevel.Error);
            newState = State.Default;
        }
        if (!isMage && !WarriorValidStates.Contains(newState))
        {
            LoggerHelper.VerboseLog($"Invalid State for warrior: {newState}", LogLevel.Error);
            newState = State.Default;
        }
        if (newState == State.EnteringMage)
        {
            isMage = true;
        }
        else if (newState == State.ExitingMage)
        {
            isMage = false;
        }
        previousState = currentState;
        currentState = newState;
    }

    private bool IsFleeingCorner() => currentState == State.FleeingCorner;

    private void UpdateAttackTime(bool isBomb)
    {
        var currentAttackTime = Game1.currentGameTime.TotalGameTime.Milliseconds;
        if (currentAttackTime - lastAttackTime <= AttackInterval && !isBomb)
        {
            missAtackStacks++;
        }
        else
        {
            missAtackStacks--;
        }
        missAtackStacks = Math.Clamp(missAtackStacks, 0, MaxMissStacks);
        lastAttackTime = !isBomb ? currentAttackTime : lastAttackTime;
    }

    private int CalculateActualDamage(int damage) => Math.Max(1, damage - resilience.Value);

    private double CalculateMissChance()
    {
        var missChanceBasedOnState = currentState switch
        {
            State.Disappearing or State.Appearing => missChance.Value
                + missAtackStacks * AttackStackMissPercentage * MissChanceFadingMultiplier,
            State.SelfHealing => missChance.Value * MissChanceSelfHealingMultiplier,
            _ => missChance.Value,
        };
        return missChanceBasedOnState;
    }

    private bool IsMiss(double missChance, double addedPrecision) =>
        Game1.random.NextDouble() < missChance - missChance * addedPrecision;

    private void HandleMiss(ref int actualDamage)
    {
        actualDamage = -1;

        if (isMage)
        {
            if (
                Game1.random.NextDouble()
                < CorneredEscapeChance + missAtackStacks * AttackStackMissPercentage
            )
            {
                nextStateAfterDamage = State.FleeingCorner;
            }
            else
            {
                nextStateAfterDamage = State.Fleeing;
            }
        }
        else
        {
            nextStateAfterDamage = State.Default;
        }
        missAtackStacks = 0;
    }

    private void HandleHit(ref int actualDamage, int xTrajectory, int yTrajectory)
    {
        LoggerHelper.VerboseLog(
            $"HandleHit called with actualDamage={actualDamage}, xTrajectory={xTrajectory}, yTrajectory={yTrajectory}",
            LogLevel.Info,
            false
        );

        LoggerHelper.VerboseLog(
            $"Trajectory set to: ({-1 * xTrajectory / 3}, {-1 * yTrajectory / 3})",
            LogLevel.Debug,
            false
        );

        base.currentLocation.playSound("shadowHit");
        base.Health -= actualDamage;
        LoggerHelper.VerboseLog($"Health after hit: {base.Health}", LogLevel.Info, false);
        if (currentState == State.Casting && Game1.random.NextBool())
        {
            coolDown += 200;
            LoggerHelper.VerboseLog(
                $"Cooldown increased by 200, new cooldown: {coolDown}",
                LogLevel.Debug,
                false
            );
        }

        if (base.Health <= 0)
        {
            LoggerHelper.VerboseLog(
                "Health is 0 or less, playing death sound and triggering death animation.",
                LogLevel.Warn
            );
            base.currentLocation.playSound("shadowDie");
            deathAnimation();
        }
        else
        {
            // Apply knockback without teleportation
            // Use the trajectory values directly without position recalculation
            int knockbackX = xTrajectory / 3;
            int knockbackY = yTrajectory / 3;

            // Ensure knockback doesn't cause teleportation by limiting magnitude
            knockbackX = Math.Max(-3, Math.Min(3, knockbackX));
            knockbackY = Math.Max(-3, Math.Min(3, knockbackY));

            setTrajectory(knockbackX, knockbackY);

            if (isMage)
            {
                nextStateAfterDamage = currentState switch
                {
                    State.SelfHealing => ResetStateAfterHealing(),
                    State.Disappearing or State.Appearing => State.Fleeing,
                    State.FleeingCorner => State.FleeingCorner,
                    _ => State.Fleeing,
                };
            }
            else
            {
                nextStateAfterDamage = State.Default;
            }
            LoggerHelper.VerboseLog(
                $"Next state after damage set to: {nextStateAfterDamage}",
                LogLevel.Info
            );
            if (nextStateAfterDamage == State.Fleeing)
            {
                fleeingCause = currentState switch
                {
                    State.SelfHealing => fleeingCause,
                    State.Disappearing or State.Appearing => "was fading.",
                    _ => "is mage.",
                };
                LoggerHelper.VerboseLog($"Fled because {fleeingCause}", LogLevel.Info);
            }
        }
    }

    private State ResetStateAfterHealing()
    {
        LoggerHelper.VerboseLog(
            $"[HEALING-INTERRUPTED] ResetStateAfterHealing called - interruptedHealingCount={interruptedHealingCount}, maxAllowed=3",
            LogLevel.Debug
        );

        if (interruptedHealingCount < MaxInterruptAttempts)
        {
            interruptedHealingCount++;
            isInterruptedHealing = true;
            fleeingCause = $"Was self healing and was attacked.";

            LoggerHelper.VerboseLog(
                $"[HEALING-INTERRUPTED] Healing interrupted! Count: {interruptedHealingCount}/3, transitioning to Fleeing state",
                LogLevel.Warn
            );
            return State.Fleeing;
        }
        else
        {
            isInterruptedHealing = false;
            isHealing = false;

            LoggerHelper.VerboseLog(
                $"[HEALING-INTERRUPTED] Max interruptions reached ({interruptedHealingCount}/3), resetting healing state",
                LogLevel.Info
            );
            return State.Default;
        }
    }

    private bool CanSeePlayerInPeripheralVision()
    {
        bool canSee = Utility.couldSeePlayerInPeripheralVision(base.Player, this);
        return canSee;
    }

    private bool HasLineOfSightToPlayer()
    {
        bool hasLineOfSight = Utility.doesPointHaveLineOfSightInMine(
            base.currentLocation,
            base.Tile,
            base.Player.Tile,
            VisionDistance
        );
        return hasLineOfSight;
    }

    private List<Monster> GetMonstersNearPlayer()
    {
        return base
            .currentLocation.characters.Where(character =>
                character is Monster monster
                && monster.withinPlayerThreshold(MonsterSearchRadius)
                && monster != this
            )
            .Select(character => (Monster)character)
            .ToList();
    }

    private (Monster?, double) GetLowestHealthMonster(List<Monster> monsters)
    {
        var superLowHealth = 0;
        Monster? lowestHealthMonster = null;
        double lowestHealth = 1.0;
        foreach (
            var m in monsters.Where(m => (double)m.Health / (double)m.MaxHealth <= lowestHealth)
        )
        {
            lowestHealthMonster = m;
            lowestHealth = (double)m.Health / (double)m.MaxHealth;
            if (lowestHealth < 0.5)
            {
                superLowHealth++;
            }
        }
        return (lowestHealthMonster, superLowHealth * 0.05);
    }

    private double CalculateHealChance((Monster?, double) lowestHealthMonster)
    {
        var monster = lowestHealthMonster.Item1;
        var chanceIncrement = lowestHealthMonster.Item2;
        var healChance = 0.4 + chanceIncrement;
        if (monster != null)
        {
            healChance = monster.Health / monster.MaxHealth <= 1 ? 0 : healChance;
        }
        return healChance;
    }

    private bool ShouldShoot(Farmer player, double healChance)
    {
        return player.Attack >= 0 && Game1.random.NextDouble() > healChance;
    }

    private void Shoot()
    {
        // faceGeneralDirection(base.Player.Position);
        float freezeChance = FreezeChance;
        if (this.withinPlayerThreshold(MonsterSearchRadius))
        {
            freezeChance += FreezeChanceProximityBonus;
        }
        var isIceBall = Game1.random.NextDouble() < freezeChance;
        Sprite.UpdateSourceRect();

        Vector2 standingPixel = getStandingPosition();
        Rectangle monsterBounds = GetBoundingBox();
        Vector2 velocityTowardPlayer = Utility.getVelocityTowardPlayer(
            monsterBounds.Center,
            15f,
            base.Player
        );
        Projectile projectile;

        if (isIceBall)
        {
            projectile = new DebuffingProjectile(
                "19",
                14,
                4,
                4,
                (float)Math.PI / 16f,
                velocityTowardPlayer.X,
                velocityTowardPlayer.Y,
                new Vector2(monsterBounds.X, monsterBounds.Y),
                base.currentLocation,
                this
            );
            base.currentLocation.playSound("coldSpell");
        }
        else
        {
            projectile = new BasicProjectile(
                15,
                10,
                2,
                4,
                0f,
                velocityTowardPlayer.X,
                velocityTowardPlayer.Y,
                standingPixel - new Vector2(32f, 0f),
                null,
                null,
                null,
                explode: true,
                damagesMonsters: false,
                base.currentLocation,
                this
            );

            base.currentLocation.playSound("fireball");
        }
        projectile.height.Value = 48f;
        base.currentLocation.projectiles.Add(projectile);
    }

    private State SpottedPlayerBehavior(GameTime time)
    {
        if (
            controller == null
            && !Utility.doesPointHaveLineOfSightInMine(
                base.currentLocation,
                base.Tile,
                base.Player.Tile,
                VisionDistance
            )
        )
        {
            controller = new PathFindController(
                this,
                base.currentLocation,
                base.Player.TilePoint,
                -1,
                null,
                300
            );
            if (
                controller == null
                || controller.pathToEndPoint == null
                || controller.pathToEndPoint.Count == 0
            )
            {
                Halt();
                controller = null;
                addedSpeed = 0f;
                return State.Default;
            }
        }
        else if (coolDown <= 0 && Game1.random.NextDouble() < RandomCastChance)
        {
            casting.Value = true;
            controller = null;
            base.IsWalkingTowardPlayer = false;
            Halt();
            return State.Casting;
        }
        return State.Default;
    }

    private bool ShouldEnterMageMode()
    {
        // Only enter mage mode when health is critically low
        return HealthPercentage() < HealthRatioToEnterMageMode;
    }

    private bool ShouldExitMageMode()
    {
        if (!CanSeePlayer())
        {
            return false;
        }
        // Add hysteresis - only exit mage mode when health is significantly restored
        var shouldExitMageMode = HealthPercentage() > HealthRatioToExitMageMode;
        return shouldExitMageMode;
    }

    private State FleeingBehavior(GameTime time)
    {
        IsWalkingTowardPlayer = false;

        int fleeDistance = (int)(FleeDistanceMultiplier * (1 - HealthPercentage()) * 400);
        LoggerHelper.TestingLog(
            $"Trying to flee? :{this.withinPlayerThreshold(MonsterSearchRadius) && FleeAttempts <= MaxFleeAttempts} "
        );
        monsterPreviousPosition ??= this.Position;
        while (this.withinPlayerThreshold(MonsterSearchRadius) && FleeAttempts <= MaxFleeAttempts)
        {
            addedSpeed = AddedSpeedFleeing;
            Point monsterPixel = base.StandingPixel;
            Point playerPixel = base.Player.StandingPixel;

            FacingDirection = getGeneralDirectionTowards(
                base.Player.getStandingPosition(),
                0,
                opposite: false,
                useTileCalculations: false
            );
            if (monsterPreviousPosition.HasValue && monsterPreviousPosition.Value == this.Position)
            {
                FacingDirection = FacingDirection switch
                {
                    DIRECTION_UP => DIRECTION_DOWN,
                    DIRECTION_DOWN => DIRECTION_UP,
                    DIRECTION_LEFT => DIRECTION_RIGHT,
                    DIRECTION_RIGHT => DIRECTION_LEFT,
                    _ => DIRECTION_UP,
                };
            }
            moveUp = false;
            moveDown = false;
            moveRight = false;
            moveLeft = false;
            setMovingInFacingDirection();
            LoggerHelper.TestingLog(
                $"Is cornered: {!moveUp && !moveDown && !moveRight && !moveLeft}"
            );
            LoggerHelper.TestingLog($"Is moving: {isMoving()}");
            if (!moveUp && !moveDown && !moveRight && !moveLeft)
            {
                // If the monster is cornered, try moving in a random direction

                int randomDirection = Game1.random.Next(4);

                switch (randomDirection)
                {
                    case 0:
                        SetMovingUp(b: true);
                        break;
                    case 1:
                        SetMovingDown(b: true);
                        break;
                    case 2:
                        SetMovingLeft(b: true);
                        break;
                    case 3:
                        SetMovingRight(b: true);
                        break;
                }
            }

            MovePosition(time, Game1.viewport, base.currentLocation);
            //.IsWalkingTowardPlayer = false;

            if (Math.Abs(playerPixel.Y - monsterPixel.Y) > fleeDistance)
            {
                if (playerPixel.X - monsterPixel.X > 0)
                {
                    SetMovingLeft(b: true);
                }
                else
                {
                    SetMovingRight(b: true);
                }
            }
            else if (playerPixel.Y - monsterPixel.Y > 0)
            {
                SetMovingUp(b: true);
            }
            else
            {
                SetMovingDown(b: true);
            }
            controller = new PathFindController(
                this,
                base.currentLocation,
                Utility.isOffScreenEndFunction,
                -1,
                offScreenBehavior,
                350,
                new Point(0)
            );
            FleeAttempts++;
            monsterPreviousPosition = this.Position;
        }
        LoggerHelper.TestingLog("Done fleeing.");
        addedSpeed = 0f;
        FleeAttempts = 0;
        if (isInterruptedHealing)
        {
            return State.SelfHealing;
        }
        if (Game1.random.NextDouble() < ChanceDisappearAfterFleeing)
        {
            return State.Disappearing;
        }
        else if (NeedsHealing())
        {
            return State.SelfHealing;
        }
        else
        {
            return State.Default;
        }
    }

    public void offScreenBehavior(Character c, GameLocation l) { }

    private State SelfHealingBehavior(GameTime time)
    {
        LoggerHelper.VerboseLog(
            $"[SELF-HEALING] Entering SelfHealingBehavior - Current Health: {Health}/{MaxHealth} ({HealthPercentage():P1})",
            LogLevel.Debug
        );
        LoggerHelper.VerboseLog(
            $"[SELF-HEALING] Previous State: {previousState}, Current State: {currentState}, IsHealing: {isHealing}, IsInterrupted: {isInterruptedHealing}",
            LogLevel.Debug
        );

        if (IsANewHealingProcess() && !isHealing && !isInterruptedHealing)
        {
            LoggerHelper.VerboseLog("[SELF-HEALING] Starting new healing process", LogLevel.Debug);
            StartNewHealingProcess();
        }
        else
        {
            LoggerHelper.VerboseLog(
                $"[SELF-HEALING] Continuing healing process - IsNewProcess: {IsANewHealingProcess()}, IsHealing: {isHealing}, IsInterrupted: {isInterruptedHealing}",
                LogLevel.Debug
            );
        }

        percentageToHeal += HealingIncrementRate;
        float healingAmount = (float)(percentageToHeal * base.MaxHealth);

        LoggerHelper.VerboseLog(
            $"[SELF-HEALING] Healing calculation - PercentageToHeal: {percentageToHeal:P2}, HealingAmount: {healingAmount:F2}, MaxHealth: {MaxHealth}",
            LogLevel.Debug
        );

        // Heal the entity with reduced amount
        SelfHeal(healingAmount);

        var bar = (float)Game1.random.Next(8500, 9500) / 10000; // Higher threshold

        LoggerHelper.VerboseLog(
            $"[SELF-HEALING] Health check - Current Ratio: {HealthPercentage():P2}, Threshold: {bar:P2}, NeedsMoreHealing: {HealthPercentage() <= bar}",
            LogLevel.Debug
        );

        if (HealthPercentage() > bar)
        {
            LoggerHelper.VerboseLog(
                "[SELF-HEALING] Healing complete - transitioning to Default state",
                LogLevel.Info
            );
            isHealing = false;
            return State.Default;
        }
        else
        {
            LoggerHelper.VerboseLog(
                $"[SELF-HEALING] Continuing healing - setting cooldown: {SelfCooldownValue * SelfCooldownMultiplier}ms",
                LogLevel.Debug
            );
            isHealing = true;
            selfCoolDown = SelfCooldownValue * SelfCooldownMultiplier; // 6.25s cooldown
            return State.SelfHealing;
        }
    }

    private bool IsANewHealingProcess()
    {
        return previousState != State.SelfHealing;
    }

    private void StartNewHealingProcess()
    {
        LoggerHelper.VerboseLog($"[START-HEALING] New Healing Process - BALANCED", LogLevel.Info);
        LoggerHelper.VerboseLog(
            $"[START-HEALING] Initializing healing: interruptedHealingCount=0, percentageToHeal=0.02f, selfCoolDown={SelfCooldownValue * SelfCooldownMultiplier}ms",
            LogLevel.Debug
        );
        interruptedHealingCount = 0;
        percentageToHeal = InitialHealPercentage;
        selfCoolDown = SelfCooldownValue * SelfCooldownMultiplier; // 6.25s cooldown
    }

    private void SelfHeal(double healingAmount)
    {
        var newHealth = (int)(this.Health + healingAmount);
        this.Health = Math.Min(this.MaxHealth, newHealth);
        PlaySoundAndHealingEffects(this, (int)healingAmount);
    }

    private int Heal(Monster? lowestHealthMonster, double percentageToHeal)
    {
        if (lowestHealthMonster == null)
        {
            LoggerHelper.TestingLog("lowestHealthMonster is null, returning 0");
            return 0;
        }
        var baseHealth = lowestHealthMonster.MaxHealth - lowestHealthMonster.Health;
        var amountToHeal = (int)(baseHealth * percentageToHeal);

        PlaySoundAndHealingEffects(lowestHealthMonster, amountToHeal);
        return amountToHeal;
    }

    private void PlaySoundAndHealingEffects(Monster monster, int healingAmount)
    {
        base.currentLocation.playSound("healSound");
        Game1.Multiplayer.broadcastSprites(
            base.currentLocation,
            new TemporaryAnimatedSprite(
                "TileSheets\\animations",
                new Rectangle(0, 256, 64, 64),
                40f,
                8,
                0,
                monster.Position + new Vector2(32f, 64f),
                flicker: false,
                flipped: false
            )
        );
        base.currentLocation.debris.Add(
            new Debris(
                healingAmount,
                new Vector2(monster.GetBoundingBox().Center.X, monster.GetBoundingBox().Center.Y),
                Color.Green,
                1f,
                monster
            )
        );
    }
    #endregion

    #region Mage Methods
    private State MageDefaultBehavior(GameTime time)
    {
        if (CanSeePlayer())
        {
            IsWalkingTowardPlayer = true;
            base.defaultMovementBehavior(time);
            controller = null;
            if (Game1.random.NextDouble() < 0.3)
            {
                base.currentLocation.playSound("shadowpeep");
            }
            return SpottedPlayerBehavior(time);
        }
        base.defaultMovementBehavior(time);
        return State.Default;
    }

    private State CastingBehavior(GameTime time)
    {
        this.facePlayer(base.Player);
        base.IsWalkingTowardPlayer = false;
        //this.IsWalkingInSquare = true;
        //faceGeneralDirection(base.Player.Position);
        var monstersNearPlayer = GetMonstersNearPlayer();
        var lowestHealthMonster = GetLowestHealthMonster(monstersNearPlayer);
        var healChance = CalculateHealChance(lowestHealthMonster);
        Rectangle monsterBounds = GetBoundingBox();
        Vector2 velocityTowardPlayer = Utility.getVelocityTowardPlayer(
            monsterBounds.Center,
            15f,
            base.Player
        );
        casting.Value = true;
        if (ShouldShoot(base.Player, healChance))
        {
            Shoot();
        }
        else
        {
            Heal(lowestHealthMonster.Item1, PercentageHealAmount);
        }
        this.doneFacingPlayer(base.Player);
        ///base.IsWalkingTowardPlayer = true;
        casting.Value = false;
        coolDown = CooldownValue;
        return State.Default;
    }

    private void MageBehaviorTick(GameTime time)
    {
        if (staticStates.Contains(currentState))
        {
            this.IsWalkingTowardPlayer = false;
            moveDown = false;
            moveLeft = false;
            moveRight = false;
            moveUp = false;
            controller = null;
        }
        else
        {
            MageDetermineNextState();
            MageRecordCloseness();
        }
        MageTryToAnimateSprite(time);

        this.willDestroyObjectsUnderfoot = false;
        if (timeBeforeAIMovementAgain <= 0f)
        {
            base.IsInvisible = false;
        }
        coolDown = Math.Max(0, coolDown - time.ElapsedGameTime.Milliseconds);
        selfCoolDown = Math.Max(0, selfCoolDown - (float)time.ElapsedGameTime.TotalMilliseconds);
        MageUpdateStateBehavior(time);
    }

    private void MageDetermineNextState()
    {
        if (ShouldExitMageMode())
        {
            SetState(State.ExitingMage);
        }
        else if (NeedsHealing() && currentState != State.Fleeing && selfCoolDown <= 0)
        {
            Halt();
            fleeingCause = "Mage needs healing";
            SetState(State.Fleeing);
            base.IsWalkingTowardPlayer = false;
        }
        else if (nextStateAfterDamage != null)
        {
            SetState(nextStateAfterDamage.Value, false);
            nextStateAfterDamage = null;
        }
    }

    private void MageUpdateStateBehavior(GameTime time)
    {
        var behaviorMethods = new Dictionary<State, Func<State>>
        {
            { State.ExitingMage, ChangingCombatBehavior },
        };

        var behaviorMethodsWithTime = new Dictionary<State, Func<GameTime, State>>
        {
            { State.Fleeing, FleeingBehavior },
            { State.Default, MageDefaultBehavior },
            { State.Casting, CastingBehavior },
            { State.SelfHealing, SelfHealingBehavior },
            { State.Appearing, FadingBehavior },
            { State.Disappearing, FadingBehavior },
            { State.FleeingCorner, FadingBehavior },
        };

        ExecuteStateBehavior(time, behaviorMethods, behaviorMethodsWithTime);
    }

    private void MageRecordCloseness()
    {
        RecordCloseness();
    }

    private void MageTryToAnimateSprite(GameTime time)
    {
        TryToAnimateSprite(time);
    }

    #endregion

    #region Warrior Methods
    private void WarriorBehaviorTick(GameTime time)
    {
        if (staticStates.Contains(currentState))
        {
            this.IsWalkingTowardPlayer = false;
            moveDown = false;
            moveLeft = false;
            moveRight = false;
            moveUp = false;
            controller = null;
        }
        else
        {
            WarriorDetermineNextState();
            WarriorRecordCloseness();
        }
        WarriorTryToAnimateSprite(time);

        this.willDestroyObjectsUnderfoot = true;
        if (timeBeforeAIMovementAgain <= 0f)
        {
            base.IsInvisible = false;
        }
        coolDown = Math.Max(0, coolDown - time.ElapsedGameTime.Milliseconds);
        selfCoolDown = Math.Max(0, selfCoolDown - (float)time.ElapsedGameTime.TotalMilliseconds);
        WarriorUpdateStateBehavior(time);
    }

    private void WarriorDetermineNextState()
    {
        if (ShouldEnterMageMode())
        {
            if (isFirstMageMode)
            {
                base.Health += (int)(HealthBoostOnFirstMageMode * base.MaxHealth);
                isFirstMageMode = false;
            }
            combatModeCause = "Warrior switching to mage mode";
            SetState(State.EnteringMage);
        }
        else if (nextStateAfterDamage != null)
        {
            SetState(nextStateAfterDamage.Value, false);
            nextStateAfterDamage = null;
        }
    }

    private State WarriorDefaultBehavior(GameTime time)
    {
        if (CanSeePlayer())
        {
            //focusedOnFarmers = true;
            IsWalkingTowardPlayer = true;
            return State.Default;
        }
        base.defaultMovementBehavior(time);
        return State.Default;
    }

    private State WarriorFleeingBehavior(GameTime time)
    {
        // Implement warrior-specific fleeing behavior here
        return FleeingBehavior(time);
    }

    private void WarriorUpdateStateBehavior(GameTime time)
    {
        var behaviorMethods = new Dictionary<State, Func<State>>
        {
            { State.EnteringMage, ChangingCombatBehavior },
        };

        var behaviorMethodsWithTime = new Dictionary<State, Func<GameTime, State>>
        {
            { State.Default, WarriorDefaultBehavior },
        };

        ExecuteStateBehavior(time, behaviorMethods, behaviorMethodsWithTime);
    }

    private void WarriorRecordCloseness()
    {
        RecordCloseness();
    }

    private void WarriorTryToAnimateSprite(GameTime time)
    {
        TryToAnimateSprite(time);
    }

    #endregion
}
