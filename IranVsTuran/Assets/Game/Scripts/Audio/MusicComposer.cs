using System;

namespace IranVsTuran.Audio
{
    /// <summary>
    /// Generates placeholder music (F-35): a tar-like plucked melody in Shur over a soft drone, and
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
}
