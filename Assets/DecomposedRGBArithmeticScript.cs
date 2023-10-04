using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class DecomposedRGBArithmeticScript : MonoBehaviour
{

    public KMAudio mAudio;
    public KMBombModule modSelf;

    public KMSelectable checkBtn, resetBtn;
    public KMSelectable[] chnToggles, gridCells;
    public Renderer[] gridL, gridM, gridR;
    public Renderer[] indLeds, progLeds;
    public Material[] ledcols;
    public TextMesh[] chnTxts;
    public TextMesh operText;

    private bool moduleSolved, haltAnim = true, interactable;
    float time = 0f;
    int step = 0, maxStep = 3;

    int[] expectedGrid, inputtedGrid, leftGrid, rightGrid, chnValEach;
    readonly int[][] templateGridOperation = new int[][] {
            new[] { 0, 1, 2, 3 },
            new[] { 4, 5, 6, -1 },
            new[] { 7, 8, -1, -1 },
            new[] { 9, -1, -1, -1 },
        };
    readonly Dictionary<char, int[,]> operatorTableTernary = new Dictionary<char, int[,]>
        {
            {'+', new int[,] {
                { 0, 0, 1 },
                { 0, 1, 2 },
                { 1, 2, 2 },
            }},
            {'\u2613', new int[,] {
                { 2, 1, 0 },
                { 1, 1, 1 },
                { 0, 1, 2 },
            }},
            {'\u25cb', new int[,] {
                { 0, 2, 1 },
                { 2, 1, 0 },
                { 1, 0, 2 },
            }},
            {'m', new int[,] {
                { 0, 0, 0 },
                { 0, 1, 1 },
                { 0, 1, 2 },
            }},
            {'M', new int[,] {
                { 0, 1, 2 },
                { 1, 1, 2 },
                { 2, 2, 2 },
            }},
            {'\u00d8', new int[,] {
                { 1, 0, 2 },
                { 0, 1, 0 },
                { 2, 0, 1 },
            }},
        };
    readonly int[] dividens = new[] { 1, 3, 9 };
    const string usedAlphabetDecomp = "-ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string possibleOpers = "+\u2613mM\u25cb\u00d8", valueRepresent = "-0+";
    string usedOpers;
    bool[] invertChns;
    List<List<int>> usedTransforms;

    static int modIDCnt;
    int moduleID;
    void QuickLog(string toLog, params object[] args)
    {
        Debug.LogFormat("[Decomposed RGB Arithmetic #{0}] {1}", moduleID, string.Format(toLog, args));
    }
    void QuickLogDebug(string toLog, params object[] args)
    {
        Debug.LogFormat("<Decomposed RGB Arithmetic #{0}> {1}", moduleID, string.Format(toLog, args));
    }
    // Use this for initialization
    void Start()
    {
        moduleID = ++modIDCnt;
        foreach (var rend in gridM)
            rend.material = ledcols[0];
        foreach (var rend in gridL)
            rend.material = ledcols[0];
        foreach (var rend in gridR)
            rend.material = ledcols[0];
        //StartCoroutine(SolveAnim());
        GenerateStage();
        for (var x = 0; x < chnToggles.Length; x++)
        {
            var y = x;
            chnToggles[x].OnInteract += delegate
            {
                if (interactable)
                {
                    chnToggles[y].AddInteractionPunch(0.5f);
                    chnValEach[2 - y] = (chnValEach[2 - y] + 1) % 3;
                    mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, chnToggles[y].transform);
                    UpdateInterface();
                }
                return false;
            };
        }
        for (var x = 0; x < gridCells.Length; x++)
        {
            var y = x;
            gridCells[x].OnInteract += delegate
            {
                if (interactable)
                {
                    gridCells[y].AddInteractionPunch(0.25f);
                    var foundIdx = templateGridOperation[y / 4][y % 4];
                    if (foundIdx != -1)
                    {
                        inputtedGrid[foundIdx] = Enumerable.Range(0, 3).Sum(a => dividens[a] * chnValEach[a]);
                        HandleStep();
                    }
                    mAudio.PlaySoundAtTransform("BlipSelect", gridCells[y].transform);
                }
                return false;
            };
        }
        resetBtn.OnInteract += delegate
        {
            if (interactable)
            {
                resetBtn.AddInteractionPunch();
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetBtn.transform);
                for (var x = 0; x < inputtedGrid.Length; x++)
                {
                    inputtedGrid[x] = 0;
                }
                HandleStep();
                
            }
            else if (moduleSolved && enabled)
            {
                enabled = false;
                for (var y = 0; y < gridM.Length; y++)
                {
                    gridM[y].material = ledcols[0];
                }
            }
            return false;
        };
        checkBtn.OnInteract += delegate
        {
            if (interactable)
            {
                checkBtn.AddInteractionPunch();
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, checkBtn.transform);
                interactable = false;
                StartCoroutine(HandleSubmission());

            }
            return false;
        };
    }
    void LogGrid(IEnumerable<int> gridToLog)
    {
        foreach (int[] v in templateGridOperation)
        {
            QuickLog(v.Select(a =>
            a < 0 || a >= gridToLog.Count() ? "XXX"
            : dividens.Select(b => "-0+"[gridToLog.ElementAt(a) / b % 3]).Reverse().Join("")).Join(" "));
        }
    }
    IEnumerator HandleSubmission()
    {
        var isAllCorrect = inputtedGrid.SequenceEqual(expectedGrid);
        haltAnim = true;
        QuickLog("The check button was pressed with the middle grid displaying the following:");
        LogGrid(inputtedGrid);

        if (isAllCorrect)
        {
            for (var x = 0; x < progLeds.Length; x++)
                progLeds[x].material = ledcols[6];
            mAudio.PlaySoundAtTransform("InputCorrect", transform);
            for (var x = 0; x < gridM.Length; x++)
                gridM[x].material = ledcols[6];
            yield return new WaitForSeconds(2.5f);
            for (var x = 0; x < indLeds.Length; x++)
                indLeds[x].material = ledcols[0];
            for (var x = 0; x < gridL.Length; x++)
                gridL[x].material = ledcols[0];
            for (var x = 0; x < gridR.Length; x++)
                gridR[x].material = ledcols[0];
            for (var x = 0; x < gridM.Length; x++)
                gridM[x].material = ledcols[0];
            for (var x = 0; x < chnTxts.Length; x++)
                chnTxts[x].text = "";
            operText.text = "";

            mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            moduleSolved = true;
            modSelf.HandlePass();
            yield return SolveAnim();
        }
        else
        {
            for (var x = 0; x < gridM.Length; x++)
            {
                var obtainedIdx = templateGridOperation[x / 4][x % 4];
                gridM[x].material = obtainedIdx != -1 && inputtedGrid[obtainedIdx] != expectedGrid[obtainedIdx] ? ledcols[18] : ledcols[6];
                if (obtainedIdx != -1 && inputtedGrid[obtainedIdx] != expectedGrid[obtainedIdx])
                    inputtedGrid[obtainedIdx] = 0;
            }
            modSelf.HandleStrike();
            yield return new WaitForSeconds(2.5f);
            HandleStep();
            haltAnim = false;
            interactable = true;
        }

        yield break;
    }

    void GenerateStage()
    {
        inputtedGrid = new int[10];
        chnValEach = new int[chnToggles.Length];

        var possibleDistributions = new[] { new[] { 4, 6 }, new[] { 5, 5 } };

        var selectedWords = possibleDistributions.PickRandom().Select(a => CipherMachineData._allWords[a].PickRandom()).ToArray().Shuffle();
        expectedGrid = selectedWords.Join("").Select(a => usedAlphabetDecomp.IndexOf(a)).ToArray();

        var numPickedOpers = Random.Range(1, 3);
        usedOpers = "";
        for (var x = 0; x < numPickedOpers; x++)
            usedOpers += (usedOpers.Any() ? possibleOpers.Except(usedOpers) : possibleOpers).PickRandom();

        var leftGridPretransforms = new int[10];
        var rightGridPretransforms = new int[10];

        for (var x = 0; x < 10; x++)
        {
            var chnValues = dividens.Select(a => expectedGrid[x] / a % 3).ToArray();

            for (var chn = 0; chn < 3; chn++)
            {
                var allowedSelections = Enumerable.Range(0, 9).Select(a => new[] { a % 3, a / 3 }) // Create an array of 9 distinct options...
                    .Where(b => operatorTableTernary[usedOpers[x % usedOpers.Length]][b.First(), b.Last()] == chnValues[chn]); // And filter this based on the operators used.
                var pickedSelection = allowedSelections.PickRandom();
                leftGridPretransforms[x] += pickedSelection.First() * dividens[chn];
                rightGridPretransforms[x] += pickedSelection.Last() * dividens[chn];
            }
        }
        // Perform the transforms here.
        var leftGridTransformedValues = leftGridPretransforms.Select(a => dividens.Select(b => a / b % 3).ToArray()).ToArray();
        var rightGridTransformedValues = rightGridPretransforms.Select(a => dividens.Select(b => a / b % 3).ToArray()).ToArray();
        var transformTL = new[] { 3, 2, 1, 0, 6, 5, 4, 8, 7, 9 };
        var transformTR = new[] { 9, 8, 6, 3, 7, 5, 2, 4, 1, 0 };
        var transformBL = new[] { 0, 4, 7, 9, 1, 5, 8, 2, 6, 3 };
        invertChns = new bool[6];
        usedTransforms = new List<List<int>>();
        // Perform Left Channel Transforms.
        for (var chn = 0; chn < 3; chn++)
        {
            var newTransforms = new List<int>();
            var invertCurChannel = Random.value < 0.5f;
            invertChns[chn] = invertCurChannel;
            if (invertCurChannel)
                for (var x = 0; x < leftGridTransformedValues.Length; x++)
                    leftGridTransformedValues[x][chn] = 2 - leftGridTransformedValues[x][chn];

            var pickedTransforms = Random.Range(0, 4);
            for (var l = 0; l < pickedTransforms; l++)
            {
                var selectedTransformIdx = newTransforms.Any() ? Enumerable.Range(0, 3).Where(a => a != newTransforms.First()).PickRandom() : Random.Range(0, 3);
                newTransforms.Insert(0, selectedTransformIdx);
                var previousChannelsValues = Enumerable.Range(0, 10).Select(a => leftGridTransformedValues[a][chn]).ToArray();
                switch (selectedTransformIdx)
                {
                    case 0:
                        for (var x = 0; x < leftGridTransformedValues.Length; x++)
                            leftGridTransformedValues[x][chn] = previousChannelsValues[transformTL[x]];
                        break;
                    case 1:
                        for (var x = 0; x < leftGridTransformedValues.Length; x++)
                            leftGridTransformedValues[x][chn] = previousChannelsValues[transformTR[x]];
                        break;
                    case 2:
                        for (var x = 0; x < leftGridTransformedValues.Length; x++)
                            leftGridTransformedValues[x][chn] = previousChannelsValues[transformBL[x]];
                        break;
                }
            }
            usedTransforms.Add(newTransforms);
        }
        // Perform Right Channel Transforms.
        for (var chn = 0; chn < 3; chn++)
        {
            var newTransforms = new List<int>();
            var invertCurChannel = Random.value < 0.5f;
            invertChns[chn + 3] = invertCurChannel;
            if (invertCurChannel)
                for (var x = 0; x < rightGridTransformedValues.Length; x++)
                    rightGridTransformedValues[x][chn] = 2 - rightGridTransformedValues[x][chn];

            var pickedTransforms = Random.Range(0, 4);
            for (var l = 0; l < pickedTransforms; l++)
            {
                var selectedTransformIdx = newTransforms.Any() ? Enumerable.Range(0, 3).Where(a => a != newTransforms.First()).PickRandom() : Random.Range(0, 3);
                newTransforms.Insert(0, selectedTransformIdx);
                var previousChannelsValues = Enumerable.Range(0, 10).Select(a => rightGridTransformedValues[a][chn]).ToArray();
                switch (selectedTransformIdx)
                {
                    case 0:
                        for (var x = 0; x < rightGridTransformedValues.Length; x++)
                            rightGridTransformedValues[x][chn] = previousChannelsValues[transformTL[x]];
                        break;
                    case 1:
                        for (var x = 0; x < rightGridTransformedValues.Length; x++)
                            rightGridTransformedValues[x][chn] = previousChannelsValues[transformTR[x]];
                        break;
                    case 2:
                        for (var x = 0; x < rightGridTransformedValues.Length; x++)
                            rightGridTransformedValues[x][chn] = previousChannelsValues[transformBL[x]];
                        break;
                }
            }
            usedTransforms.Add(newTransforms);
        }
        leftGrid = new int[10];
        for (var x = 0; x < leftGridTransformedValues.Length; x++)
        {
            leftGrid[x] = Enumerable.Range(0, 3).Sum(a => leftGridTransformedValues[x][a] * dividens[a]);
        }
        rightGrid = new int[10];
        for (var x = 0; x < rightGridTransformedValues.Length; x++)
        {
            rightGrid[x] = Enumerable.Range(0, 3).Sum(a => rightGridTransformedValues[x][a] * dividens[a]);
        }

        QuickLog("The left grid displayed is:");
        LogGrid(leftGrid);

        QuickLog("The right grid displayed is:");
        LogGrid(rightGrid);

        var transformNames = new[] { "TL", "TR", "BL", };

        QuickLog("The left grid's transformations are: {0}",
            Enumerable.Range(0, 3).Select(a => string.Format("[{0}: {1}{2}]",
            "RGB"[a], usedTransforms[2 - a].Any() ? usedTransforms[2 - a].Select(b => transformNames[b]).Join(", ") : "<none>",
            invertChns[2 - a] ? " + BR" : "")).Join(";"));
        QuickLog("The right grid's transformations are: {0}",
            Enumerable.Range(0, 3).Select(a => string.Format("[{0}: {1}{2}]",
            "RGB"[a], usedTransforms[5 - a].Any() ? usedTransforms[5 - a].Select(b => transformNames[b]).Join(", ") : "<none>",
            invertChns[5 - a] ? " + BR" : "")).Join(";"));

        QuickLog("The left grid after transformations should be:");
        LogGrid(leftGridPretransforms);

        QuickLog("The right grid after transformations should be:");
        LogGrid(rightGridPretransforms);

        QuickLog("The used operators are: {0}", usedOpers.Join(", "));

        QuickLog("The center grid should display:");
        LogGrid(expectedGrid);

        QuickLog("Decrypting the center grid gives these two words in reading order: {0}",selectedWords.Join());

        maxStep = 60;

        indLeds[3].material = ledcols[Enumerable.Range(0, 3).Sum(a => (invertChns[a] ? 2 : 0) * dividens[a])];
        indLeds[7].material = ledcols[Enumerable.Range(0, 3).Sum(a => (invertChns[a + 3] ? 2 : 0) * dividens[a])];

        haltAnim = false;
        interactable = true;
        HandleStep();
        UpdateInterface();
    }
    void UpdateInterface()
    {
        for (var x = 0; x < chnTxts.Length; x++)
            chnTxts[x].text = valueRepresent[chnValEach[x]].ToString();
    }

    void HandleStep()
    {
        if (usedOpers.Length > 1)
        {
            var curChr = usedOpers.ElementAtOrDefault(step % (usedOpers.Length + 1));
            operText.text = curChr.ToString();
        }
        else
            operText.text = usedOpers;
        var templateGridDisplay = new int[][] {
            new[] { 0, 1, 2, 3 },
            new[] { 4, 5, 6, -1 },
            new[] { 7, 8, -1, -2 },
            new[] { 9, -1, -3, -4 },
        };
        for (var x = 0; x < templateGridDisplay.Length; x++)
        {
            for (var y = 0; y < templateGridDisplay[x].Length; y++)
            {
                var selectedIdx = templateGridDisplay[x][y];
                gridL[4 * x + y].material = ledcols[selectedIdx >= 0 ? leftGrid[selectedIdx] : selectedIdx == -1 ? 0 : leftGrid[step % leftGrid.Length] / dividens[-1 * selectedIdx - 2] % 3 * dividens[-1 * selectedIdx - 2]];
            }
        }
        for (var x = 0; x < templateGridDisplay.Length; x++)
        {
            for (var y = 0; y < templateGridDisplay[x].Length; y++)
            {
                var selectedIdx = templateGridDisplay[x][y];
                gridR[4 * x + y].material = ledcols[selectedIdx >= 0 ? rightGrid[selectedIdx] : selectedIdx == -1 ? 0 : rightGrid[step % rightGrid.Length] / dividens[-1 * selectedIdx - 2] % 3 * dividens[-1 * selectedIdx - 2]];
            }
        }
        for (var x = 0; x < templateGridDisplay.Length; x++)
        {
            for (var y = 0; y < templateGridDisplay[x].Length; y++)
            {
                var selectedIdx = templateGridDisplay[x][y];
                gridM[4 * x + y].material = ledcols[selectedIdx >= 0 ? inputtedGrid[selectedIdx] : selectedIdx == -1 ? 0 : inputtedGrid[step % inputtedGrid.Length] / dividens[-1 * selectedIdx - 2] % 3 * dividens[-1 * selectedIdx - 2]];
            }
        }
        var leftGridSteps = Enumerable.Range(0, 3).Select(a => step % (1 + usedTransforms[a].Count)).ToArray();
        var obtainedIdxesLeft = Enumerable.Range(0, 3).Select(b => usedTransforms[b].Count <= leftGridSteps[b] ? -1 : usedTransforms[b][leftGridSteps[b]]);
        for (var x = 0; x < 3; x++)
        {
            indLeds[x].material = ledcols[Enumerable.Range(0, 3).Sum(a => (x == obtainedIdxesLeft.ElementAt(a) ? 2 : 0) * dividens[a])];
        }
        var rightGridSteps = Enumerable.Range(0, 3).Select(a => step % (1 + usedTransforms[a + 3].Count)).ToArray();
        var obtainedIdxesRight = Enumerable.Range(0, 3).Select(b => usedTransforms[b + 3].Count <= rightGridSteps[b] ? -1 : usedTransforms[b + 3][rightGridSteps[b]]);
        for (var x = 0; x < 3; x++)
        {
            indLeds[x + 4].material = ledcols[Enumerable.Range(0, 3).Sum(a => (x == obtainedIdxesRight.ElementAt(a) ? 2 : 0) * dividens[a])];
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!haltAnim)
        {
            time += Time.deltaTime;
            if (time >= 1f)
            {
                time = 0f;
                step = (step + 1) % maxStep;
                //if (step == 0)
                //    QuickLogDebug("loop {0}", maxStep);
                HandleStep();
            }
            for (var x = 0; x < progLeds.Length; x++)
            {
                progLeds[(x + step) % 3].material = Mathf.Round(time * progLeds.Length) >= (x + 1) ? ledcols[6] : ledcols[0];
            }
        }
    }
    private IEnumerator SolveAnim()
    {
        int n = 1;
        while (enabled)
        {
            gridM[0].material = ledcols[n % 27];
            if (n > 1)
            {
                gridM[1].material = ledcols[(n - 1) % 27];
                gridM[4].material = ledcols[(n - 1) % 27];
            }
            if (n > 2)
            {
                gridM[2].material = ledcols[(n - 2) % 27];
                gridM[5].material = ledcols[(n - 2) % 27];
                gridM[8].material = ledcols[(n - 2) % 27];
            }
            if (n > 3)
            {
                gridM[3].material = ledcols[(n - 3) % 27];
                gridM[6].material = ledcols[(n - 3) % 27];
                gridM[9].material = ledcols[(n - 3) % 27];
                gridM[12].material = ledcols[(n - 3) % 27];
            }
            if (n > 4)
            {
                gridM[7].material = ledcols[(n - 4) % 27];
                gridM[10].material = ledcols[(n - 4) % 27];
                gridM[13].material = ledcols[(n - 4) % 27];
            }
            if (n > 5)
            {
                gridM[11].material = ledcols[(n - 5) % 27];
                gridM[14].material = ledcols[(n - 5) % 27];
            }
            if (n > 6)
                gridM[15].material = ledcols[(n - 6) % 27];
            yield return new WaitForSeconds(0.1f);
            n++;
            if (n > 53)
                n -= 26;
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <a-d><1-4> [Selects cell] | !{0} <-0+><-0+><-0+> [Selects colour] | !{0} submit | !{0} reset | Selection commands can be chained, separated with spaces i.e. !{0} +0- a1 b1 --+ b2";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (!interactable)
        {
            yield return "sendtochaterror The module is refusing to accept inputs right now.";
            yield break;
        }

        if (command.ToLowerInvariant() == "submit")
        {
            yield return null;
            checkBtn.OnInteract();
            yield return "solve";
            yield break;
        }
        if (command.ToLowerInvariant() == "reset")
        {
            yield return null;
            resetBtn.OnInteract();
            yield break;
        }
        else
        {
            List<int[]> c = new List<int[]> { };
            List<int> g = new List<int> { };
            List<bool> b = new List<bool> { };
            string[] commands = command.ToLowerInvariant().Split(' ');
            for (int i = 0; i < commands.Length; i++)
            {
                var m = Regex.Match(commands[i], @"^\s*([-0+]{3})\s*$");
                if (m.Success)
                {
                    b.Add(true);
                    c.Add(new int[3] { "-0+".IndexOf(commands[i][0].ToString()), "-0+".IndexOf(commands[i][1].ToString()), "-0+".IndexOf(commands[i][2].ToString()) });
                }
                else if (commands[i].Length == 2 && "abcd".Contains(commands[i][0]) && "1234".Contains(commands[i][1]))
                {
                    b.Add(false);
                    g.Add("abcd".IndexOf(commands[i][0]) + ("1234".IndexOf(commands[i][1]) * 4));
                }
                else if (commands[i].Replace(" ", "").Length == 0)
                    continue;
                else
                {
                    yield return "sendtochaterror Invalid command: " + commands[i];
                    yield break;
                }
            }
            int[] indices = new int[2];
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i])
                {
                    for (int j = 0; j < 3; j++)
                        while (chnValEach[2 - j] != c[indices[0]][j])
                        {
                            yield return null;
                            chnToggles[j].OnInteract();
                            yield return "trycancel I've canceled the command due to a request to cancel!";
                        }
                    indices[0]++;
                }
                else
                {
                    yield return null;
                    gridCells[g[indices[1]]].OnInteract();
                    indices[1]++;
                }
            }
        }
    }

    private int[] IntToBalTer(int num)
    {
        return new int[3] { num % 3, num / 3 % 3, num / 9 };
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (!interactable)
                yield return true;
            while (!inputtedGrid.SequenceEqual(expectedGrid))
            {
                var relevantIdxes = new int[] { 0, 1, 2, 3, 4, 5, 6, 8, 9, 12 };
                for (var x = 0; x < expectedGrid.Length; x++)
                {
                    var curTernaryRepre = IntToBalTer(expectedGrid[x]);
                    for (var y = 0; y < curTernaryRepre.Length; y++)
                    {
                        while (chnValEach[y] != curTernaryRepre[y])
                        {
                            yield return null;
                            chnToggles[2 - y].OnInteract();
                        }
                    }
                    gridCells[relevantIdxes[x]].OnInteract();
                    yield return null;
                }
            }
            checkBtn.OnInteract();

            while (!interactable)
                yield return true;
            yield break;
        }
    }
}
