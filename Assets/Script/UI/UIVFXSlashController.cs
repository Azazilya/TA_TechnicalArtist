using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Drag-and-drop UI navigation for a collection of modular slash spawners.</summary>
[DisallowMultipleComponent]
public sealed class UIVFXSlashController : MonoBehaviour
{
    [Header("Slash Effects")]
    [SerializeField] private UIVFXSlash[] slashEffects;
    [SerializeField, Min(0)] private int startingIndex;
    [SerializeField] private bool playOnEnable = true;

    [Header("UI")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button replayButton;
    [Tooltip("Optional UI Toggle. When enabled, the active slash replays continuously.")]
    [SerializeField] private Toggle loopingToggle;
    [SerializeField] private TMP_Text activeSlashName;

    [Header("Looping")]
    [SerializeField] private bool looping;

    private int currentIndex;

    public UIVFXSlash CurrentSlash =>
        HasEffects ? slashEffects[currentIndex] : null;

    private bool HasEffects => slashEffects != null && slashEffects.Length > 0;

    private void Awake()
    {
        currentIndex = HasEffects
            ? Mathf.Clamp(startingIndex, 0, slashEffects.Length - 1)
            : 0;
        UpdateLabel();
    }

    private void OnEnable()
    {
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (loopingToggle != null)
        {
            loopingToggle.SetIsOnWithoutNotify(looping);
            loopingToggle.onValueChanged.AddListener(SetLooping);
        }
        SubscribeToCurrent();
        if (playOnEnable && Application.isPlaying) Replay();
    }

    private void OnDisable()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
        if (loopingToggle != null) loopingToggle.onValueChanged.RemoveListener(SetLooping);
        UnsubscribeFromCurrent();
    }

    public void Next()
    {
        Select(currentIndex + 1);
    }

    public void Back()
    {
        Select(currentIndex - 1);
    }

    public void Replay()
    {
        if (CurrentSlash == null)
            return;
        CurrentSlash.Play();
        UpdateLabel();
    }

    public void Select(int index)
    {
        if (!HasEffects)
            return;

        UnsubscribeFromCurrent();
        CurrentSlash?.Stop();
        currentIndex = (index % slashEffects.Length + slashEffects.Length) % slashEffects.Length;
        SubscribeToCurrent();
        UpdateLabel();
        Replay();
    }

    /// <summary>Can be connected directly to Toggle.onValueChanged.</summary>
    public void SetLooping(bool enabled)
    {
        looping = enabled;
        if (loopingToggle != null && loopingToggle.isOn != enabled)
            loopingToggle.SetIsOnWithoutNotify(enabled);

        if (looping && Application.isPlaying && CurrentSlash != null && !CurrentSlash.IsPlaying)
            Replay();
    }

    /// <summary>Can be connected to a normal Button.onClick.</summary>
    public void ToggleLooping()
    {
        SetLooping(!looping);
    }

    private void SubscribeToCurrent()
    {
        if (CurrentSlash != null)
            CurrentSlash.Completed += HandleSlashCompleted;
    }

    private void UnsubscribeFromCurrent()
    {
        if (CurrentSlash != null)
            CurrentSlash.Completed -= HandleSlashCompleted;
    }

    private void HandleSlashCompleted(UIVFXSlash completedSlash)
    {
        if (looping && isActiveAndEnabled && completedSlash == CurrentSlash)
            completedSlash.Play();
    }

    private void UpdateLabel()
    {
        if (activeSlashName != null)
            activeSlashName.text = CurrentSlash != null ? CurrentSlash.DisplayName : "No Slash VFX";
    }

    private void OnValidate()
    {
        if (HasEffects)
        {
            startingIndex = Mathf.Clamp(startingIndex, 0, slashEffects.Length - 1);
            currentIndex = Mathf.Clamp(currentIndex, 0, slashEffects.Length - 1);
        }
        UpdateLabel();
    }
}
