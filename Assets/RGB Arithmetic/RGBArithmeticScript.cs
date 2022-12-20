using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class RGBArithmeticScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule module;
    public KMSelectable[] submit;
    public KMSelectable[] sels;
    public KMSelectable[] cells;
    public Renderer[] gridL;
    public Renderer[] gridM;
    public Renderer[] gridR;
    public Renderer[] indleds;
    public Renderer[] progleds;
    public Material[] ledcols;
    public TextMesh[] displays;
    public AudioClip[] sounds;

    private int[][][] grids = new int[2][][] { new int[3][] { new int[16], new int[16], new int[16] }, new int[3][] { new int[16], new int[16], new int[16] } };
    private int[][][] dispgrids = new int[2][][] { new int[3][] { new int[16], new int[16], new int[16] }, new int[3][] { new int[16], new int[16], new int[16] } };
    private int[][] cellcols = new int[2][] { new int[16], new int[16] };
    private int[][] results = new int[3][] { new int[16], new int[16], new int[16] };
    private int[] ansgrid = new int[16];
    private int[] inputgrid = new int[16];
    private int[] input = new int[3] { -1, -1, -1 };
    private string[][] logstuff = new string[4][] { new string[4], new string[4], new string[4], new string[4] };
    private bool gridinteract;
    private bool[][] inds = new bool[3][] { new bool[8], new bool[8], new bool[8] };
    private int operation;
    private int stage;

    private static int moduleIDCounter = 1;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        gridinteract = true;
        foreach (Renderer s in gridM)
            s.material = ledcols[0];
        moduleID = moduleIDCounter++;
        for (int i = 0; i < 3; i++)
            displays[i].text = "\u2212";
        submit[0].OnInteract += delegate () { if (gridinteract && !moduleSolved) Check(); return false; };
        submit[1].OnInteract += delegate ()
        {
            if (gridinteract && !moduleSolved) Blank();
            else if (moduleSolved)
            {
                StopAllCoroutines();
                foreach (Renderer g in gridM)
                    g.material = ledcols[0];
            }
            return false;
        };
        foreach (KMSelectable sel in sels)
        {
            int c = Array.IndexOf(sels, sel);
            sel.OnInteract += delegate () { if (!moduleSolved) ColSelect(c); return false; };
        }
        foreach (KMSelectable cell in cells)
        {
            int c = Array.IndexOf(cells, cell);
            cell.OnInteract += delegate () { if (gridinteract && !moduleSolved) GridSelect(c); return false; };
        }
        Setup();
    }

    private void Setup()
    {
        if (stage == 1)
        {
            for (int i = 0; i < 8; i++)
                if (Random.Range(0, 2) == 0)
                {
                    indleds[i].material = ledcols[26];
                    for (int j = 0; j < 3; j++)
                    {
                        inds[j][i] = true;
                    }
                }
        }
        else if (stage == 2)
        {
            for (int i = 0; i < 8; i++)
            {
                int c = 0;
                for (int j = 0; j < 3; j++)
                    if (Random.Range(0, 2) == 0)
                    {
                        inds[j][i] = true;
                        c += (int)Mathf.Pow(2, j);
                    }
                    else
                        inds[j][i] = false;
                indleds[i].material = ledcols[new int[8] { 0, 18, 6, 24, 2, 20, 8, 26 }[c]];
            }
        }
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 3; k++)
                {
                    grids[i][k][j] = Random.Range(-1, 2);
                    dispgrids[i][k][j] = grids[i][k][j];
                }
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (inds[i][4 * j])
                {
                    int[] tempgrid = new int[16];
                    for (int k = 0; k < 16; k++)
                        tempgrid[k] = dispgrids[j][i][(4 * (k / 4)) + (3 - (k % 4))];
                    dispgrids[j][i] = tempgrid;
                }
                if (inds[i][(4 * j) + 1])
                {
                    int[] tempgrid = new int[16];
                    for (int k = 0; k < 16; k++)
                        tempgrid[k] = dispgrids[j][i][(4 * (3 - (k / 4))) + (k % 4)];
                    dispgrids[j][i] = tempgrid;
                }
                if (inds[i][(4 * j) + 2])
                {
                    int[] tempgrid = new int[16];
                    for (int k = 0; k < 16; k++)
                        tempgrid[k] = dispgrids[j][i][(4 * (k % 4)) + (k / 4)];
                    dispgrids[j][i] = tempgrid;
                }
                if (inds[i][(4 * j) + 3])
                    for (int k = 0; k < 16; k++)
                        dispgrids[j][i][k] *= -1;
            }
        }
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 2; j++)
                cellcols[j][i] = (9 * dispgrids[j][0][i]) + (3 * dispgrids[j][1][i]) + dispgrids[j][2][i] + 13;
            gridL[i].material = ledcols[cellcols[0][i]];
            gridR[i].material = ledcols[cellcols[1][i]];
        }
        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 16; i++)
                logstuff[i / 4][i % 4] = "-0+"[dispgrids[j][0][i] + 1].ToString() + "-0+"[dispgrids[j][1][i] + 1].ToString() + "-0+"[dispgrids[j][2][i] + 1].ToString();
            Debug.LogFormat("[RGB Arithmetic #{0}] The {2} grid displays:\n[RGB Arithmetic #{0}] {1}", moduleID, string.Join("\n[RGB Arithmetic #" + moduleID + "] ", logstuff.Select(i => string.Join(" ", i)).ToArray()), new string[2] { "left", "right" }[j]);
        }
        if (stage == 1)
        {
            Debug.LogFormat("[RGB Arithmetic #{0}] The transformations on the left grid are: {1}", moduleID, string.Join(", ", inds[0].Where((x, i) => i < 4).Select((x, i) => new string[] { "horizontal flip", "vertical flip", "diagonal flip", "inversion" }[i]).Where((x, i) => inds[0][i]).ToArray()));
            Debug.LogFormat("[RGB Arithmetic #{0}] The transformations on the right grid are: {1}", moduleID, string.Join(", ", inds[0].Where((x, i) => i > 3).Select((x, i) => new string[] { "horizontal flip", "vertical flip", "diagonal flip", "inversion" }[i]).Where((x, i) => inds[0][i + 4]).ToArray()));
        }
        else if (stage == 2)
        {
            for (int j = 0; j < 3; j++)
                Debug.LogFormat("[RGB Arithmetic #{0}] The transformations on the {2} channel of the left grid are: {1}", moduleID, string.Join(", ", inds[j].Where((x, i) => i < 4).Select((x, i) => new string[] { "horizontal flip", "vertical flip", "diagonal flip", "inversion" }[i]).Where((x, i) => inds[j][i]).ToArray()), new string[] { "red", "green", "blue" }[j]);
            for (int j = 0; j < 3; j++)
                Debug.LogFormat("[RGB Arithmetic #{0}] The transformations on the {2} channel of the right grid are: {1}", moduleID, string.Join(", ", inds[j].Where((x, i) => i > 3).Select((x, i) => new string[] { "horizontal flip", "vertical flip", "diagonal flip", "inversion" }[i]).Where((x, i) => inds[j][i + 4]).ToArray()), new string[] { "red", "green", "blue" }[j]);
        }
        if (stage > 0)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 16; i++)
                    logstuff[i / 4][i % 4] = "-0+"[grids[j][0][i] + 1].ToString() + "-0+"[grids[j][1][i] + 1].ToString() + "-0+"[grids[j][2][i] + 1].ToString();
                Debug.LogFormat("[RGB Arithmetic #{0}] The transformed {2} grid is:\n[RGB Arithmetic #{0}] {1}", moduleID, string.Join("\n[RGB Arithmetic #" + moduleID + "] ", logstuff.Select(i => string.Join(" ", i)).ToArray()), new string[2] { "left", "right" }[j]);
            }
        }
        operation = Random.Range(0, 9);
        displays[3].text = new string[9] { "+", "!", "\u2613", "m", "M", "#", "\x25cb", "\u00d8", "\x25c7" }[operation];
        Debug.LogFormat("[RGB Arithmetic #{0}] The operator is: {1}", moduleID, new string[9] { "+", "!", "\u2613", "m", "M", "#", "\x25cb", "\u00d8", "\x25c7" }[operation]);
        switch (operation)
        {
            case 0:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] + grids[1][j][i] == 0) ? 0 : (int)Mathf.Sign(grids[0][j][i] + grids[1][j][i]);
                break;
            case 1:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] + grids[1][j][i] == 0) ? 0 : (int)Mathf.Sign(grids[0][j][i] + grids[1][j][i]) * -1;
                break;
            case 2:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] == 0 || grids[1][j][i] == 0) ? 0 : (int)Mathf.Sign(grids[0][j][i] * grids[1][j][i]);
                break;
            case 3:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = Mathf.Min(grids[0][j][i], grids[1][j][i]);
                break;
            case 4:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = Mathf.Max(grids[0][j][i], grids[1][j][i]);
                break;
            case 5:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] == 0 && grids[1][j][i] == 0) ? 0 : (grids[0][j][i] == 0 || grids[1][j][i] == 0 ? -1 : 1);
                break;
            case 6:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = new int[] { -1, 1, 0, -1, 1 }[grids[0][j][i] + grids[1][j][i] + 2];
                break;
            case 7:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] == grids[1][j][i]) ? 0 : (grids[0][j][i] == 0 || grids[1][j][i] == 0 ? -1 : 1);
                break;
            case 8:
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 3; j++)
                        results[j][i] = (grids[0][j][i] == grids[1][j][i]) ? 0 : ((grids[0][j][i] + grids[1][j][i] == 0) ? 0 : (int)Mathf.Sign(grids[0][j][i] + grids[1][j][i]));
                break;
        }
        for (int i = 0; i < 16; i++)
        {
            ansgrid[i] = (9 * results[0][i]) + (3 * results[1][i]) + results[2][i] + 13;
            logstuff[i / 4][i % 4] = "-0+"[results[0][i] + 1].ToString() + "-0+"[results[1][i] + 1].ToString() + "-0+"[results[2][i] + 1].ToString();
        }
        Debug.LogFormat("[RGB Arithmetic #{0}] The center grid should display:\n[RGB Arithmetic #{0}] {1}", moduleID, string.Join("\n[RGB Arithmetic #" + moduleID + "] ", logstuff.Select(i => string.Join(" ", i)).ToArray()));
    }

    private void Blank()
    {
        submit[1].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        for (int i = 0; i < 16; i++)
        {
            inputgrid[i] = 0;
            gridM[i].material = ledcols[0];
        }
    }

    private void Check()
    {
        submit[0].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        bool all = Enumerable.SequenceEqual(inputgrid, ansgrid);
        if (all)
        {
            progleds[stage].material = ledcols[6];
            Audio.PlaySoundAtTransform(sounds[1].name, transform);
        }
        else
            module.HandleStrike();
        for (int i = -1; i < 16; i++)
            StartCoroutine(Submit(i, i > -1 && (inputgrid[i] == ansgrid[i]), all));
        for (int i = 0; i < 16; i++)
            logstuff[i / 4][i % 4] = "-0+"[inputgrid[i] / 9].ToString() + "-0+"[(inputgrid[i] / 3) % 3].ToString() + "-0+"[inputgrid[i] % 3].ToString();
        Debug.LogFormat("[RGB Arithmetic #{0}] The submitted grid was:\n[RGB Arithmetic #{0}] {1}", moduleID, string.Join("\n[RGB Arithmetic #" + moduleID + "] ", logstuff.Select(i => string.Join(" ", i)).ToArray()));
    }

    private void ColSelect(int c)
    {
        sels[c].AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, sels[c].transform);
        input[c] = input[c] == 1 ? -1 : input[c] + 1;
        displays[c].text = new string[] { "\u2212", "0", "+" }[input[c] + 1];
    }

    private void GridSelect(int c)
    {
        cells[c].AddInteractionPunch(0.25f);
        Audio.PlaySoundAtTransform(sounds[0].name, cells[c].transform);
        inputgrid[c] = 13 + (9 * input[0]) + (3 * input[1]) + input[2];
        gridM[c].material = ledcols[inputgrid[c]];
    }

    private IEnumerator Submit(int c, bool correct, bool allcorrect)
    {
        if (c == -1)
            gridinteract = false;
        else
            gridM[c].material = correct ? ledcols[6] : ledcols[18];
        yield return new WaitForSeconds(2.5f);
        if (c == -1)
        {
            if (allcorrect)
            {
                if (stage < 2)
                {
                    stage++;
                    Setup();
                    gridinteract = true;
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        gridL[i].material = ledcols[0];
                        gridR[i].material = ledcols[0];
                    }
                    foreach (Renderer l in indleds)
                        l.material = ledcols[0];
                    for (int i = 0; i < 4; i++)
                        displays[i].text = string.Empty;
                    moduleSolved = true;
                    module.HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    StartCoroutine(SolveAnim());
                }
            }
        }
        else
        {
            gridM[c].material = correct && !allcorrect ? ledcols[ansgrid[c]] : ledcols[0];
            gridinteract = true;
            if (!correct || allcorrect)
                inputgrid[c] = 0;
        }
    }

    private IEnumerator SolveAnim()
    {
        int n = 1;
        while (moduleSolved)
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
        if (command.ToLowerInvariant() == "submit")
        {
            yield return null;
            submit[0].OnInteract();
            yield break;
        }
        if(command.ToLowerInvariant() == "reset")
        {
            yield return null;
            submit[1].OnInteract();
            yield break;
        }
        else
        {
            List<int[]> c = new List<int[]> { };
            List<int> g = new List<int> { };
            List<bool> b = new List<bool> { };
            string[] commands = command.ToLowerInvariant().Split(' ');
            for(int i = 0; i < commands.Length; i++)
            {
                var m = Regex.Match(commands[i], @"^\s*([-0+]{3})\s*$");
                if (m.Success)
                {
                    b.Add(true);
                    c.Add(new int[3] { "-0+".IndexOf(commands[i][0].ToString()) - 1, "-0+".IndexOf(commands[i][1].ToString()) - 1, "-0+".IndexOf(commands[i][2].ToString()) - 1 });
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
            for(int i = 0; i < b.Count; i++)
            {
                if (b[i])
                {
                    for(int j = 0; j < 3; j++)
                        while(input[j] != c[indices[0]][j])
                        {
                            yield return null;
                            sels[j].OnInteract();
                        }
                    indices[0]++;
                }
                else
                {
                    yield return null;
                    cells[g[indices[1]]].OnInteract();
                    indices[1]++;
                }
            }
        }
    }

    private int[] IntToBalTer(int num)
    {
        return new int[3] { num / 9, (num / 3) % 3, num % 3 };
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int st = stage; st < 3; st++)
        {
            for (int cell = 0; cell < 16; cell++)
            {
                if (ansgrid[cell] == inputgrid[cell])
                    continue;
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        while (input[i] + 1 != IntToBalTer(ansgrid[cell])[i])
                        {
                            sels[i].OnInteract();
                            yield return null;
                        }
                    }
                    cells[cell].OnInteract();
                    yield return null;
                }
            }
            submit[0].OnInteract();
            while (!gridinteract)
                yield return true;
        }
    }
}
