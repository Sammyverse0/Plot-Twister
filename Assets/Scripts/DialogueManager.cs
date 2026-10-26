using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;
    [TextArea(2, 4)] public string text;
}

[System.Serializable]
public class DialogueSequence
{
    public string id;
    public List<DialogueLine> lines = new();
}

public class DialogueManager : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("UI")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Typewriter")]
    [SerializeField] private float charsPerSecond = 35f;
    [SerializeField] private float holdTime = 1.5f;
    [SerializeField] private float startDelay = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;
    [SerializeField] private int soundEveryNChars = 2;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    [Header("Sequences")]
    [SerializeField] private List<DialogueSequence> sequences = new()
    {
        new DialogueSequence
        {
            id = "Intro",
            lines = new List<DialogueLine>
            {
                new DialogueLine { speakerName = "Farmer", text = "I wonder why the crops are dry... I should check the generator." },
                new DialogueLine { speakerName = "Farmer", text = "Better twist the plots to get the water flowing." }
            }
        },
        new DialogueSequence
        {
            id = "AlienRise",
            lines = new List<DialogueLine>
            {
                new DialogueLine { speakerName = "Farmer", text = "What the heck?! ALIENS IN MY PLOTS?!" }
            }
        }
    };

    private Coroutine _routine;
    private CanvasGroup _group;
    private bool _tutorialSeen;
    private bool _introPlayed;

    private void Awake()
    {
        if (dialogueBox == null || nameText == null || bodyText == null)
        {
            Debug.LogError("[Dialogue] dialogueBox, nameText or bodyText is not assigned.", this);
            return;
        }

        dialogueBox.SetActive(true);
        _group = dialogueBox.GetComponent<CanvasGroup>();
        if (_group == null) _group = dialogueBox.AddComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Update()
    {
        if (tutorialPanel == null || _introPlayed) return;

        if (tutorialPanel.activeInHierarchy)
        {
            _tutorialSeen = true;
        }
        else if (_tutorialSeen)
        {
            _introPlayed = true;
            Debug.Log("[Dialogue] Tutorial closed, playing intro.");
            Play("Intro");
        }
    }

    public void Play(string id)
    {
        if (_group == null)
        {
            Debug.LogError("[Dialogue] Not set up, check the UI slots.", this);
            return;
        }

        DialogueSequence sequence = sequences.Find(s => s.id == id);
        if (sequence == null)
        {
            Debug.LogWarning($"[Dialogue] No sequence with id '{id}'.", this);
            return;
        }

        Stop();
        Debug.Log($"[Dialogue] Playing '{id}'.");
        _routine = StartCoroutine(RunSequence(sequence));
    }

    public void Stop()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        SetVisible(false);
    }

    [ContextMenu("Test Intro")]
    private void TestIntro() => Play("Intro");

    private void SetVisible(bool visible)
    {
        if (_group == null) return;

        _group.alpha = visible ? 1f : 0f;
        _group.blocksRaycasts = visible;
        _group.interactable = visible;
    }

    private IEnumerator RunSequence(DialogueSequence sequence)
    {
        yield return new WaitForSecondsRealtime(startDelay);

        SetVisible(true);

        foreach (DialogueLine line in sequence.lines)
        {
            yield return TypeLine(line);
            yield return new WaitForSecondsRealtime(holdTime);
        }

        SetVisible(false);
        _routine = null;
    }

    public void PlayIntroOnce()
    {
        if (_introPlayed) return;
        _introPlayed = true;
        Play("Intro");
    }
    private IEnumerator TypeLine(DialogueLine line)
    {
        nameText.text = line.speakerName;
        if (portraitImage != null && line.portrait != null)
            portraitImage.sprite = line.portrait;

        bodyText.text = line.text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 1; i <= total; i++)
        {
            bodyText.maxVisibleCharacters = i;

            char c = bodyText.textInfo.characterInfo[i - 1].character;
            if (!char.IsWhiteSpace(c) && i % Mathf.Max(1, soundEveryNChars) == 0)
                PlayTypeSound();

            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private void PlayTypeSound()
    {
        if (audioSource == null || typeSound == null) return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(typeSound);
    }
}