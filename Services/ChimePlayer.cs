using System.IO;
using System.Media;
using System.Text;

namespace FocusTracker.Services;

/// <summary>
/// Generates two distinct chimes in memory (no sound files needed): a calm,
/// descending cue when a focus session ends, and a brighter rising cue when a
/// break ends. PCM is synthesized at runtime, so it stays fully offline.
/// </summary>
public class ChimePlayer
{
    private const int SampleRate = 44100;

    private readonly SoundPlayer _focusDone;
    private readonly SoundPlayer _breakDone;

    public ChimePlayer()
    {
        // Focus finished -> relaxed descending G5 -> C5 ("time to rest").
        _focusDone = Build((784, 200), (0, 40), (523, 340));
        // Break finished -> energizing rising C5 -> G5 -> C6 ("back to it").
        _breakDone = Build((523, 150), (0, 35), (784, 150), (0, 30), (1046, 300));
    }

    public void PlayFocusFinished() => SafePlay(_focusDone);
    public void PlayBreakFinished() => SafePlay(_breakDone);

    private static void SafePlay(SoundPlayer p)
    {
        try { p.Play(); } catch { /* no audio device available */ }
    }

    private static SoundPlayer Build(params (double freq, int ms)[] tones)
    {
        var wav = WrapWav(Synthesize(tones));
        var player = new SoundPlayer(new MemoryStream(wav));
        try { player.Load(); } catch { /* ignore load issues */ }
        return player;
    }
    private static short[] Synthesize((double freq, int ms)[] tones)
    {
        var samples = new List<short>();
        foreach (var (freq, ms) in tones)
        {
            int n = SampleRate * ms / 1000;
            for (int i = 0; i < n; i++)
            {
                double env = Envelope(i, n);
                double v = freq <= 0 ? 0 : Math.Sin(2 * Math.PI * freq * i / SampleRate);
                samples.Add((short)(v * env * 0.35 * short.MaxValue));
            }
        }
        return samples.ToArray();
    }

    private static double Envelope(int i, int n)
    {
        // Short fade-in and linear fade-out to avoid clicks at the edges.
        int attack = Math.Min(400, n / 8);
        if (i < attack) return (double)i / Math.Max(1, attack);
        double t = (double)(i - attack) / Math.Max(1, n - attack);
        return Math.Max(0, 1 - t);
    }

    private static byte[] WrapWav(short[] samples)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        int dataBytes = samples.Length * 2;

        Tag(bw, "RIFF");
        bw.Write(36 + dataBytes);
        Tag(bw, "WAVE");
        Tag(bw, "fmt ");
        bw.Write(16);              // PCM header size
        bw.Write((short)1);        // PCM format
        bw.Write((short)1);        // mono
        bw.Write(SampleRate);
        bw.Write(SampleRate * 2);  // byte rate
        bw.Write((short)2);        // block align
        bw.Write((short)16);       // bits per sample
        Tag(bw, "data");
        bw.Write(dataBytes);
        foreach (var s in samples) bw.Write(s);

        bw.Flush();
        return ms.ToArray();
    }

    private static void Tag(BinaryWriter bw, string tag) => bw.Write(Encoding.ASCII.GetBytes(tag));
}
