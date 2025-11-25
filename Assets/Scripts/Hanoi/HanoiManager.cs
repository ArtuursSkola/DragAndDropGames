using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HanoiManager : MonoBehaviour
{
    public PegUI[] pegs;                   // 3 pegs
    public DiskUI[] diskPrefabs;           // Array of disk prefabs (Disk0...Disk5)
    public int diskCount = 3;              // 3–6
    public Text movesText;
    [Header("Winning UI")]
    public GameObject winningWindow; // assign your inactive panel here
    public Text winningTimeText;
    public Text winningMovesText;

    private PegUI selectedFrom = null;
    private DiskUI selectedDisk = null;
    private int moves = 0;
    private float elapsedTime = 0f;
    private bool isSolved = false;

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
    }

    private void Update()
    {
        if (!isSolved)
        {
            elapsedTime += Time.unscaledDeltaTime;
        }
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

            moves++;
            UpdateMovesText();

            if (pegs[2].disks.Count == diskCount)
            {
                Debug.Log("Solved in " + moves + " moves!");
                OnSolved();
            }
        }

        selectedDisk = null;
        selectedFrom = null;
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
