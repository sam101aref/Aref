using System.Collections.Generic;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Story
{
    /// <summary>Someone who speaks in the story: a name and the figure used for their portrait.</summary>
    public class Speaker
    {
        public readonly LocText Name;
        /// <summary>Full-figure sprite under Art/chars, or null for no portrait.</summary>
        public readonly string Figure;
        public readonly Color NameColor;

        public Speaker(LocText name, string figure, Color nameColor)
        {
            Name = name;
            Figure = figure;
            NameColor = nameColor;
        }
    }

    public enum StageMode
    {
        /// <summary>The storyteller's coffeehouse; the painting shows on the curtain behind him.</summary>
        Coffeehouse,
        /// <summary>Inside the painting: it fills the screen within a gold frame.</summary>
        Painting,
        /// <summary>Over the running game: only dialogue boxes and portraits.</summary>
        Overlay,
    }

    /// <summary>A figure standing in a painting.</summary>
    public class Actor
    {
        public string Figure;   // sprite under Art/chars
        public float X;         // 0 = left edge of the painting, 1 = right edge
        public float Height;    // fraction of the painting's height
        public float Ground;    // where the feet stand, 0 = bottom, 1 = top
        public bool Flip;       // face left instead of right
        public float Delay;     // seconds before it fades in
    }

    public enum BeatKind
    {
        Scene,
        Line,
        Title,
        Choice,
        Reveal,
    }

    public class ChoiceOption
    {
        public LocText Text;
        /// <summary>True for what Siavosh really did; anything else is undone by the storyteller.</summary>
        public bool Faithful;
        /// <summary>What the storyteller says when this (unfaithful) option is chosen.</summary>
        public LocText Rewind;
        public int Honor;
    }

    public class Beat
    {
        public BeatKind Kind;

        // Scene
        public StageMode Mode;
        public string Painting;         // background sprite under Art/bg
        public bool Covered;            // coffeehouse only: the curtain is still under its cloth
        public float Zoom = 1.08f;      // slow push-in over the scene
        public Vector2 Pan;             // drift during the scene, in painting fractions
        public List<Actor> Actors = new List<Actor>();

        // Line
        public Speaker Speaker;
        public LocText Text;

        // Title
        public string TitleKey;         // titles/<key>_fa|_en
        public LocText Subtitle;

        // Choice
        public LocText Prompt;
        public List<ChoiceOption> Options = new List<ChoiceOption>();
    }

    /// <summary>
    /// A cutscene: scenes (a painting and the figures in it) and the lines spoken over them, plus
    /// chapter titles and story choices. Written with the fluent methods below, e.g.
    /// <c>new Cutscene("id", title).Scene(...).Say(Cast.Rostam, "…", "…")</c>.
    /// </summary>
    public class Cutscene
    {
        public readonly string Id;
        public readonly LocText Title;
        public readonly List<Beat> Beats = new List<Beat>();

        public Cutscene(string id, LocText title)
        {
            Id = id;
            Title = title;
        }

        public bool IsOverlay { get { return Beats.Count > 0 && Beats[0].Kind == BeatKind.Scene && Beats[0].Mode == StageMode.Overlay; } }

        public Cutscene Coffeehouse(string painting, bool covered = false)
        {
            Beats.Add(new Beat { Kind = BeatKind.Scene, Mode = StageMode.Coffeehouse, Painting = painting, Covered = covered, Zoom = 1.04f });
            return this;
        }

        public Cutscene Scene(string painting, params Actor[] actors)
        {
            var beat = new Beat { Kind = BeatKind.Scene, Mode = StageMode.Painting, Painting = painting };
            beat.Actors.AddRange(actors);
            Beats.Add(beat);
            return this;
        }

        public Cutscene ScenePan(string painting, Vector2 pan, float zoom, params Actor[] actors)
        {
            Scene(painting, actors);
            Beats[Beats.Count - 1].Pan = pan;
            Beats[Beats.Count - 1].Zoom = zoom;
            return this;
        }

        public Cutscene Overlay()
        {
            Beats.Add(new Beat { Kind = BeatKind.Scene, Mode = StageMode.Overlay });
            return this;
        }

        /// <summary>The storyteller pulls the cloth from the curtain.</summary>
        public Cutscene Reveal()
        {
            Beats.Add(new Beat { Kind = BeatKind.Reveal });
            return this;
        }

        public Cutscene Say(Speaker speaker, string fa, string en)
        {
            Beats.Add(new Beat { Kind = BeatKind.Line, Speaker = speaker, Text = new LocText(fa, en) });
            return this;
        }

        public Cutscene TitleCard(string key, string faSub, string enSub)
        {
            Beats.Add(new Beat { Kind = BeatKind.Title, TitleKey = key, Subtitle = faSub == null ? null : new LocText(faSub, enSub) });
            return this;
        }

        public Cutscene Choice(LocText prompt, params ChoiceOption[] options)
        {
            var beat = new Beat { Kind = BeatKind.Choice, Prompt = prompt };
            beat.Options.AddRange(options);
            Beats.Add(beat);
            return this;
        }

        public static Actor At(string figure, float x, float height = 0.62f, bool flip = false, float ground = 0.06f, float delay = 0f)
        {
            return new Actor { Figure = figure, X = x, Height = height, Flip = flip, Ground = ground, Delay = delay };
        }
    }
}
