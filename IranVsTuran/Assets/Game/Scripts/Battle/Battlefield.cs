using System;
using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IranVsTuran.Battle
{
    public enum BattleState
    {
        /// <summary>Building before the first wave is called.</summary>
        Prep,
        Running,
        Won,
        Lost,
    }

    /// <summary>
    /// Runs one battle: the map, the waves, coins and lives, every unit, tower and projectile,
    /// the two spells, items, and victory or defeat. Everything is updated from here in a fixed
    /// order so the simulation is easy to follow. The HUD calls the public actions.
    /// </summary>
    public class Battlefield : MonoBehaviour
    {
        public static readonly float[] DifficultyHp = { 0.75f, 1f, 1.3f };
        const float WaveGap = 16f;
        const float EarlyCallCoinsPerSecond = 1.5f;

        public LevelDef Level { get; private set; }
        public PathTrack[] Tracks { get; private set; }
        public readonly List<Slot> Slots = new List<Slot>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Ally> Allies = new List<Ally>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        readonly List<Enemy> pendingEnemies = new List<Enemy>();
        readonly List<GroundFire> fires = new List<GroundFire>();
        public Effects Effects { get; private set; }

        public BattleState State { get; private set; }
        public int Coins { get; private set; }
        public int Lives { get; private set; }
        public int StartLives { get; private set; }
        public int Kills { get; private set; }
        public bool Revived { get; private set; }
        public Ally Hero { get; private set; }
        public float FreezeTimer { get; private set; }

        /// <summary>Waves started so far (1 = the first wave is under way).</summary>
        public int WavesStarted { get; private set; }
        public int WaveCount { get { return Level.waves.Length; } }
        /// <summary>Seconds until the next wave starts by itself; negative when no countdown is running.</summary>
        public float NextWaveTimer { get; private set; }
        public bool CanCallWave { get { return (State == BattleState.Prep || NextWaveTimer > 0f) && WavesStarted < WaveCount; } }

        public float ArrowsCooldown { get; private set; }
        public float ReinforceCooldown { get; private set; }
        public float ArrowsCooldownMax { get; private set; }
        public float ReinforceCooldownMax { get { return SpellDefs.ReinforceCooldown; } }

        public event Action Changed;
        public event Action<EnemyDef> BossArrived;
        public event Action<bool> Ended;

        class ActiveWave
        {
            public WaveDef def;
            public float time;
            public int[] spawned;
            public int alternate;
            public bool Done;
        }

        readonly List<ActiveWave> activeWaves = new List<ActiveWave>();
        float hpMultiplier = 1f;
        float endTimer = -1f;
        Camera battleCamera;
        Vector3 cameraHome;
        float shake;
        readonly Dictionary<Sfx, float> lastSound = new Dictionary<Sfx, float>();
        Sprite mapSprite;

        public static Battlefield Create(LevelDef level, int difficulty, Camera camera)
        {
            var go = new GameObject("Battlefield");
            var field = go.AddComponent<Battlefield>();
            field.Init(level, difficulty, camera);
            return field;
        }

        void Init(LevelDef level, int difficulty, Camera camera)
        {
            Level = level;
            battleCamera = camera;
            cameraHome = camera != null ? camera.transform.position : Vector3.zero;
            hpMultiplier = DifficultyHp[Mathf.Clamp(difficulty, 0, DifficultyHp.Length - 1)];
            Effects = new Effects(transform);

            var background = new GameObject("Map").AddComponent<SpriteRenderer>();
            background.sprite = mapSprite = ArtLibrary.Map(level);
            background.sortingOrder = Depth.Background;
            background.transform.SetParent(transform, false);

            Tracks = new PathTrack[level.PathCount];
            for (var i = 0; i < Tracks.Length; i++)
                Tracks[i] = new PathTrack(level.Path(i));
            foreach (var slot in level.slots)
                Slots.Add(new Slot(this, new Vector2(slot.x, slot.y)));

            Coins = level.startCoins + Mathf.RoundToInt(Upgrades.Bonus(UpgradeDefs.Economy));
            Lives = StartLives = level.lives;
            NextWaveTimer = -1f;
            ArrowsCooldownMax = SpellDefs.ArrowsCooldown;
            State = BattleState.Prep;

            var heroDef = Heroes.Selected;
            if (heroDef != null)
            {
                var track = Tracks[0];
                var spawn = track.At(Mathf.Max(0f, track.Length - 2.2f));
                var home = track.At(track.Length * 0.62f);
                Hero = Ally.CreateHero(this, heroDef, Heroes.Skin(heroDef.id), Heroes.Level(heroDef.id), spawn, home);
                Hero.Order(home);
                Allies.Add(Hero);
            }
        }

        // ------------------------------------------------------------------ update loop

        void Update()
        {
            var dt = Time.deltaTime;
            UpdateShake(dt);
            if (State == BattleState.Lost)
                return;
            if (State == BattleState.Won)
            {
                Effects.Update(dt);
                return;
            }

            ArrowsCooldown = Mathf.Max(0f, ArrowsCooldown - dt);
            ReinforceCooldown = Mathf.Max(0f, ReinforceCooldown - dt);
            FreezeTimer = Mathf.Max(0f, FreezeTimer - dt);

            UpdateWaves(dt);

            for (var i = 0; i < Enemies.Count; i++)
                Enemies[i].Update(dt);
            for (var i = 0; i < Allies.Count; i++)
                Allies[i].Update(dt);
            for (var i = 0; i < Towers.Count; i++)
                Towers[i].Update(dt);
            for (var i = 0; i < Projectiles.Count; i++)
                Projectiles[i].Update(dt);
            for (var i = fires.Count - 1; i >= 0; i--)
            {
                fires[i].Update(this, dt);
                if (fires[i].TimeLeft <= 0f)
                    fires.RemoveAt(i);
            }
            Effects.Update(dt);

            Enemies.RemoveAll(e => e.Removed || !e.Alive);
            Allies.RemoveAll(a => a.Removed);
            Projectiles.RemoveAll(p => p.Done);
            if (pendingEnemies.Count > 0)
            {
                Enemies.AddRange(pendingEnemies);
                pendingEnemies.Clear();
            }

            CheckVictory(dt);
        }

        void UpdateWaves(float dt)
        {
            if (State != BattleState.Running)
                return;

            foreach (var wave in activeWaves)
            {
                if (wave.Done)
                    continue;
                wave.time += dt;
                var finished = true;
                for (var g = 0; g < wave.def.groups.Count; g++)
                {
                    var group = wave.def.groups[g];
                    while (wave.spawned[g] < group.count && wave.time >= group.delay + wave.spawned[g] * group.interval)
                    {
                        var path = group.path >= 0 ? Mathf.Min(group.path, Tracks.Length - 1) : wave.alternate++ % Tracks.Length;
                        SpawnEnemy(EnemyDefs.Get(group.enemy), path, 0f);
                        wave.spawned[g]++;
                    }
                    if (wave.spawned[g] < group.count)
                        finished = false;
                }
                if (finished)
                {
                    wave.Done = true;
                    if (WavesStarted < WaveCount && NextWaveTimer <= 0f)
                    {
                        NextWaveTimer = WaveGap;
                        RaiseChanged();
                    }
                }
            }

            if (NextWaveTimer > 0f)
            {
                NextWaveTimer -= dt;
                if (NextWaveTimer <= 0f)
                    StartNextWave();
            }
        }

        void StartNextWave()
        {
            if (WavesStarted >= WaveCount)
                return;
            var def = Level.waves[WavesStarted];
            activeWaves.Add(new ActiveWave { def = def, spawned = new int[def.groups.Count] });
            WavesStarted++;
            NextWaveTimer = -1f;
            State = BattleState.Running;
            Sound(Sfx.Horn, 0.8f);
            RaiseChanged();
        }

        void CheckVictory(float dt)
        {
            if (State != BattleState.Running || WavesStarted < WaveCount)
                return;
            foreach (var wave in activeWaves)
                if (!wave.Done)
                    return;
            if (Enemies.Count > 0 || pendingEnemies.Count > 0)
                return;
            if (endTimer < 0f)
                endTimer = 1.2f;
            endTimer -= dt;
            if (endTimer > 0f)
                return;
            State = BattleState.Won;
            Sound(Sfx.Victory, 1f);
            if (Ended != null)
                Ended(true);
        }

        void UpdateShake(float dt)
        {
            if (battleCamera == null)
                return;
            shake = Mathf.Max(0f, shake - dt);
            var offset = shake > 0f ? (Vector3)(Random.insideUnitCircle * shake * 0.4f) : Vector3.zero;
            battleCamera.transform.position = cameraHome + offset;
        }

        void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }

        // ------------------------------------------------------------------ player actions

        /// <summary>Calls the next wave now; early calls pay coins for the time saved.</summary>
        public void CallWave()
        {
            if (!CanCallWave)
                return;
            if (NextWaveTimer > 0f)
                Coins += Mathf.CeilToInt(NextWaveTimer * EarlyCallCoinsPerSecond);
            StartNextWave();
        }

        /// <summary>Seconds-to-coins bonus the horn would pay right now.</summary>
        public int EarlyCallBonus { get { return NextWaveTimer > 0f ? Mathf.CeilToInt(NextWaveTimer * EarlyCallCoinsPerSecond) : 0; } }

        public bool Build(Slot slot, TowerDef def)
        {
            if (slot.Tower != null || Coins < def.cost || IsOver)
                return false;
            Coins -= def.cost;
            var tower = new Tower(this, slot, def);
            slot.Tower = tower;
            Towers.Add(tower);
            Effects.Smoke(slot.Position, 1f);
            Sound(Sfx.Build, 0.8f);
            RaiseChanged();
            return true;
        }

        public bool Upgrade(Tower tower, TowerDef next)
        {
            if (Coins < next.cost || IsOver || Array.IndexOf(tower.Def.upgrades, next.id) < 0)
                return false;
            Coins -= next.cost;
            tower.Upgrade(next);
            Sound(Sfx.Build, 0.8f);
            RaiseChanged();
            return true;
        }

        public void Sell(Tower tower)
        {
            if (IsOver)
                return;
            Coins += tower.SellValue;
            Towers.Remove(tower);
            tower.Slot.Tower = null;
            tower.Destroy();
            Effects.Smoke(tower.Position, 1f);
            Sound(Sfx.Coin, 0.8f);
            RaiseChanged();
        }

        public void MoveHero(Vector2 point)
        {
            if (Hero != null && Hero.Alive)
                Hero.Order(ClampToField(point));
        }

        public bool CastArrows(Vector2 point)
        {
            if (ArrowsCooldown > 0f || IsOver)
                return false;
            var bonus = 1f + Upgrades.Bonus(UpgradeDefs.SpellArrows);
            for (var i = 0; i < SpellDefs.ArrowsCount; i++)
            {
                var offset = Random.insideUnitCircle * SpellDefs.ArrowsRadius;
                var damage = Random.Range(SpellDefs.ArrowsDamageMin, SpellDefs.ArrowsDamageMax) * bonus;
                Projectiles.Add(Projectile.RainArrow(this, point + new Vector2(offset.x, offset.y * 0.7f), damage, i * 0.06f));
            }
            ArrowsCooldown = ArrowsCooldownMax;
            Sound(Sfx.Bow, 1f);
            RaiseChanged();
            return true;
        }

        public bool CastReinforce(Vector2 point)
        {
            if (ReinforceCooldown > 0f || IsOver)
                return false;
            point = ClampToField(point);
            var bonus = 1f + Upgrades.Bonus(UpgradeDefs.SpellReinforce);
            var look = new Look { main = 0x5A4A3A, accent = 0x8A5A2E, head = Headgear.Cap, weapon = Weapon.Spear };
            for (var i = 0; i < 2; i++)
            {
                var spot = point + new Vector2(i == 0 ? -0.25f : 0.25f, 0f);
                var ally = new Ally(this, AllyKind.Reinforcement, look, spot, spot)
                {
                    Lifetime = SpellDefs.ReinforceDuration,
                    DamageMin = SpellDefs.ReinforceMin * bonus,
                    DamageMax = SpellDefs.ReinforceMax * bonus,
                    Armor = 0.1f,
                    EngageRadius = 1.3f,
                };
                ally.MaxHp = ally.Hp = SpellDefs.ReinforceHp * bonus;
                Allies.Add(ally);
            }
            Effects.Smoke(point, 1f);
            ReinforceCooldown = SpellDefs.ReinforceCooldown;
            Sound(Sfx.Build, 0.6f);
            RaiseChanged();
            return true;
        }

        /// <summary>Uses an item from the player's inventory. Returns false if none is left.</summary>
        public bool UseItem(string itemId, Vector2 point)
        {
            if (IsOver || !Economy.TryUseItem(itemId))
                return false;
            switch (itemId)
            {
                case ItemIds.Nushdaru:
                    Lives += ItemDefs.NushdaruLives;
                    Sound(Sfx.Magic, 1f);
                    break;
                case ItemIds.Simorgh:
                    FreezeTimer = ItemDefs.SimorghFreeze;
                    Sound(Sfx.Whoosh, 1f);
                    break;
                case ItemIds.Naphtha:
                    Projectiles.Add(Projectile.Lobbed(this, ProjectileKind.Naphtha, point + new Vector2(-4f, 5f), point, 0.8f,
                        ItemDefs.NaphthaDamage, ItemDefs.NaphthaRadius, DamageType.True));
                    AddFire(point, ItemDefs.NaphthaRadius * 0.8f, 30f, 4f);
                    break;
                case ItemIds.Treasure:
                    Coins += ItemDefs.TreasureCoins;
                    Sound(Sfx.Coin, 1f);
                    break;
            }
            RaiseChanged();
            return true;
        }

        /// <summary>Second chance after a defeat (rewarded ad or gems): lives back and a short freeze.</summary>
        public void Revive()
        {
            if (State != BattleState.Lost)
                return;
            Revived = true;
            Lives = ShopDefs.ReviveLives;
            FreezeTimer = ItemDefs.SimorghFreeze;
            State = WavesStarted > 0 ? BattleState.Running : BattleState.Prep;
            RaiseChanged();
        }

        public bool IsOver { get { return State == BattleState.Won || State == BattleState.Lost; } }

        public void SetRally(Tower tower, Vector2 point)
        {
            var delta = point - tower.Position;
            if (delta.magnitude > tower.Range)
                point = tower.Position + delta.normalized * tower.Range;
            tower.SetRally(point);
        }

        // ------------------------------------------------------------------ events from units

        public void SpawnEnemy(EnemyDef def, int pathIndex, float distance)
        {
            if (def == null)
                return;
            var enemy = new Enemy(this, def, Mathf.Clamp(pathIndex, 0, Tracks.Length - 1), distance, hpMultiplier);
            pendingEnemies.Add(enemy);
            if (def.boss)
            {
                Sound(Sfx.Roar, 1f);
                Shake(0.3f);
                if (BossArrived != null)
                    BossArrived(def);
            }
        }

        public void EnemyKilled(Enemy enemy)
        {
            Coins += enemy.Def.bounty;
            Kills++;
            Effects.Coin(enemy.Position);
            Effects.Smoke(enemy.Position, 0.6f * enemy.Def.look.scale);
            Sound(enemy.Boss ? Sfx.Roar : Sfx.Die, enemy.Boss ? 1f : 0.35f);
            enemy.Remove();
            RaiseChanged();
        }

        public void EnemyEscaped(Enemy enemy)
        {
            Lives = Mathf.Max(0, Lives - enemy.Def.lives);
            enemy.Remove();
            Sound(Sfx.LifeLost, 0.8f);
            Shake(0.12f);
            RaiseChanged();
            if (Lives <= 0 && State != BattleState.Lost)
            {
                State = BattleState.Lost;
                Sound(Sfx.Defeat, 1f);
                if (Ended != null)
                    Ended(false);
            }
        }

        public void AddFire(Vector2 point, float radius, float dps, float seconds)
        {
            fires.Add(new GroundFire { Position = point, Radius = radius, Dps = dps, TimeLeft = seconds });
        }

        public void FireAtAlly(Vector2 from, Ally target, float damage)
        {
            Projectiles.Add(Projectile.AtAlly(this, from, target, damage));
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        /// <summary>Plays a sound, at most once every 60 ms per sound so volleys don't stack up.</summary>
        public void Sound(Sfx sfx, float volume)
        {
            float last;
            var now = Time.unscaledTime;
            if (lastSound.TryGetValue(sfx, out last) && now - last < 0.06f)
                return;
            lastSound[sfx] = now;
            AudioService.Play(sfx, volume);
        }

        // ------------------------------------------------------------------ queries

        /// <summary>The nearest point on any road, where a barracks sends its soldiers.</summary>
        public Vector2 DefaultRally(Vector2 from)
        {
            var best = from;
            var bestOff = float.MaxValue;
            foreach (var track in Tracks)
            {
                float off;
                var distance = track.Project(from, out off);
                if (off < bestOff)
                {
                    bestOff = off;
                    best = track.At(distance);
                }
            }
            return best;
        }

        public Vector2 ClampToField(Vector2 point)
        {
            return new Vector2(Mathf.Clamp(point.x, -9.2f, 9.2f), Mathf.Clamp(point.y, -5f, 4.6f));
        }

        static float Remaining(Enemy enemy)
        {
            return enemy.Track.Length - enemy.Distance;
        }

        /// <summary>The enemy closest to getting through, within range.</summary>
        public Enemy FirstEnemyInRange(Vector2 from, float range, bool hitsFlying)
        {
            Enemy best = null;
            var bestRemaining = float.MaxValue;
            var rangeSq = range * range;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || (enemy.Flying && !hitsFlying))
                    continue;
                if ((enemy.Position - from).sqrMagnitude > rangeSq)
                    continue;
                var remaining = Remaining(enemy);
                if (remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    best = enemy;
                }
            }
            return best;
        }

        public Enemy SecondEnemyInRange(Vector2 from, float range, Enemy except)
        {
            Enemy best = null;
            var bestRemaining = float.MaxValue;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || enemy == except || (enemy.Position - from).sqrMagnitude > range * range)
                    continue;
                var remaining = Remaining(enemy);
                if (remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    best = enemy;
                }
            }
            return best;
        }

        public Enemy NearestEnemyExcept(Vector2 from, float range, Enemy except)
        {
            Enemy best = null;
            var bestSq = range * range;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || enemy == except)
                    continue;
                var sq = (enemy.Position - from).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = enemy;
                }
            }
            return best;
        }

        /// <summary>A ground enemy for a melee ally near its post; unblocked enemies first.</summary>
        public Enemy FindEnemyFor(Ally ally)
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            var radiusSq = ally.EngageRadius * ally.EngageRadius;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || enemy.Flying || (enemy.Position - ally.Home).sqrMagnitude > radiusSq)
                    continue;
                var blockedByOther = enemy.Blocker != null && enemy.Blocker != ally && enemy.Blocker.Alive;
                var score = (enemy.Position - ally.Position).sqrMagnitude + (blockedByOther ? 100f : 0f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }
            return best;
        }

        public int CountEnemiesNear(Vector2 point, float radius, bool includeFlying)
        {
            var count = 0;
            foreach (var enemy in Enemies)
                if (enemy.Alive && (includeFlying || !enemy.Flying) && (enemy.Position - point).sqrMagnitude < radius * radius)
                    count++;
            return count;
        }

        public void DamageEnemies(Vector2 point, float radius, float damage, DamageType type, bool includeFlying, float stun)
        {
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive || (enemy.Flying && !includeFlying))
                    continue;
                if ((enemy.Position - point).sqrMagnitude > radius * radius)
                    continue;
                enemy.TakeDamage(damage, type);
                if (stun > 0f && enemy.Alive)
                    enemy.Stun(stun);
            }
        }

        public Ally NearestAlly(Vector2 point, float range)
        {
            Ally best = null;
            var bestSq = range * range;
            foreach (var ally in Allies)
            {
                if (!ally.Alive)
                    continue;
                var sq = (ally.Position - point).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = ally;
                }
            }
            return best;
        }

        public bool AnyAllyNear(Vector2 point, float radius)
        {
            return NearestAlly(point, radius) != null;
        }

        public void DamageAllies(Vector2 point, float radius, float damage)
        {
            foreach (var ally in Allies)
                if (ally.Alive && (ally.Position - point).sqrMagnitude < radius * radius)
                    ally.TakeDamage(damage, DamageType.Physical);
        }

        public void StunAllies(Vector2 point, float radius, float seconds, float damage)
        {
            foreach (var ally in Allies)
                if (ally.Alive && (ally.Position - point).sqrMagnitude < radius * radius)
                {
                    ally.TakeDamage(damage, DamageType.Physical);
                    ally.Stun(seconds);
                }
        }

        public void HealAllies(Vector2 point, float radius, float amount)
        {
            foreach (var ally in Allies)
                if (ally.Alive && (ally.Position - point).sqrMagnitude < radius * radius)
                {
                    ally.Heal(amount);
                    Effects.Sparkle(ally.Center, new Color(0.5f, 1f, 0.7f));
                }
        }

        public Tower NearestTower(Vector2 point, float radius)
        {
            Tower best = null;
            var bestSq = radius * radius;
            foreach (var tower in Towers)
            {
                if (tower.DisabledTimer > 0f)
                    continue;
                var sq = (tower.Position - point).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = tower;
                }
            }
            return best;
        }

        /// <summary>The build plot (with or without a tower) under a tap, if any.</summary>
        public Slot SlotAt(Vector2 point)
        {
            Slot best = null;
            var bestSq = 0.6f * 0.6f;
            foreach (var slot in Slots)
            {
                // the tap target is the plot plus the tower body above it
                var center = slot.Position + new Vector2(0f, slot.Tower != null ? 0.3f : 0f);
                var delta = point - center;
                var sq = delta.x * delta.x + delta.y * delta.y * (slot.Tower != null ? 0.45f : 1f);
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = slot;
                }
            }
            return best;
        }

        public bool IsHeroAt(Vector2 point)
        {
            return Hero != null && Hero.Alive && (Hero.Center - point).sqrMagnitude < 0.45f * 0.45f;
        }

        void OnDestroy()
        {
            if (mapSprite != null)
            {
                Destroy(mapSprite.texture);
                Destroy(mapSprite);
            }
            if (battleCamera != null)
                battleCamera.transform.position = cameraHome;
        }
    }
}
