using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject instructionsPanel;
    public GameObject scanningPanel;
    public GameObject placingPanel;
    public GameObject hudPanel;
    public GameObject endPanel;

    public TMP_Text instructionsText;
    public Button instructionsOkButton;
    [TextArea(8, 20)]
    public string instructions =
        "GOAL: grow 3 flowers to full bloom.\n\n" +
        "1. Move your phone slowly over a table or the floor until you see the grid.\n" +
        "2. Tap to place the garden. Drag to move it, pinch to resize, twist with two fingers to rotate. Then press Start.\n" +
        "3. Tap a plant to water it. The bar above each plant shows its water. Don't overwater: tapping a full plant hurts it.\n" +
        "4. Tap the bugs to squash them before they eat your plants.\n" +
        "5. If a plant dies, tap it to replant. Lose 3 plants and the game is over.";

    public TMP_Text scanningText;
    [TextArea(2, 5)]
    public string unsupportedMessage =
        "This device does not support ARCore. Garden Keeper needs an ARCore-compatible Android phone.";

    public Button confirmPlacementButton;
    public Button rescanButton;

    public TMP_Text bloomsText;
    public TMP_Text deathsText;
    public TMP_Text scoreText;
    public TMP_Text timeText;
    public Button resetButton;

    public TMP_Text endTitleText;
    public TMP_Text endDetailsText;
    public Button retryButton;

    GameManager gm;
    string scanningHint;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            enabled = false;
            return;
        }

        if (scanningText) scanningHint = scanningText.text;
        if (instructionsText) instructionsText.text = instructions;

        Bind(instructionsOkButton, gm.CloseInstructions);
        Bind(confirmPlacementButton, gm.ConfirmPlacement);
        Bind(rescanButton, gm.StartScanning);
        Bind(resetButton, gm.ResetGame);
        Bind(retryButton, gm.StartScanning);

        gm.StateChanged += OnStateChanged;
        OnStateChanged(gm.State);
    }

    void OnDestroy()
    {
        if (gm != null) gm.StateChanged -= OnStateChanged;
    }

    void Update()
    {
        if (gm == null || gm.State != GameState.Playing) return;
        SetText(bloomsText, $"Blooms {gm.Blooms}/{gm.bloomsToWin}");
        SetText(deathsText, $"Lost {gm.Deaths}/{gm.deathsToLose}");
        SetText(scoreText, $"Score {gm.Score}");
        SetText(timeText, FormatTime(gm.ElapsedTime));
    }

    void OnStateChanged(GameState s)
    {
        bool unsupported = s == GameState.Unsupported;

        Show(instructionsPanel, s == GameState.Instructions);
        Show(scanningPanel, s == GameState.Scanning || unsupported);
        Show(placingPanel, s == GameState.Placing);
        Show(hudPanel, s == GameState.Playing);
        Show(endPanel, s == GameState.Won || s == GameState.Lost);

        if (scanningText) scanningText.text = unsupported ? unsupportedMessage : scanningHint;

        if (s == GameState.Won || s == GameState.Lost) FillEndScreen(s == GameState.Won);
    }

    void FillEndScreen(bool won)
    {
        SetText(endTitleText, won ? "Garden saved!" : "The garden withered");
        SetText(endDetailsText,
            $"Score: {gm.Score}\n" +
            $"Time: {FormatTime(gm.ElapsedTime)}\n" +
            $"Blooms: {gm.Blooms}/{gm.bloomsToWin}\n" +
            $"Plants lost: {gm.Deaths}");
    }

    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    static void Bind(Button button, UnityAction action)
    {
        if (button) button.onClick.AddListener(action);
    }

    static void Show(GameObject panel, bool visible)
    {
        if (panel && panel.activeSelf != visible) panel.SetActive(visible);
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text) text.text = value;
    }
}
