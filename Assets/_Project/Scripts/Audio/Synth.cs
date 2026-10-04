using System;

namespace Arash.Audio
{
    /// <summary>
    /// Tiny sound synthesis toolkit (F-35): plucked strings (Karplus–Strong), decaying tones and
    /// noise, filters and mixing. Everything is mono float PCM at <see cref="SampleRate"/>.
    /// Placeholder audio until recorded Persian instruments are added.
    /// </summary>
    public static class Synth
    {
        public const int SampleRate = 22050;

        public static int Samples(float seconds)
        {
            return Math.Max(1, (int)(seconds * SampleRate));
        }

        /// <summary>Plucked string: a burst of noise circulating in a delay line that loses energy.</summary>
        public static float[] Pluck(float frequency, float seconds, float sustain, int seed)
        {
            var output = new float[Samples(seconds)];
            var period = Math.Max(2, (int)Math.Round(SampleRate / frequency));
            var line = new float[period];
            var random = new Random(seed);
            for (var i = 0; i < period; i++)
                line[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            var index = 0;
            for (var i = 0; i < output.Length; i++)
            {
                var current = line[index];
                var next = line[(index + 1) % period];
                line[index] = sustain * 0.5f * (current + next);
                output[i] = current;
                index = (index + 1) % period;
            }
            return output;
        }

        /// <summary>A sine that starts sharp (pitch drop) and fades exponentially: drums and twangs.</summary>
        public static float[] Tone(float frequency, float seconds, float pitchDrop, float decay)
        {
            var output = new float[Samples(seconds)];
            var phase = 0.0;
            for (var i = 0; i < output.Length; i++)
            {
                var t = i / (double)SampleRate;
                var f = frequency * (1.0 + pitchDrop * Math.Exp(-t * 25.0));
                phase += 2.0 * Math.PI * f / SampleRate;
                output[i] = (float)(Math.Sin(phase) * Math.Exp(-t * decay));
            }
            return output;
        }

        /// <summary>White noise with an exponential fade.</summary>
        public static float[] Noise(float seconds, float decay, int seed)
        {
            var output = new float[Samples(seconds)];
            var random = new Random(seed);
            for (var i = 0; i < output.Length; i++)
            {
                var t = i / (double)SampleRate;
                output[i] = (float)((random.NextDouble() * 2.0 - 1.0) * Math.Exp(-t * decay));
            }
            return output;
        }

        /// <summary>One-pole low-pass; amount near 0 is dark, near 1 is unfiltered.</summary>
        public static float[] LowPass(float[] samples, float amount)
        {
            var y = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                y += amount * (samples[i] - y);
                samples[i] = y;
            }
            return samples;
        }

        public static float[] HighPass(float[] samples, float amount)
        {
            var low = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                low += amount * (samples[i] - low);
                samples[i] -= low;
            }
            return samples;
        }

        /// <summary>Rise and fall over the whole sound (sine window), e.g. a whoosh.</summary>
        public static float[] Swell(float[] samples)
        {
            for (var i = 0; i < samples.Length; i++)
                samples[i] *= (float)Math.Sin(Math.PI * i / samples.Length);
            return samples;
        }

        /// <summary>Adds <paramref name="source"/> into <paramref name="target"/> from a sample offset.</summary>
        public static void Mix(float[] target, float[] source, int offset, float gain)
        {
            for (var i = 0; i < source.Length; i++)
            {
                var j = offset + i;
                if (j < 0)
                    continue;
                if (j >= target.Length)
                    break;
                target[j] += source[i] * gain;
            }
        }

        public static float[] Normalize(float[] samples, float peak)
        {
            var max = 0f;
            foreach (var s in samples)
                max = Math.Max(max, Math.Abs(s));
            if (max > 0f)
            {
                var gain = peak / max;
                for (var i = 0; i < samples.Length; i++)
                    samples[i] *= gain;
            }
            return samples;
        }

        /// <summary>Short fades at both ends so clips and loops start and stop without clicks.</summary>
        public static float[] FadeEdges(float[] samples, float seconds)
        {
            var n = Math.Min(Samples(seconds), samples.Length / 2);
            for (var i = 0; i < n; i++)
            {
                var k = i / (float)n;
                samples[i] *= k;
                samples[samples.Length - 1 - i] *= k;
            }
            return samples;
        }
    }

    /// <summary>
    /// Dastgah-e Shur, the most common Persian mode, on D. Its second degree is "koron": a quarter
    /// tone between E-flat and E, which gives Persian melodies their colour.
    /// </summary>
    public static class PersianScale
    {
        /// <summary>Cents above the tonic: D, E-koron, F, G, A, B-flat, C.</summary>
        public static readonly int[] ShurCents = { 0, 150, 300, 500, 700, 800, 1000 };

        public const float TonicD4 = 293.66f;

        /// <summary>Frequency of a scale degree; degrees beyond 0–6 continue into other octaves.</summary>
        public static float Frequency(float tonic, int degree)
        {
            var octave = (int)Math.Floor(degree / 7.0);
            var step = degree - octave * 7;
            var cents = ShurCents[step] + 1200 * octave;
            return (float)(tonic * Math.Pow(2.0, cents / 1200.0));
        }
    }
}
