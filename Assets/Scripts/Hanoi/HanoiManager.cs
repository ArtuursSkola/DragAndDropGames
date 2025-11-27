using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HanoiManager : MonoBehaviour
{
    public PegUI[] pegs;                   // 3 pegs
    public DiskUI[] diskPrefabs;           // Array of disk prefabs (Disk0...Disk5)
    public int diskCount = 3;              // 3–6
    public Text movesText;
    public Text timerText; // assign the on-screen Timer Text (named 'Timer')

    [Header("Freeze UI")]
    public GameObject freezeWindow; // panel to show when freeze is active (assign in Inspector or name it 'FreezeWindow')
    public Text freezeCountdownText; // text inside freeze window showing remaining seconds
    public GameObject freezeStartButton; // optional: assign the FreezeButton GameObject (named 'FreezeButton')

    [Header("Winning UI")]
    public GameObject winningWindow; // assign your inactive panel here
    public Text winningTimeText;
    public Text winningMovesText;

    private PegUI selectedFrom = null;
    private DiskUI selectedDisk = null;
    private int moves = 0;
    private float elapsedTime = 0f;
    private bool isSolved = false;
    private bool isFrozen = false; // when true: timer paused and moves not counted, but player can interact
    private bool isInteractionLocked = false; // general lock for interactions (used when showing notice)
    private bool waitingForStart = false; // waiting for player to press OK on freeze notice
    private Transform freezeCountdownOriginalParent = null;

    void Start()
    {
        if (pegs == null || pegs.Length < 3)
        {
            Debug.LogError("HanoiManager requires 3 PegUI references assigned.");
            return;
        }

        for (int i = 0; i < pegs.Length; i++)
        {
            pegs[i].manager = this;
            pegs[i].pegIndex = i;
        }

        InitializeDisks();
        UpdateMovesText();

        // ensure winning window is hidden at start
        if (winningWindow != null)
            winningWindow.SetActive(false);

        // ensure freeze UI initial state
        if (freezeWindow != null)
        {
            freezeWindow.SetActive(false);
            if (freezeCountdownText != null)
                freezeCountdownText.gameObject.SetActive(false);
            if (freezeStartButton != null)
                freezeStartButton.SetActive(false);
        }

        // try to auto-find UI Texts if not assigned in inspector
        if (timerText == null)
        {
            var tObj = GameObject.Find("Timer");
            if (tObj != null)
                timerText = tObj.GetComponent<Text>();
        }
        if (movesText == null)
        {
            var mObj = GameObject.Find("Moves");
            if (mObj != null)
                movesText = mObj.GetComponent<Text>();
        }

        // subscribe to rewarded ad event to receive freeze reward
        try { RewardedAds.OnRewardGranted += HandleRewardFreeze; } catch { }

        // try to auto-find start button if not assigned and wire it
        if (freezeStartButton == null)
        {
            var fb = GameObject.Find("FreezeButton");
            if (fb != null)
                freezeStartButton = fb;
        }

        if (freezeStartButton != null)
        {
            var b = freezeStartButton.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.AddListener(ConfirmStartFreeze);
                Debug.Log("HanoiManager: auto-wired FreezeButton.onClick -> ConfirmStartFreeze");
            }
        }
    }

    private void Update()
    {
        // only advance elapsed time when not solved and not frozen
        if (!isSolved && !isFrozen)
        {
            elapsedTime += Time.unscaledDeltaTime;
        }

        // update on-screen timer and moves during play
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
        if (movesText != null)
            movesText.text = "Moves: " + moves;
    }

    private void OnDisable()
    {
        try { RewardedAds.OnRewardGranted -= HandleRewardFreeze; } catch { }

        // remove any listener we may have auto-added to the FreezeButton
        if (freezeStartButton != null)
        {
            var b = freezeStartButton.GetComponent<Button>();
            if (b != null)
                b.onClick.RemoveListener(ConfirmStartFreeze);
        }
    }

    private void HandleRewardFreeze()
    {
        // show the freeze notice and wait for player to press FreezeButton to start the 20s freeze
        ShowFreezeNotice();
    }

    private void ShowFreezeNotice()
    {
        if (freezeWindow == null)
            return;

        // show the window, show start button, hide countdown text
        freezeWindow.SetActive(true);
        if (freezeStartButton != null)
            freezeStartButton.SetActive(true);
        if (freezeCountdownText != null)
            freezeCountdownText.gameObject.SetActive(false);

        waitingForStart = true;

        // lock interactions with the puzzle while the notice is up so user can't interact unexpectedly
        isInteractionLocked = true;
    }

    // This should be wired to the FreezeButton OnClick in the Inspector (or found by name)
    public void ConfirmStartFreeze()
    {
        if (!waitingForStart)
            return;

        waitingForStart = false;

        // hide the start button (so it can't be clicked again)
        if (freezeStartButton != null)
            freezeStartButton.SetActive(false);

        // show countdown text in the panel
        if (freezeCountdownText != null)
            freezeCountdownText.gameObject.SetActive(true);

        // hide any winning UI that might be covering the freeze UI
        if (winningWindow != null && winningWindow.activeSelf)
        {
            winningWindow.SetActive(false);
            Debug.Log("HanoiManager: hiding winningWindow because freeze started");
        }

        // Enter freeze mode: stop timer and stop counting moves, but allow the player to interact
        isFrozen = true;
        isInteractionLocked = false; // allow peg/disk clicks during freeze

        // If the countdown Text is a child of the freeze window, detach it so it remains visible
        // after we deactivate the freezeWindow. Reparent back at the end of the countdown.
        if (freezeCountdownText != null && freezeWindow != null)
        {
            if (freezeCountdownText.transform.IsChildOf(freezeWindow.transform))
            {
                freezeCountdownOriginalParent = freezeCountdownText.transform.parent;
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    freezeCountdownText.transform.SetParent(canvas.transform, false);
                }
            }
        }

        // hide the freeze notice panel (we still keep the countdown text visible)
        if (freezeWindow != null)
            freezeWindow.SetActive(false);

        Debug.Log("HanoiManager: ConfirmStartFreeze called, starting freeze countdown");

        // begin the actual freeze countdown (unfreeze will re-enable interaction)
        StartCoroutine(FreezeCountdownCoroutine(20));
    }

    private System.Collections.IEnumerator FreezeCountdownCoroutine(int seconds)
    {
        // ensure the countdown text is visible; keep the freezeWindow itself inactive
        if (freezeCountdownText != null)
            freezeCountdownText.gameObject.SetActive(true);
        if (freezeWindow != null)
            freezeWindow.SetActive(false);

        int remaining = seconds;
        while (remaining > 0)
        {
            if (freezeCountdownText != null)
                freezeCountdownText.text = remaining.ToString() + "s";
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
        }

        if (freezeCountdownText != null)
            freezeCountdownText.text = "0s";

        // end freeze: re-enable timer and move counting and hide the freeze UI
        isFrozen = false;
        isInteractionLocked = false;

        // hide countdown text and restore its original parent if we moved it
        if (freezeCountdownText != null)
        {
            freezeCountdownText.gameObject.SetActive(false);
            if (freezeCountdownOriginalParent != null)
            {
                freezeCountdownText.transform.SetParent(freezeCountdownOriginalParent, false);
                freezeCountdownOriginalParent = null;
            }
        }

        // ensure freeze window is inactive
        if (freezeWindow != null)
            freezeWindow.SetActive(false);
    }

    void InitializeDisks()
    {
        foreach (var p in pegs)
            p.disks.Clear();

        // size = 1 (smallest) … diskCount (largest)
        for (int size = diskCount; size >= 1; size--)
        {
            int prefabIndex = size - 1;

            if (prefabIndex >= diskPrefabs.Length)
            {
                Debug.LogError("Not enough disk prefabs! Add more prefabs to diskPrefabs array.");
                return;
            }

            // spawn correct prefab for this size
            DiskUI d = Instantiate(diskPrefabs[prefabIndex], pegs[0].dropZone);
            d.size = size;
            d.manager = this;

            pegs[0].Push(d);
        }
    }

    public void OnDiskClicked(DiskUI disk)
    {
        if (isInteractionLocked || isSolved) return;
        for (int i = 0; i < pegs.Length; i++)
        {
            if (pegs[i].Peek() == disk)
            {
                selectedFrom = pegs[i];
                selectedDisk = disk;
                return;
            }
        }
    }

    public void OnPegClicked(PegUI peg)
    {
        if (isInteractionLocked || isSolved) return;

        if (selectedDisk == null)
        {
            var top = peg.Peek();
            if (top != null)
            {
                selectedFrom = peg;
                selectedDisk = top;
            }
            return;
        }

        if (peg == selectedFrom)
        {
            selectedDisk = null;
            selectedFrom = null;
            return;
        }

        if (peg.CanPlace(selectedDisk))
        {
            var d = selectedFrom.Pop();
            peg.Push(d);

            // if we are frozen, allow the player to move pieces but do not count moves
            if (!isFrozen)
            {
                moves++;
                UpdateMovesText();
            }

            if (pegs[2].disks.Count == diskCount)
            {
                Debug.Log("Solved in " + moves + " moves!");
                OnSolved();
            }
        }

        selectedDisk = null;
        selectedFrom = null;
    }

    // Called by UI drag/drop when a disk has been moved from one peg to another.
    // Ensures moves are counted consistently and triggers win check.
    public void RegisterMove(PegUI sourcePeg, PegUI targetPeg, DiskUI disk)
    {
        if (isInteractionLocked || isSolved) return;

        // if we are frozen, allow moves but do not count them
        if (!isFrozen)
        {
            moves++;
            UpdateMovesText();
        }

        // check win condition: all disks on peg index 2
        if (pegs != null && pegs.Length > 2 && pegs[2].disks.Count == diskCount)
        {
            Debug.Log("Solved in " + moves + " moves! (via drag)");
            OnSolved();
        }
    }

    private void UpdateMovesText()
    {
        if (movesText != null)
            movesText.text = "Moves: " + moves;
    }

    public void ResetPuzzle()
    {
        moves = 0;
        InitializeDisks();
        UpdateMovesText();

        // reset timer and state
        elapsedTime = 0f;
        isSolved = false;
        if (winningWindow != null)
            winningWindow.SetActive(false);
    }

    private void OnSolved()
    {
        isSolved = true;
        // ensure any freeze UI is hidden so the winning window is visible
        if (freezeWindow != null && freezeWindow.activeSelf)
            freezeWindow.SetActive(false);

        // show winning window and populate info
        if (winningWindow != null)
        {
            if (winningMovesText != null)
                winningMovesText.text = "Moves: " + moves;
            if (winningTimeText != null)
                winningTimeText.text = "Time: " + FormatTime(elapsedTime);
            winningWindow.SetActive(true);
        }
    }

    private string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds % 60);
        int m = Mathf.FloorToInt((seconds / 60f) % 60);
        return string.Format("{0:00}:{1:00}", m, s);
    }
}
