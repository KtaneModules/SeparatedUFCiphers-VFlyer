using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class _3x3YellowCipherScript : MonoBehaviour {
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMBombInfo bombInfo;
	public KMSelectable leftArrow, rightArrow, submit;
	public KMSelectable[] keyboard;
	public TextMesh[] screenTexts;
	public TextMesh subText;
	public MeshRenderer[] displayRenderers;
	static int modIDCnt;
	int moduleID;
	string inputtedWord = "", expectedWord;
	const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM";
	const int maxLengthInput = 6, minLengthInput = 6;
	List<string> displays;
	List<bool> invertDisplays;
	bool moduleSelected, moduleSolved;
	int curPageIdx, maxPages;
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[3x3 Yellow Cipher #{0}] {1}", moduleID, string.Format(value, args));
	}
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		GenerateModule();

		modSelf.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
		modSelf.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };
		submit.OnInteract += delegate {
			if (!moduleSolved)
			{
				submit.AddInteractionPunch();
				HandleSubmit();
			}
			return false;
		};

		leftArrow.OnInteract += delegate {
			if (!moduleSolved)
			{
				leftArrow.AddInteractionPunch();
				mAudio.PlaySoundAtTransform("ArrowPress", leftArrow.transform);
				HandlePageDelta(-1);
			}
			return false;
		};
		rightArrow.OnInteract += delegate {
			if (!moduleSolved)
			{
				rightArrow.AddInteractionPunch();
				mAudio.PlaySoundAtTransform("ArrowPress", rightArrow.transform);
				HandlePageDelta(1);
			}
			return false;
		};
		for (var x = 0; x < keyboard.Length; x++)
		{
			var y = x;
			keyboard[x].OnInteract += delegate {
				if (!moduleSolved)
				{
					keyboard[y].AddInteractionPunch(0.2f);
					mAudio.PlaySoundAtTransform("KeyboardPress", keyboard[y].transform);
					HandleInput(y);
				}
				return false;
			};
		}
	}
    void GenerateModule()
    {
		expectedWord = CipherMachineData._allWords[6].PickRandom();
		QuickLog("Decoded Word: {0}", expectedWord);
		
		var affineReverseReference = new Dictionary<int, int>
		{
			{ 1, 1 },
			{ 3, 9 },
			{ 5, 21 },
			{ 7, 15 },
			{ 9, 3 },
			{ 11, 19 },
			{ 15, 7 },
			{ 17, 23 },
			{ 19, 11 },
			{ 21, 5 },
			{ 23, 17 },
			{ 25, 25 },
		};

		var matrix1 = new int[9];
		var attemptCount = 0;
		var detM1 = 1;
		do
		{
			attemptCount++;
			for (var x = 0; x < 9; x++)
				matrix1[x] = Random.Range(0, 26);

			detM1 = matrix1[0] * matrix1[4] * matrix1[8] + matrix1[1] * matrix1[5] * matrix1[6] + matrix1[2] * matrix1[3] * matrix1[7] -
				matrix1[0] * matrix1[5] * matrix1[7] - matrix1[1] * matrix1[3] * matrix1[8] - matrix1[2] * matrix1[4] * matrix1[6];
			//Debug.Log(det);
			//Debug.Log(matrix.Join());
		}
		while (ObtainGCM(PMod(detM1, 26), 26) != 1);
		QuickLog("Generated 3x3 Matrix in {1} attempt{2}: {0}", matrix1.Join(), attemptCount, attemptCount == 1 ? "" : "s");
		var matrix2 = new int[9];
		attemptCount = 0;
		var detM2 = 1;
		do
		{
			attemptCount++;
			for (var x = 0; x < 9; x++)
				matrix2[x] = Random.Range(0, 26);

			detM2 = matrix2[0] * matrix2[4] * matrix2[8] + matrix2[1] * matrix2[5] * matrix2[6] + matrix2[2] * matrix2[3] * matrix2[7] -
				matrix2[0] * matrix2[5] * matrix2[7] - matrix2[1] * matrix2[3] * matrix2[8] - matrix2[2] * matrix2[4] * matrix2[6];
			//Debug.Log(det);
			//Debug.Log(matrix.Join());
		}
		while (ObtainGCM(PMod(detM2, 26), 26) != 1);
		QuickLog("Generated ANOTHER 3x3 Matrix in {1} attempt{2}: {0}", matrix2.Join(), attemptCount, attemptCount == 1 ? "" : "s");
		var adjMatrix1 = new[] {
			matrix1[4] * matrix1[8] - matrix1[5] * matrix1[7],
			matrix1[2] * matrix1[7] - matrix1[1] * matrix1[8],
			matrix1[1] * matrix1[5] - matrix1[2] * matrix1[4],
			matrix1[5] * matrix1[6] - matrix1[3] * matrix1[8],
			matrix1[0] * matrix1[8] - matrix1[2] * matrix1[6],
			matrix1[2] * matrix1[3] - matrix1[0] * matrix1[5],
			matrix1[3] * matrix1[7] - matrix1[4] * matrix1[6],
			matrix1[1] * matrix1[6] - matrix1[0] * matrix1[7],
			matrix1[0] * matrix1[4] - matrix1[1] * matrix1[3],
		};
		var adjMatrix2 = new[] {
			matrix2[4] * matrix2[8] - matrix2[5] * matrix2[7],
			matrix2[2] * matrix2[7] - matrix2[1] * matrix2[8],
			matrix2[1] * matrix2[5] - matrix2[2] * matrix2[4],
			matrix2[5] * matrix2[6] - matrix2[3] * matrix2[8],
			matrix2[0] * matrix2[8] - matrix2[2] * matrix2[6],
			matrix2[2] * matrix2[3] - matrix2[0] * matrix2[5],
			matrix2[3] * matrix2[7] - matrix2[4] * matrix2[6],
			matrix2[1] * matrix2[6] - matrix2[0] * matrix2[7],
			matrix2[0] * matrix2[4] - matrix2[1] * matrix2[3],
		};
		var invMatrix1 = adjMatrix1.Select(a => PMod(affineReverseReference[PMod(detM1, 26)] * a, 26));
		var invMatrix2 = adjMatrix2.Select(a => PMod(affineReverseReference[PMod(detM2, 26)] * a, 26));
		var resultFullEncryption = "";
		QuickLog("Inverse First Matrix: {0}", invMatrix1.Join());
		QuickLog("Inverse Second Matrix: {0}", invMatrix2.Join());
		var invertSecondMatrix = Random.value < 0.5f;
		var encryptMatrix1First = bombInfo.GetSerialNumberNumbers().LastOrDefault() > 4;
		var outputEncrpytionFirstMatrix = "";
		if (encryptMatrix1First)
		{
			QuickLog("The {0} will be used to encrypt the message, and the {1} will be used to encrypt the result of the first.", invertSecondMatrix ? "first matrix" : "inverse of the first matrix", invertSecondMatrix ? "inverse of the second" : "second");
			for (var x = 0; x < expectedWord.Length / 3; x++)
			{
				var X3AlphaPos = expectedWord.Substring(3 * x, 3).Select(a => 1 + alphabet.IndexOf(a));
				QuickLog("Alphabetic positions of letters {0}-{1}: {2}", 3 * x + 1, 3 * x + 3, X3AlphaPos.Join(", "));
				for (var o = 0; o < 3; o++)
				{
					var sumCurSetValues = Enumerable.Range(0, 3).Sum(b => (invertSecondMatrix ? matrix1 : invMatrix1).ElementAt(3 * o + b) * X3AlphaPos.ElementAt(b));
					QuickLog("{0} = {1}", Enumerable.Range(0, 3).Select(a => string.Format("{0} * {1}", (invertSecondMatrix ? matrix1 : invMatrix1).ElementAt(3 * o + a), X3AlphaPos.ElementAt(a))).Join(" + "), sumCurSetValues);
					QuickLog("modulo 26 = {0}", sumCurSetValues % 26);
					outputEncrpytionFirstMatrix += alphabet[(sumCurSetValues - 1) % alphabet.Length];
				}
			}
			QuickLog(outputEncrpytionFirstMatrix);
			for (var x = 0; x < outputEncrpytionFirstMatrix.Length / 3; x++)
			{
				var X3AlphaPos = outputEncrpytionFirstMatrix.Substring(3 * x, 3).Select(a => 1 + alphabet.IndexOf(a));
				QuickLog("Alphabetic positions of letters {0}-{1}: {2}", 3 * x + 1, 3 * x + 3, X3AlphaPos.Join(", "));
				for (var o = 0; o < 3; o++)
				{
					var sumCurSetValues = Enumerable.Range(0, 3).Sum(b => (invertSecondMatrix ? invMatrix2 : matrix2).ElementAt(3 * o + b) * X3AlphaPos.ElementAt(b));
					QuickLog("{0} = {1}", Enumerable.Range(0, 3).Select(a => string.Format("{0} * {1}", (invertSecondMatrix ? invMatrix2 : matrix2).ElementAt(3 * o + a), X3AlphaPos.ElementAt(a))).Join(" + "), sumCurSetValues);
					QuickLog("modulo 26 = {0}", sumCurSetValues % 26);
					resultFullEncryption += alphabet[(sumCurSetValues - 1) % alphabet.Length];
				}
			}
		}
		else
		{
			QuickLog("The {0} will be used to encrypt the message, and the {1} will be used to encrypt the result of the second.", invertSecondMatrix ? "inverse of the second matrix" : "second matrix", invertSecondMatrix ? "first" : "inverse of the first");
			for (var x = 0; x < expectedWord.Length / 3; x++)
			{
				var X3AlphaPos = expectedWord.Substring(3 * x, 3).Select(a => 1 + alphabet.IndexOf(a));
				QuickLog("Alphabetic positions of letters {0}-{1}: {2}", 3 * x + 1, 3 * x + 3, X3AlphaPos.Join(", "));
				for (var o = 0; o < 3; o++)
				{
					var sumCurSetValues = Enumerable.Range(0, 3).Sum(b => (invertSecondMatrix ? invMatrix2 : matrix2).ElementAt(3 * o + b) * X3AlphaPos.ElementAt(b));
					QuickLog("{0} = {1}", Enumerable.Range(0, 3).Select(a => string.Format("{0} * {1}", (invertSecondMatrix ? invMatrix2 : matrix2).ElementAt(3 * o + a), X3AlphaPos.ElementAt(a))).Join(" + "), sumCurSetValues);
					QuickLog("modulo 26 = {0}", sumCurSetValues % 26);
					outputEncrpytionFirstMatrix += alphabet[(sumCurSetValues - 1) % alphabet.Length];
				}
			}
			QuickLog(outputEncrpytionFirstMatrix);
			for (var x = 0; x < outputEncrpytionFirstMatrix.Length / 3; x++)
			{
				var X3AlphaPos = outputEncrpytionFirstMatrix.Substring(3 * x, 3).Select(a => 1 + alphabet.IndexOf(a));
				QuickLog("Alphabetic positions of letters {0}-{1}: {2}", 3 * x + 1, 3 * x + 3, X3AlphaPos.Join(", "));
				for (var o = 0; o < 3; o++)
				{
					var sumCurSetValues = Enumerable.Range(0, 3).Sum(b => (invertSecondMatrix ? matrix1 : invMatrix1).ElementAt(3 * o + b) * X3AlphaPos.ElementAt(b));
					QuickLog("{0} = {1}", Enumerable.Range(0, 3).Select(a => string.Format("{0} * {1}", (invertSecondMatrix ? matrix1 : invMatrix1).ElementAt(3 * o + a), X3AlphaPos.ElementAt(a))).Join(" + "), sumCurSetValues);
					QuickLog("modulo 26 = {0}", sumCurSetValues % 26);
					resultFullEncryption += alphabet[(sumCurSetValues - 1) % alphabet.Length];
				}
			}
		}
		displays = new List<string>
        {
            resultFullEncryption,
            detM1.ToString(),
            detM2.ToString()
        };
		invertDisplays = new List<bool> {
			false,
			!invertSecondMatrix,
			invertSecondMatrix
		};
        for (var x = 0; x < matrix1.Length / 3; x++)
        {
			displays.Add(matrix1.Skip(3 * x).Take(3).Select(a => a.ToString("00")).Join("-"));
			invertDisplays.Add(!invertSecondMatrix);
        }
        for (var x = 0; x < matrix2.Length / 3; x++)
        {
			displays.Add(matrix2.Skip(3 * x).Take(3).Select(a => a.ToString("00")).Join("-"));
			invertDisplays.Add(invertSecondMatrix);
		}

        maxPages = (displays.Count + screenTexts.Length - 1) / screenTexts.Length;
		DisplayPage();

	}
	void HandleInput(int idx)
	{
		if (inputtedWord.Length >= maxLengthInput) return;
		inputtedWord += keyboardLayout[idx];
		for (var x = 0; x < screenTexts.Length; x++)
		{
			screenTexts[x].text = x + 1 >= screenTexts.Length ? inputtedWord : "";
			screenTexts[x].characterSize = x + 1 >= screenTexts.Length ? inputtedWord.Length > 6 ? 0.03f : 0.04f : 0.04f;
			screenTexts[x].color = Color.white;
			displayRenderers[x].material.color = Color.black;
		}
		subText.text = "SUB";
	}

	void HandlePageDelta(int delta = 0)
	{
		curPageIdx += delta;
		if (curPageIdx < 0)
			curPageIdx = maxPages - 1;
		else if (curPageIdx >= maxPages)
			curPageIdx = 0;
		DisplayPage();
	}

	void DisplayPage()
	{
		subText.text = string.Format("{0}", curPageIdx + 1, maxPages);
		for (var x = 0; x < screenTexts.Length; x++)
		{
			screenTexts[x].characterSize = 0.05f;
			screenTexts[x].text = displays.ElementAtOrDefault(x + curPageIdx * screenTexts.Length) ?? "";
			if (invertDisplays.ElementAtOrDefault(x + curPageIdx * screenTexts.Length))
			{
				screenTexts[x].color = Color.black;
				displayRenderers[x].material.color = Color.white;
			}
			else
            {
				screenTexts[x].color = Color.white;
				displayRenderers[x].material.color = Color.black;
			}

		}
		inputtedWord = "";
	}

	void HandleSubmit()
	{
		if (string.IsNullOrEmpty(expectedWord) || inputtedWord == expectedWord)
		{
			modSelf.HandlePass();
			moduleSolved = true;
			mAudio.PlaySoundAtTransform("SolveSFX", transform);
			for (var x = 0; x < screenTexts.Length; x++)
			{
				screenTexts[x].characterSize = 0.05f;
				screenTexts[x].text = "";
			}
		}
		else
		{
			if (string.IsNullOrEmpty(inputtedWord))
				QuickLog("Submitted <no letters>. I do not know about that one...");
			else
				QuickLog("Submitted \"{0}\". I do not know about that one...", inputtedWord);
			mAudio.PlaySoundAtTransform("StrikeSFX", transform);
			curPageIdx = 0;
			modSelf.HandleStrike();
			DisplayPage();
		}
	}
	int ObtainGCM(int firstValue, int secondValue)
	{
		var maxVal = firstValue < secondValue ? secondValue : firstValue;
		var minVal = firstValue < secondValue ? firstValue : secondValue;
		while (minVal > 0)
		{
			var newMin = maxVal % minVal;
			maxVal = minVal;
			minVal = newMin;
		}
		return maxVal;
	}
	int PMod(int dividend, int divisor)
	{
		return ((dividend % divisor) + divisor) % divisor;
	}
	private int getPositionFromChar(char c)
	{
		return keyboardLayout.IndexOf(c);
	}
	void Update()
	{
		if (moduleSelected)
		{
			for (var ltr = 0; ltr < 26; ltr++)
				if (Input.GetKeyDown(((char)('a' + ltr)).ToString()))
					keyboard[getPositionFromChar((char)('A' + ltr))].OnInteract();
			if (Input.GetKeyDown(KeyCode.Return))
				submit.OnInteract();
			if (Input.GetKeyDown(KeyCode.RightArrow))
				rightArrow.OnInteract();
			if (Input.GetKeyDown(KeyCode.LeftArrow))
				leftArrow.OnInteract();
		}
	}
#pragma warning disable 414
	protected string TwitchHelpMessage = "!{0} right/left/r/l [move between screens] | !{0} submit <answer> [Submits <answer> of 6 letters in length]";
#pragma warning restore 414

	protected IEnumerator ProcessTwitchCommand(string command)
	{
		if (command.EqualsIgnoreCase("right") || command.EqualsIgnoreCase("r"))
		{
			yield return null;
			yield return new[] { rightArrow };
			yield break;
		}

		if (command.EqualsIgnoreCase("left") || command.EqualsIgnoreCase("l"))
		{
			yield return null;
			yield return new[] { leftArrow };
			yield break;
		}

		var split = command.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length != 2 || !split[0].Equals("SUBMIT"))
			yield break;
		if (split[1].Length > maxLengthInput || split[1].Length < minLengthInput)
        {
			yield return string.Format("sendtochaterror You're attempting to send a word that is {0} letter(s) long! The correct word does not have that many letters.", split[1].Length);
			yield break;
        }
		var buttons = split[1].Select(getPositionFromChar).ToArray();
		if (buttons.Any(x => x < 0))
			yield break;

		yield return null;

		foreach (var let in split[1])
		{
			yield return new WaitForSeconds(.1f);
			keyboard[getPositionFromChar(let)].OnInteract();
		}
		yield return new WaitForSeconds(.25f);
		submit.OnInteract();
		yield return new WaitForSeconds(.1f);
	}

	protected IEnumerator TwitchHandleForcedSolve()
	{
		if (!expectedWord.StartsWith(inputtedWord))
		{
			leftArrow.OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		for (var i = inputtedWord.Length; i < expectedWord.Length; i++)
		{
			keyboard[getPositionFromChar(expectedWord[i])].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		submit.OnInteract();
		yield return new WaitForSeconds(0.1f);
	}
}
