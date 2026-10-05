using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum Backdrop
    {
        Palace,
        River,
        Steppe,
        Battlefield,
        Forest,
        Cave,
        Castle,
        Camp,
        Fortress,
        Mountain,
        Damavand,
    }

    public class Slide
    {
        public Backdrop backdrop;
        /// <summary>Character id from <see cref="StoryDefs.Characters"/>, or null for the narrator.</summary>
        public string speaker;
        public string textKey;
    }

    public class CutsceneDef
    {
        public string id;
        public List<Slide> slides = new List<Slide>();
    }

    /// <summary>
    /// The story told between battles. Each slide is a painted backdrop, an optional speaker
    /// portrait and one line (text keys are "story.&lt;cutscene&gt;.&lt;n&gt;").
    /// </summary>
    public static class StoryDefs
    {
        /// <summary>Looks for characters who only appear in the story (heroes and bosses reuse theirs).</summary>
        public static readonly Dictionary<string, Look> Characters = new Dictionary<string, Look>
        {
            { "kavus", new Look { main = 0x6A1E5A, accent = 0xE8C35A, head = Headgear.Crown, weapon = Weapon.Staff, beard = 0x8A8A8A, cape = 0xB0302A } },
            { "kaykhosrow", new Look { main = 0x1E4A7A, accent = 0xE8C35A, head = Headgear.Crown, weapon = Weapon.Sword, beard = 0x3A2418, cape = 0x6A2A7A } },
            { "sohrab", new Look { main = 0x7A5A2A, accent = 0xC9A227, skin = 0xE6B98E, head = Headgear.Helmet, weapon = Weapon.Sword, shield = true, cape = 0x8E2B25 } },
        };

        public static readonly List<CutsceneDef> All = new List<CutsceneDef>
        {
            Scene("prologue",
                S(Backdrop.River, null), S(Backdrop.Fortress, "afrasiab"), S(Backdrop.Palace, HeroIds.Zal),
                S(Backdrop.Steppe, HeroIds.Rostam), S(Backdrop.River, null)),
            Scene("invasion_end",
                S(Backdrop.Battlefield, null), S(Backdrop.Fortress, "afrasiab")),
            Scene("haftkhan",
                S(Backdrop.Palace, "kavus"), S(Backdrop.Cave, "divsepid"), S(Backdrop.Steppe, HeroIds.Zal), S(Backdrop.Forest, HeroIds.Rostam)),
            Scene("haftkhan_end",
                S(Backdrop.Cave, null), S(Backdrop.Cave, null), S(Backdrop.Palace, "kavus")),
            Scene("sohrab",
                S(Backdrop.Camp, null), S(Backdrop.Camp, "sohrab"), S(Backdrop.Castle, HeroIds.Gordafarid), S(Backdrop.Camp, "houman")),
            Scene("sohrab_end",
                S(Backdrop.Battlefield, null), S(Backdrop.Battlefield, "sohrab"), S(Backdrop.Battlefield, HeroIds.Rostam), S(Backdrop.Battlefield, null)),
            Scene("siavash",
                S(Backdrop.Palace, null), S(Backdrop.Steppe, null), S(Backdrop.Palace, "kaykhosrow"), S(Backdrop.Mountain, HeroIds.Rostam)),
            Scene("ending",
                S(Backdrop.Fortress, null), S(Backdrop.Palace, "kaykhosrow"), S(Backdrop.Palace, null), S(Backdrop.Damavand, null)),
            Scene("damavand",
                S(Backdrop.Damavand, null), S(Backdrop.Damavand, null), S(Backdrop.Damavand, "zahhak"), S(Backdrop.Damavand, HeroIds.Rostam)),
            Scene("damavand_end",
                S(Backdrop.Damavand, null)),
        };

        public static CutsceneDef Get(string id)
        {
            return string.IsNullOrEmpty(id) ? null : All.Find(c => c.id == id);
        }

        /// <summary>The look of a story character, hero or enemy, or null for the narrator.</summary>
        public static Look LookOf(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;
            Look look;
            if (Characters.TryGetValue(characterId, out look))
                return look;
            var hero = HeroDefs.Get(characterId);
            if (hero != null)
                return hero.look;
            var enemy = EnemyDefs.Get(characterId);
            return enemy != null ? enemy.look : null;
        }

        /// <summary>Name key of a story character, hero or enemy.</summary>
        public static string NameKeyOf(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;
            if (Characters.ContainsKey(characterId))
                return "character." + characterId;
            var hero = HeroDefs.Get(characterId);
            if (hero != null)
                return hero.NameKey;
            var enemy = EnemyDefs.Get(characterId);
            return enemy != null ? enemy.NameKey : null;
        }

        static Slide S(Backdrop backdrop, string speaker)
        {
            return new Slide { backdrop = backdrop, speaker = speaker };
        }

        static CutsceneDef Scene(string id, params Slide[] slides)
        {
            var scene = new CutsceneDef { id = id };
            for (var i = 0; i < slides.Length; i++)
            {
                slides[i].textKey = "story." + id + "." + (i + 1);
                scene.slides.Add(slides[i]);
            }
            return scene;
        }
    }
}
