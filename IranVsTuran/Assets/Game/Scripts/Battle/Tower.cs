using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>A build plot on the map; holds at most one tower.</summary>
    public class Slot
    {
        public readonly Vector2 Position;
        public Tower Tower;
        public readonly GameObject Root;

        public Slot(Battlefield field, Vector2 position)
        {
            Position = position;
            var renderer = new GameObject("Slot").AddComponent<SpriteRenderer>();
            renderer.sprite = ArtLibrary.Slot();
            renderer.sortingOrder = Depth.Ground + 10;
            Root = renderer.gameObject;
            Root.transform.SetParent(field.transform, false);
            Root.transform.localPosition = new Vector3(position.x, position.y - 0.15f, 0f);
        }
    }

    /// <summary>
    /// A built tower. Shooting towers pick the enemy furthest along its path within range;
    /// barracks keep a squad of soldiers at their rally point. Meta upgrades from the Armory are
    /// applied when the tower is built or upgraded.
    /// </summary>
    public class Tower
    {
        const float ShooterHeight = 0.72f;

        readonly Battlefield field;
        public TowerDef Def { get; private set; }
        public readonly Slot Slot;
        public Vector2 Position { get { return Slot.Position; } }
        public int Spent { get; private set; }
        public float DisabledTimer;
        public Vector2 Rally { get; private set; }
        public readonly List<Ally> Soldiers = new List<Ally>();

        public float Range { get; private set; }
        float damageMultiplier = 1f;
        float interval;
        float splash;
        float cooldown;
        float recoil;
        float time;

        readonly GameObject root;
        readonly SpriteRenderer sprite;
        readonly List<SpriteRenderer> shooters = new List<SpriteRenderer>();
        SpriteRenderer flame;

        public Tower(Battlefield field, Slot slot, TowerDef def)
        {
            this.field = field;
            Slot = slot;
            root = new GameObject("Tower");
            root.transform.SetParent(field.transform, false);
            root.transform.localPosition = new Vector3(slot.Position.x, slot.Position.y - 0.15f, 0f);
            sprite = root.AddComponent<SpriteRenderer>();
            Slot.Root.SetActive(false);
            Rally = field.DefaultRally(slot.Position);
            Apply(def);
            Spent = def.cost;
        }

        public bool CanUpgrade { get { return Def.upgrades.Length > 0; } }
        public int SellValue { get { return Mathf.RoundToInt(Spent * TowerDefs.SellRefund); } }

        public void Upgrade(TowerDef next)
        {
            Spent += next.cost;
            Apply(next);
            field.Effects.Sparkle(Position + new Vector2(0f, 0.6f), new Color(1f, 0.9f, 0.5f));
        }

        void Apply(TowerDef def)
        {
            Def = def;
            sprite.sprite = ArtLibrary.Tower(def);
            sprite.sortingOrder = Depth.Of(Position.y);

            var family = def.family;
            var rangeBonus = family == TowerFamily.Archer ? Upgrades.Bonus(UpgradeDefs.ArcherRange) : 0f;
            Range = def.range * (1f + rangeBonus);
            damageMultiplier = 1f + (family == TowerFamily.Archer ? Upgrades.Bonus(UpgradeDefs.ArcherDamage)
                : family == TowerFamily.Mage ? Upgrades.Bonus(UpgradeDefs.MageDamage)
                : family == TowerFamily.Artillery ? Upgrades.Bonus(UpgradeDefs.ArtilleryDamage) : 0f);
            interval = def.interval * (family == TowerFamily.Mage ? 1f - Upgrades.Bonus(UpgradeDefs.MageSpeed) : 1f);
            splash = def.splash * (family == TowerFamily.Artillery ? 1f + Upgrades.Bonus(UpgradeDefs.ArtillerySplash) : 1f);

            foreach (var shooter in shooters)
                Object.Destroy(shooter.gameObject);
            shooters.Clear();
            if (flame != null)
                Object.Destroy(flame.gameObject);

            if (family == TowerFamily.Archer)
            {
                var count = def.special == TowerSpecial.DoubleShot ? 2 : 1;
                for (var i = 0; i < count; i++)
                {
                    var shooter = new GameObject("Archer").AddComponent<SpriteRenderer>();
                    shooter.sprite = ArtLibrary.Character(TowerArt.ArcherLook(def.tier));
                    shooter.transform.SetParent(root.transform, false);
                    var offset = count == 1 ? 0f : (i == 0 ? -0.18f : 0.18f);
                    shooter.transform.localPosition = new Vector3(offset, ShooterHeight - (def.tier == 1 ? 0.05f : 0f), 0f);
                    shooter.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                    shooter.sortingOrder = sprite.sortingOrder + 1;
                    shooters.Add(shooter);
                }
            }
            else if (family == TowerFamily.Mage)
            {
                flame = new GameObject("Flame").AddComponent<SpriteRenderer>();
                flame.sprite = ArtLibrary.Flame();
                flame.transform.SetParent(root.transform, false);
                flame.transform.localPosition = new Vector3(0f, def.tier == 1 ? 0.68f : 0.86f, 0f);
                flame.sortingOrder = sprite.sortingOrder + 1;
                if (def.id == "mage4b")
                    flame.color = new Color(0.6f, 0.9f, 1f);
            }
            else if (family == TowerFamily.Barracks)
            {
                UpdateSquad();
            }
        }

        void UpdateSquad()
        {
            var hpMultiplier = 1f + Upgrades.Bonus(UpgradeDefs.BarracksHp);
            var respawn = Def.respawn * (1f - Upgrades.Bonus(UpgradeDefs.BarracksRespawn));
            var firstNew = Soldiers.Count;
            while (Soldiers.Count < Def.soldiers)
            {
                var soldier = new Ally(field, AllyKind.Soldier, SoldierLook(Def), Position, Rally) { Owner = this };
                Soldiers.Add(soldier);
                field.Allies.Add(soldier);
            }
            for (var i = 0; i < Soldiers.Count; i++)
            {
                var soldier = Soldiers[i];
                var maxHp = Def.soldierHp * hpMultiplier;
                if (i >= firstNew)
                    soldier.Hp = maxHp;
                else if (soldier.Hp > 0f && soldier.MaxHp > 0f)
                    soldier.Hp = soldier.Hp / soldier.MaxHp * maxHp;
                soldier.MaxHp = maxHp;
                soldier.Armor = Def.soldierArmor;
                soldier.DamageMin = Def.soldierMin;
                soldier.DamageMax = Def.soldierMax;
                soldier.RespawnDelay = respawn;
                soldier.Regen = Def.special == TowerSpecial.Regen ? Def.specialValue : 2f;
                soldier.EngageRadius = Range * 0.7f;
                soldier.SpawnPoint = Position;
                soldier.Home = Rally + Formation(i, Soldiers.Count);
            }
        }

        static Look SoldierLook(TowerDef def)
        {
            if (def.id == "barracks4a")
                return new Look { main = 0x2E7A3A, accent = 0xC9A227, head = Headgear.Plume, weapon = Weapon.Mace, shield = true, beard = 0x2A1A10 };
            if (def.id == "barracks4b")
                return new Look { main = 0x6A2A7A, accent = 0xE2B13C, head = Headgear.SpikeHelm, weapon = Weapon.Spear, shield = true };
            return new Look
            {
                main = def.tier >= 3 ? 0x1E4A7Au : 0x2E5A8Bu,
                accent = 0xC9A227,
                head = def.tier >= 2 ? Headgear.Helmet : Headgear.Cap,
                weapon = Weapon.Sword,
                shield = def.tier >= 2,
            };
        }

        static Vector2 Formation(int index, int count)
        {
            if (count <= 1)
                return Vector2.zero;
            var angle = index / (float)count * Mathf.PI * 2f + 0.5f;
            return new Vector2(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.18f);
        }

        public void SetRally(Vector2 point)
        {
            Rally = point;
            for (var i = 0; i < Soldiers.Count; i++)
                Soldiers[i].Order(Rally + Formation(i, Soldiers.Count));
        }

        public void Update(float dt)
        {
            time += dt;
            recoil = Mathf.Max(0f, recoil - dt * 4f);
            var squash = 1f - Mathf.Sin(recoil * Mathf.PI) * 0.06f;
            root.transform.localScale = new Vector3(1f / squash, squash, 1f);
            if (flame != null)
                flame.transform.localScale = new Vector3(1f + Mathf.Sin(time * 13f) * 0.08f, 1f + Mathf.Sin(time * 9f) * 0.12f, 1f);

            if (DisabledTimer > 0f)
            {
                DisabledTimer -= dt;
                sprite.color = new Color(0.55f, 0.55f, 0.6f);
                if (flame != null)
                    flame.enabled = false;
                return;
            }
            sprite.color = Color.white;
            if (flame != null)
                flame.enabled = true;

            if (Def.family == TowerFamily.Barracks)
                return;

            cooldown -= dt;
            if (cooldown > 0f)
                return;

            var target = field.FirstEnemyInRange(Position, Range, Def.CanHitFlying);
            if (target == null)
                return;
            cooldown = interval;
            Fire(target);
        }

        Vector2 Muzzle { get { return Position + new Vector2(0f, Def.family == TowerFamily.Artillery ? 0.55f : 0.95f); } }

        float RollDamage()
        {
            return Random.Range(Def.damageMin, Def.damageMax) * damageMultiplier;
        }

        void Fire(Enemy target)
        {
            switch (Def.family)
            {
                case TowerFamily.Archer:
                    var damage = RollDamage();
                    var crit = Def.special == TowerSpecial.Crit && Random.value < Def.specialChance;
                    if (crit)
                        damage *= Def.specialValue;
                    field.Projectiles.Add(Projectile.Homing(field, ProjectileKind.Arrow, Muzzle, target, damage, DamageType.Physical, crit));
                    if (Def.special == TowerSpecial.DoubleShot)
                    {
                        var second = field.SecondEnemyInRange(Position, Range, target);
                        field.Projectiles.Add(Projectile.Homing(field, ProjectileKind.Arrow, Muzzle, second ?? target, RollDamage(), DamageType.Physical));
                    }
                    foreach (var shooter in shooters)
                        shooter.flipX = target.Position.x < Position.x;
                    field.Sound(Audio.Sfx.Bow, 0.35f);
                    break;

                case TowerFamily.Mage:
                    var bolt = Projectile.Homing(field, Def.projectile, Muzzle, target, RollDamage(), DamageType.Magic);
                    bolt.Special = Def.special;
                    bolt.SpecialValue = Def.specialValue;
                    field.Projectiles.Add(bolt);
                    field.Sound(Audio.Sfx.Magic, 0.35f);
                    break;

                case TowerFamily.Artillery:
                    var flight = 1.0f;
                    var shell = Projectile.Lobbed(field, Def.projectile, Muzzle, target.Predict(flight), flight, RollDamage(), splash);
                    shell.Special = Def.special;
                    shell.SpecialValue = Def.specialValue;
                    field.Projectiles.Add(shell);
                    recoil = 1f;
                    field.Sound(Audio.Sfx.Launch, 0.45f);
                    break;
            }
        }

        public void Destroy()
        {
            foreach (var soldier in Soldiers)
                soldier.Remove();
            Soldiers.Clear();
            Object.Destroy(root);
            Slot.Root.SetActive(true);
        }
    }
}
