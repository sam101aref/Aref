using System;
using System.Collections.Generic;
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
    }

    /// <summary>One line of dialogue (F-19): who speaks and the string key of what they say.</summary>
    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker;
        public string textKey;
    }

    public enum CutsceneMotif
    {
        Village,
        Army,
        Forest,
        Camp,
        Mountains,
        Damavand,
        ArrowFlight,
        River,
        Celebration,
    }

    /// <summary>One illustrated panel of a cutscene: a scene motif, colours and a caption.</summary>
    [Serializable]
    public class CutscenePanel
    {
        public CutsceneMotif motif;
        public string captionKey;
        public Color sky = new Color(0.98f, 0.82f, 0.55f);
        public Color land = new Color(0.55f, 0.43f, 0.28f);
        [Min(1f)] public float duration = 6f;
    }

    /// <summary>
    /// A cutscene (F-20): panels shown one after another with slow parallax motion and captions.
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
