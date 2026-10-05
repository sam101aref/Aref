using System;
using System.Collections.Generic;
using Arash.Art;
using UnityEngine;

namespace Arash.Story
{
    public enum Speaker
    {
        Narrator,
        Arash,
        Roshana,
        Mobad,
        Manuchehr,
        Afrasiab,
        Turanian,
        Barman,
        Garsivaz,
        WhiteDiv,
        Villager,
        Commander,
        Shaman,
        Envoy,
        Div,
    }

    /// <summary>One line of dialogue (F-19): who speaks and the string key of what they say.</summary>
    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker;
        public string textKey;
    }

    /// <summary>How a character stands in a cutscene panel.</summary>
    public enum ActorPose
    {
        Stand,
        /// <summary>Bow (or weapon) raised towards where it faces.</summary>
        Aim,
        /// <summary>Arm raised high: a staff, a sword, a greeting.</summary>
        Raise,
        Kneel,
        Fallen,
        /// <summary>Walks along its drift, bobbing.</summary>
        Walk,
    }

    /// <summary>A character in a cutscene panel. Positions are in units; the ground is at y = 0.</summary>
    [Serializable]
    public class CutsceneActor
    {
        public CharacterLook look;
        public Vector2 position;
        public bool facingRight = true;
        [Min(0.3f)] public float scale = 1f;
        public ActorPose pose;
        [Tooltip("Units per second the actor drifts during the panel.")]
        public Vector2 drift;
    }

    /// <summary>A sprite from Resources/Art/Props placed in a panel (tents, fire, banners, the Simurgh…).</summary>
    [Serializable]
    public class CutsceneProp
    {
        public string sprite;
        public Vector2 position;
        [Min(0.1f)] public float scale = 1f;
        public bool flip;
        public Vector2 drift;
        [Tooltip("Drawn in front of the characters.")]
        public bool front;
    }

    /// <summary>
    /// One illustrated panel of a cutscene (F-60): a biome at a time of day, characters and props,
    /// a slow camera move from one framing to another, and a caption, optionally spoken by someone.
    /// </summary>
    [Serializable]
    public class CutscenePanel
    {
        public Biome biome = Biome.Village;
        public TimeOfDay time = TimeOfDay.Day;
        public List<CutsceneActor> actors = new List<CutsceneActor>();
        public List<CutsceneProp> props = new List<CutsceneProp>();
        public Speaker speaker = Speaker.Narrator;
        public string captionKey;
        [Min(1f)] public float duration = 6f;
        [Tooltip("Camera centre at the start and end of the panel (ground at y = 0).")]
        public Vector2 cameraFrom = new Vector2(0f, 2.6f);
        public Vector2 cameraTo = new Vector2(0f, 2.6f);
        [Tooltip("Half the view height at the start and end; smaller is closer.")]
        public float zoomFrom = 4.4f;
        public float zoomTo = 4f;
    }

    /// <summary>
    /// A cutscene (F-20, F-60): illustrated panels shown one after another with slow camera moves
    /// and captions.
    /// Seen cutscenes are collected in the story book (F-21).
    /// </summary>
    [CreateAssetMenu(menuName = "Arash/Cutscene", fileName = "Cutscene")]
    public class CutsceneDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by the save file.")]
        public string id;
        public string titleKey;
        public List<CutscenePanel> panels = new List<CutscenePanel>();
    }
}
