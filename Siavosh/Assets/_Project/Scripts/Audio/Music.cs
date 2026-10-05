using System;

namespace Siavosh.Audio
{
    /// <summary>
    /// Generates the music: a tar-like plucked melody in Shur over a soft drone, and
    /// for battles a daf rhythm in 6/8. The melody is a seeded random walk that comes home to the
    /// tonic at the end of every four-bar phrase, so it loops naturally.
    /// </summary>
    public static class MusicComposer
    {
        const int EighthsPerBar = 6;

        // 6/8 daf pattern: dum, rest, tak, dum, tak, tak.
        static readonly char[] DafPattern = { 'D', '-', 'T', 'D', 'T', 'T' };
        static readonly int[] Steps = { -2, -1, -1, 0, 1, 1, 2 };
        static readonly int[] Lengths = { 1, 1, 2, 2, 3 };

        public static float[] Compose(int seed, float eighthsPerMinute, int bars, bool withDaf)
        {
            var eighth = 60f / eighthsPerMinute;
            var output = new float[Synth.Samples(bars * EighthsPerBar * eighth)];
            var random = new Random(seed);

            AddDrone(output);
            AddMelody(output, random, eighth, bars, seed);
            if (withDaf)
                AddDaf(output, eighth, bars, seed);

            return Synth.FadeEdges(Synth.Normalize(output, 0.8f), 0.02f);
        }

        static void AddDrone(float[] output)
        {
            var low = PersianScale.Frequency(PersianScale.TonicD4, -7);
            var phase = 0.0;
            for (var i = 0; i < output.Length; i++)
            {
                var t = i / (double)Synth.SampleRate;
                phase += 2.0 * Math.PI * low / Synth.SampleRate;
                var swell = 0.75 + 0.25 * Math.Sin(t * 0.8);
                output[i] += (float)((Math.Sin(phase) + 0.3 * Math.Sin(phase * 2.0)) * 0.12 * swell);
            }
        }

        static void AddMelody(float[] output, Random random, float eighth, int bars, int seed)
        {
            var degree = 0;
            var noteSeed = seed;
            for (var bar = 0; bar < bars; bar++)
            {
                var position = 0;
                var phraseEnd = bar % 4 == 3;
                while (position < EighthsPerBar)
                {
                    var length = Math.Min(Lengths[random.Next(Lengths.Length)], EighthsPerBar - position);
                    if (phraseEnd && position + length >= EighthsPerBar - 2)
                    {
                        // Cadence: settle on the tonic for the rest of the bar.
                        degree = 0;
                        length = EighthsPerBar - position;
                    }
                    else
                    {
                        degree = Math.Max(-1, Math.Min(8, degree + Steps[random.Next(Steps.Length)]));
                    }

                    var start = Synth.Samples((bar * EighthsPerBar + position) * eighth);
                    var duration = length * eighth;
                    var frequency = PersianScale.Frequency(PersianScale.TonicD4, degree);
                    Synth.Mix(output, Synth.Pluck(frequency, duration + 0.5f, 0.996f, ++noteSeed), start, 0.5f);

                    // Long notes get the tar's tremolo ("riz"): quick re-plucks.
                    if (length >= 3)
                    {
                        var stroke = eighth / 2f;
                        for (var t = stroke; t < duration; t += stroke)
                            Synth.Mix(output, Synth.Pluck(frequency, stroke + 0.2f, 0.994f, ++noteSeed), start + Synth.Samples(t), 0.3f);
                    }
                    position += length;
                }
            }
        }

        static void AddDaf(float[] output, float eighth, int bars, int seed)
        {
            for (var bar = 0; bar < bars; bar++)
            {
                for (var step = 0; step < EighthsPerBar; step++)
                {
                    var start = Synth.Samples((bar * EighthsPerBar + step) * eighth);
                    switch (DafPattern[step])
                    {
                        case 'D':
                            Synth.Mix(output, Synth.Tone(70f, 0.4f, 0.6f, 9f), start, 0.7f);
                            Synth.Mix(output, Synth.LowPass(Synth.Noise(0.08f, 40f, seed + bar * 7 + step), 0.2f), start, 0.4f);
                            break;
                        case 'T':
                            Synth.Mix(output, Synth.HighPass(Synth.Noise(0.06f, 55f, seed + bar * 13 + step), 0.3f), start, 0.35f);
                            // the daf's ring of jingles
                            Synth.Mix(output, Synth.HighPass(Synth.Noise(0.18f, 18f, seed + bar * 17 + step), 0.6f), start, 0.08f);
                            break;
                    }
                }
            }
        }
    }

    public enum Sfx
    {
        Click,
        Swing,
        HitFlesh,
        HitWood,
        Block,
        Parry,
        BowRelease,
        ArrowHit,
        Jump,
        Land,
        Coin,
        Leaf,
        Heal,
        Hurt,
        Victory,
        Defeat,
        FarrReady,
        FarrBlast,
        Hoof,
        Honor,
        Page,
    }

    /// <summary>Builds each sound effect's samples.</summary>
    public static class SfxLibrary
    {
        public static float[] Build(Sfx sfx)
        {
            float[] s;
            switch (sfx)
            {
                case Sfx.Click:
                    s = Synth.HighPass(Synth.Noise(0.03f, 200f, 6), 0.5f);
                    break;
                case Sfx.Swing:
                    s = Synth.Swell(Synth.HighPass(Synth.LowPass(Synth.Noise(0.22f, 0f, 2), 0.35f), 0.05f));
                    break;
                case Sfx.HitFlesh:
                    s = Synth.Tone(90f, 0.2f, 0.2f, 25f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.08f, 50f, 4), 0.1f), 0, 0.8f);
                    break;
                case Sfx.HitWood:
                    s = Synth.Tone(180f, 0.25f, 0.3f, 30f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.05f, 80f, 3), 0.3f), 0, 0.6f);
                    break;
                case Sfx.Block:
                    s = Synth.Tone(620f, 0.35f, 0f, 14f);
                    Synth.Mix(s, Synth.Tone(980f, 0.3f, 0f, 18f), 0, 0.5f);
                    Synth.Mix(s, Synth.HighPass(Synth.Noise(0.04f, 90f, 7), 0.4f), 0, 0.5f);
                    break;
                case Sfx.Parry:
                    s = Synth.Tone(1320f, 0.6f, 0f, 7f);
                    Synth.Mix(s, Synth.Tone(1980f, 0.6f, 0f, 9f), 0, 0.5f);
                    Synth.Mix(s, Synth.Tone(820f, 0.4f, 0f, 12f), 0, 0.5f);
                    break;
                case Sfx.BowRelease:
                    s = Synth.Tone(110f, 0.35f, 0.5f, 12f);
                    Synth.Mix(s, Synth.HighPass(Synth.Noise(0.06f, 60f, 1), 0.2f), 0, 0.5f);
                    break;
                case Sfx.ArrowHit:
                    s = Synth.Tone(200f, 0.2f, 0.4f, 35f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.04f, 90f, 8), 0.4f), 0, 0.6f);
                    break;
                case Sfx.Jump:
                    s = Synth.Swell(Synth.LowPass(Synth.Noise(0.12f, 0f, 9), 0.15f));
                    break;
                case Sfx.Land:
                    s = Synth.Tone(70f, 0.15f, 0.3f, 40f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.06f, 60f, 10), 0.08f), 0, 0.7f);
                    break;
                case Sfx.Coin:
                    s = Arpeggio(new[] { 7, 9 }, 0.05f, 0.4f, 40);
                    break;
                case Sfx.Leaf:
                    s = Arpeggio(new[] { 4, 7, 9, 11 }, 0.08f, 1.0f, 50);
                    Synth.Mix(s, Synth.Swell(Synth.HighPass(Synth.Noise(0.25f, 0f, 11), 0.3f)), 0, 0.3f);
                    break;
                case Sfx.Heal:
                    s = Arpeggio(new[] { 0, 4, 7 }, 0.06f, 0.7f, 60);
                    break;
                case Sfx.Hurt:
                    s = Synth.Tone(140f, 0.25f, -0.3f, 18f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.1f, 40f, 12), 0.15f), 0, 0.7f);
                    break;
                case Sfx.Victory:
                    s = Arpeggio(new[] { 0, 2, 4, 7 }, 0.13f, 1.6f, 10);
                    break;
                case Sfx.Defeat:
                    s = Arpeggio(new[] { 4, 3, 1, 0 }, 0.3f, 2f, 20);
                    break;
                case Sfx.FarrReady:
                    s = Arpeggio(new[] { 7, 9, 11 }, 0.07f, 0.9f, 30);
                    break;
                case Sfx.FarrBlast:
                    s = Synth.Tone(55f, 1.0f, 1.2f, 5f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.6f, 6f, 13), 0.06f), 0, 0.9f);
                    Synth.Mix(s, Arpeggio(new[] { 0, 4, 7, 11 }, 0.04f, 1.0f, 70), 0, 0.4f);
                    break;
                case Sfx.Hoof:
                    s = Synth.Tone(60f, 0.12f, 0.5f, 45f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.05f, 70f, 14), 0.1f), 0, 0.6f);
                    break;
                case Sfx.Honor:
                    s = Arpeggio(new[] { 0, 2, 4, 7, 9 }, 0.09f, 1.6f, 80);
                    break;
                default:
                    s = Synth.Swell(Synth.HighPass(Synth.LowPass(Synth.Noise(0.3f, 0f, 15), 0.5f), 0.2f));
                    break;
            }
            return Synth.FadeEdges(Synth.Normalize(s, 0.9f), 0.003f);
        }

        static float[] Arpeggio(int[] degrees, float spacing, float seconds, int seed)
        {
            var s = new float[Synth.Samples(seconds)];
            for (var i = 0; i < degrees.Length; i++)
            {
                var frequency = PersianScale.Frequency(PersianScale.TonicD4, degrees[i]);
                Synth.Mix(s, Synth.Pluck(frequency, seconds - i * spacing, 0.996f, seed + i), Synth.Samples(i * spacing), 0.6f);
            }
            return s;
        }
    }
}
