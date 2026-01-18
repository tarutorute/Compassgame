using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CompassGameManager : MonoBehaviour
{
    [Header("Needle")]
    public NeedleSpinner spinner;
    public Transform targetMark;

    [Header("UI Panels")]
    public GameObject titlePanel;
    public GameObject gamePanel;
    public GameObject resultPanel;
    public GameObject fortunePanel;

    [Header("UI Texts")]
    public TMP_Text roundText;
    public TMP_Text judgeText;
    public TMP_Text totalText;
    public TMP_Text fortuneText;

    [Header("Judgement (degrees)")]
    public float perfectDeg = 5f;
    public float goodDeg = 12f;
    public float badDeg = 25f;

    [Header("Speed Change")]
    public float perfectMult = 2.5f;
    public float missMult = 0.88f;
    public float minSpeed = 120f;
    public float maxSpeed = 500f;

    [Header("Judge Ring (World Space UI)")]
    public RectTransform judgeRingRoot; // JudgeRingRoot を入れる
    public Image badArc;                // BadArc を入れる
    public Image goodArc;               // GoodArc を入れる
    public Image perfectArc;            // PerfectArc を入れる

    [Header("SE")]
    public AudioSource seSource;
    public AudioClip seStart;
    public AudioClip seStop;
    public AudioClip sePerfect;
    public AudioClip seGood;
    public AudioClip seBad;
    public AudioClip seMiss;
    public AudioClip seResult;
    public AudioClip seFortune;


    int round = 0;
    int totalScore = 0;

    enum State { Title, Spinning, ShowingResult, Fortune }
    State state = State.Title;

    void Start()
    {
        ShowTitle();
        UpdateJudgeUI();
    }

    void Update()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!pressed) return;

        switch (state)
        {
            case State.Title:
                StartGame();
                break;

            case State.Spinning:
                StopAndJudge();
                break;

            case State.ShowingResult:
                NextRoundOrFortune();
                break;

            case State.Fortune:
                ShowTitle();
                break;
        }
    }

    void PlaySE(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        seSource.PlayOneShot(clip);
    }

    void ShowTitle()
    {
        state = State.Title;

        if (titlePanel) titlePanel.SetActive(true);
        if (gamePanel) gamePanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);
        if (fortunePanel) fortunePanel.SetActive(false);

        round = 0;
        totalScore = 0;

        if (spinner) spinner.isSpinning = false;

        UpdateJudgeUI();
    }

    void StartGame()
    {
        state = State.Spinning;

        if (titlePanel) titlePanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
        if (resultPanel) resultPanel.SetActive(false);
        if (fortunePanel) fortunePanel.SetActive(false);

        round = 1;
        totalScore = 0;

        if (roundText) roundText.text = $"Round {round}/5";

        if (spinner) spinner.isSpinning = true;

        UpdateJudgeUI();
        PlaySE(seStart);

    }

    void StopAndJudge()
    {
        PlaySE(seStop);

        if (!spinner) return;

        spinner.isSpinning = false;

        float current = spinner.CurrentAngleY();
        float targetAngle = targetMark != null ? targetMark.eulerAngles.y : 0f;
        float diff = Mathf.Abs(Mathf.DeltaAngle(current, targetAngle));
        float uiAngleZ = judgeRingRoot != null ? judgeRingRoot.localEulerAngles.z : 0f;
        Debug.Log($"needleY={current:0.0} targetY={targetAngle:0.0} diff={diff:0.0} uiZ={uiAngleZ:0.0}");


        string judge;
        int scoreAdd;

        if (diff <= perfectDeg)
        {
            judge = "PERFECT";
            scoreAdd = 100;
            spinner.spinSpeed = Mathf.Clamp(spinner.spinSpeed * perfectMult, minSpeed, maxSpeed);
            PlaySE(sePerfect);
        }
        else if (diff <= goodDeg)
        {
            judge = "GOOD";
            scoreAdd = 60;
            PlaySE(seGood);
        }
        else if (diff <= badDeg)
        {
            judge = "BAD";
            scoreAdd = 30;
            PlaySE(seBad);
        }
        else
        {
            judge = "MISS";
            scoreAdd = 0;
            spinner.spinSpeed = Mathf.Clamp(spinner.spinSpeed * missMult, minSpeed, maxSpeed);
            PlaySE(seMiss);
        }

        totalScore += scoreAdd;

        if (judgeText) judgeText.text = $"{judge}\n+{scoreAdd}\n(diff {diff:0.0}°)";
        if (totalText) totalText.text = $"TOTAL: {totalScore}";

        state = State.ShowingResult;
        if (resultPanel) resultPanel.SetActive(true);
        PlaySE(seResult);

    }

    void NextRoundOrFortune()
    {
        if (resultPanel) resultPanel.SetActive(false);

        if (round >= 5)
        {
            ShowFortune();
            return;
        }

        round++;
        if (roundText) roundText.text = $"Round {round}/5";

        if (spinner) spinner.isSpinning = true;
        state = State.Spinning;

        // ターゲットを動かしたいならここでランダム化
        // SetRandomTarget();

        UpdateJudgeUI();
    }

    void ShowFortune()
    {
        state = State.Fortune;

        PlaySE(seFortune);


        if (gamePanel) gamePanel.SetActive(false);
        if (fortunePanel) fortunePanel.SetActive(true);

        if (fortuneText) fortuneText.text = FortuneFromScore(totalScore);
    }

    string FortuneFromScore(int score)
    {
        if (score >= 450) return $"TOTAL {score}\n大\n今日は最強の日";
        if (score >= 350) return $"TOTAL {score}\n中吉\nだいたい上手くいく";
        if (score >= 200) return $"TOTAL {score}\n小吉\n慎重にいけばOK";
        return $"TOTAL {score}\n凶\n無理せず守りで";
    }

    void UpdateJudgeUI()
    {
        // 角度に応じた扇形の大きさ
        float perfectFill = (perfectDeg * 2f) / 360f;
        float goodFill = (goodDeg * 2f) / 360f;
        float badFill = (badDeg * 2f) / 360f;

        if (perfectArc) perfectArc.fillAmount = perfectFill;
        if (goodArc) goodArc.fillAmount = goodFill;
        if (badArc) badArc.fillAmount = badFill;

        // ターゲット方向にリングを回す（UIはZ回転）
        float targetAngle = targetMark != null ? targetMark.eulerAngles.y : 0f;
        if (judgeRingRoot)
        {
            judgeRingRoot.localEulerAngles = new Vector3(0f, 0f, -targetAngle);
        }
    }

    // ターゲットを毎回ランダムにしたい場合
    // void SetRandomTarget()
    // {
    //     if (!targetMark) return;
    //     float y = Random.Range(0f, 360f);
    //     targetMark.eulerAngles = new Vector3(0f, y, 0f);
    // }
}
