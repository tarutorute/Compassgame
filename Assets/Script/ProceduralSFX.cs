using UnityEngine;

public class ProceduralSFX : MonoBehaviour
{
    [Header("Assign")]
    public CompassGameManager gameManager; // GameObject(CompassGameManagerが付いてるやつ)を入れる

    [Header("Audio")]
    public int sampleRate = 44100;
    [Range(0.05f, 1f)] public float master = 0.7f;

    void Awake()
    {
        if (gameManager == null) return;

        // 生成してゲームマネージャに流し込む
        gameManager.seStart = MakeStart();
        gameManager.seStop = MakeStop();
        gameManager.sePerfect = MakePerfect();
        gameManager.seGood = MakeGood();
        gameManager.seBad = MakeBad();
        gameManager.seMiss = MakeMiss();
        gameManager.seResult = MakeResult();
        gameManager.seFortune = MakeFortune();
    }

    // ---- 基本波形ユーティリティ ----
    AudioClip MakeClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
        // クリップ全体の音量調整
        for (int i = 0; i < data.Length; i++) data[i] *= master;
        clip.SetData(data, 0);
        return clip;
    }

    float[] Tone(float seconds, float freq, float amp = 0.6f, float attack = 0.01f, float release = 0.08f)
    {
        int n = Mathf.CeilToInt(seconds * sampleRate);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;
            float env = Envelope(t, seconds, attack, release);
            // 少しだけ倍音（矩形っぽさ）を足してゲームっぽく
            float s = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.75f + Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * 0.25f;
            d[i] = s * amp * env;
        }
        return d;
    }

    float[] Sweep(float seconds, float f0, float f1, float amp = 0.6f, float attack = 0.01f, float release = 0.08f)
    {
        int n = Mathf.CeilToInt(seconds * sampleRate);
        float[] d = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;
            float env = Envelope(t, seconds, attack, release);
            float f = Mathf.Lerp(f0, f1, t / seconds);
            phase += 2f * Mathf.PI * f / sampleRate;
            float s = Mathf.Sin(phase);
            d[i] = s * amp * env;
        }
        return d;
    }

    float[] NoiseClick(float seconds, float amp = 0.6f)
    {
        int n = Mathf.CeilToInt(seconds * sampleRate);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;
            // クリックは超短い減衰
            float env = Mathf.Exp(-t * 60f);
            d[i] = (Random.value * 2f - 1f) * amp * env;
        }
        return d;
    }

    float Envelope(float t, float length, float attack, float release)
    {
        float a = Mathf.Clamp01(t / Mathf.Max(attack, 0.0001f));
        float r = Mathf.Clamp01((length - t) / Mathf.Max(release, 0.0001f));
        return Mathf.Min(a, r);
    }

    float[] Mix(params float[][] parts)
    {
        int n = 0;
        for (int i = 0; i < parts.Length; i++) n = Mathf.Max(n, parts[i].Length);

        float[] d = new float[n];
        for (int p = 0; p < parts.Length; p++)
        {
            var s = parts[p];
            for (int i = 0; i < s.Length; i++) d[i] += s[i];
        }

        // クリップしないよう軽く正規化
        float max = 0f;
        for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        if (max > 1f)
        {
            float k = 1f / max;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }
        return d;
    }

    float[] Concat(params float[][] parts)
    {
        int total = 0;
        foreach (var p in parts) total += p.Length;

        float[] d = new float[total];
        int idx = 0;
        foreach (var p in parts)
        {
            System.Array.Copy(p, 0, d, idx, p.Length);
            idx += p.Length;
        }
        return d;
    }

    // ---- 各SE（ここがゲーム用の音の設計）----
    AudioClip MakeStart()
    {
        // ピッ→ピッ（上がる）
        return MakeClip("SE_Start", Concat(
            Tone(0.10f, 880f, 0.45f, 0.005f, 0.03f),
            Tone(0.14f, 1175f, 0.55f, 0.005f, 0.05f)
        ));
    }

    AudioClip MakeStop()
    {
        // カチッ + 軽い低音
        return MakeClip("SE_Stop", Mix(
            NoiseClick(0.08f, 0.45f),
            Tone(0.12f, 220f, 0.30f, 0.002f, 0.09f)
        ));
    }

    AudioClip MakePerfect()
    {
        // キラッ（短い和音）
        return MakeClip("SE_Perfect", Concat(
            Tone(0.10f, 1046.5f, 0.55f, 0.005f, 0.04f), // C6
            Tone(0.12f, 1318.5f, 0.50f, 0.005f, 0.05f)  // E6
        ));
    }

    AudioClip MakeGood()
    {
        // ポン（軽い）
        return MakeClip("SE_Good", Tone(0.14f, 880f, 0.45f, 0.005f, 0.07f));
    }

    AudioClip MakeBad()
    {
        // ブ（低め）
        return MakeClip("SE_Bad", Tone(0.18f, 392f, 0.45f, 0.005f, 0.10f));
    }

    AudioClip MakeMiss()
    {
        // ブーー（下がる）
        return MakeClip("SE_Miss", Sweep(0.22f, 300f, 140f, 0.55f, 0.005f, 0.12f));
    }

    AudioClip MakeResult()
    {
        // 結果表示：トトン
        return MakeClip("SE_Result", Concat(
            Tone(0.10f, 659f, 0.40f, 0.005f, 0.05f),
            Tone(0.12f, 784f, 0.50f, 0.005f, 0.06f)
        ));
    }

    AudioClip MakeFortune()
    {
        // 運勢：シュワッ（上がる）
        return MakeClip("SE_Fortune", Mix(
            Sweep(0.45f, 600f, 1400f, 0.38f, 0.01f, 0.18f),
            NoiseClick(0.18f, 0.18f)
        ));
    }
}
