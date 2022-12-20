using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class RedHuffmanCipherScript : MonoBehaviour {
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMSelectable leftArrow, rightArrow, submit;
	public KMSelectable[] keyboard;
	public TextMesh[] screenTexts;
	public TextMesh subText;
	static int modIDCnt;
	int moduleID;
	string inputtedWord = "", expectedWord;
	const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM";
	const int maxLengthInput = 8;
	List<string> displays;
	bool moduleSelected, moduleSolved;
	int curPageIdx, maxPages;
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[Red Huffman Cipher #{0}] {1}", moduleID, string.Format(value, args));
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
			return false; };

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
			if (!moduleSolved) {
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

		DisplayPage();
	}
	void HandleInput(int idx)
    {
		if (inputtedWord.Length >= maxLengthInput) return;
		inputtedWord += keyboardLayout[idx];
		for (var x = 0; x < screenTexts.Length; x++)
		{
			screenTexts[x].text = x + 1 >= screenTexts.Length ? inputtedWord : "";
			screenTexts[x].characterSize = x + 1 >= screenTexts.Length ? inputtedWord.Length > 6 ? 0.03f : 0.045f : 0.04f;
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
		subText.text = string.Format("{0}/{1}",curPageIdx + 1, maxPages);
		for (var x = 0; x < screenTexts.Length; x++)
		{
			screenTexts[x].characterSize = 0.04f;
			screenTexts[x].text = displays.ElementAtOrDefault(x + curPageIdx * screenTexts.Length) ?? "";
		}
		inputtedWord = "";
    }

	void HandleSubmit()
    {
		if (inputtedWord == expectedWord)
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
	void GenerateModule()
    {
		var HuffmanEncodingsAll = new List<string> { "0", "1" };
		var possibleDigits = "01";
		var outputEncodingHuffmanTree = "";
		var finalBinary = "";
		var currentDigitString = "";
		// Generate the Huffman Tree
		//QuickLog("Constructing Huffman Tree for Digits:");
		while (HuffmanEncodingsAll.Count < 26)
		{
			currentDigitString += possibleDigits.PickRandom();
			if (HuffmanEncodingsAll.Count(a => a.StartsWith(currentDigitString)) == 1)
			{
				outputEncodingHuffmanTree += currentDigitString;
				var idxObtained = HuffmanEncodingsAll.IndexOf(currentDigitString);
				//QuickLog("Detected \"{0}\" with only 1 entry. Splitting and clearing...", currentDigitString);

				HuffmanEncodingsAll[idxObtained] += '0';
				if (idxObtained + 1 < HuffmanEncodingsAll.Count)
					HuffmanEncodingsAll.Insert(idxObtained + 1, currentDigitString + '1');
				else
					HuffmanEncodingsAll.Add(currentDigitString + '1');
				currentDigitString = "";
				//QuickLog("Leafing nodes after splitting: [{0}]", HuffmanEncodingsAll.Join("],["));
			}
		}
		finalBinary += outputEncodingHuffmanTree;
		expectedWord = CipherMachineData._allWords[Random.Range(4, 9)].PickRandom();

		finalBinary += expectedWord.Select(a => HuffmanEncodingsAll[alphabet.IndexOf(a)]).Join("");

		var SubstitutionEncodings = new Dictionary<string, char> {
			{ "0000", 'A' },
			{ "0001", 'B' },
			{ "0010", 'C' },
			{ "0011", 'D' },
			{ "0100", 'E' },
			{ "0101", 'F' },
			{ "0110", 'G' },
			{ "0111", 'H' },
			{ "1000", 'I' },
			{ "1001", 'J' },
			{ "1010", 'K' },
			{ "1011", 'L' },
			{ "1100", 'M' },
			{ "1101", 'N' },
			{ "1110", 'O' },
			{ "1111", 'P' },
			{ "00", 'Q' },
			{ "01", 'R' },
			{ "10", 'S' },
			{ "11", 'T' },
			{ "0", 'U' },
			{ "1", 'V' },
		};
		var displayableEncodings = "";
		for (var x = 0; x < finalBinary.Length; )
        {
			var remainingBinary = finalBinary.Substring(x);
			//QuickLog(remainingBinary);
			var PossibleSubstitutions = SubstitutionEncodings.Where(a => remainingBinary.StartsWith(a.Key));
			var longestSubstitutionPossible = PossibleSubstitutions.First();
			displayableEncodings += longestSubstitutionPossible.Value;
			x += longestSubstitutionPossible.Key.Length;
        }
		
		displays = new List<string>();
		for (var x = 0; x < displayableEncodings.Length;x += 6)
		{
			var remainingEncodings = displayableEncodings.Substring(x);
			displays.Add(remainingEncodings.Substring(0, Mathf.Min(6, remainingEncodings.Length)));
		}
        maxPages = (displays.Count + screenTexts.Length - 1) / screenTexts.Length;
		// Start reverse procedure to mimic the Huffman's loggings
		QuickLog("Displayed Encodings from {1} page(s): {0}", displayableEncodings, maxPages);
		QuickLog("Use these conversions to convert each character from the encodings obtain the original binary string: {0}", SubstitutionEncodings.Where(a => displayableEncodings.Contains(a.Value)).Select(a => string.Format("[{0} = {1}]", a.Value, a.Key)).Join("; "));
        QuickLog("Obtained Binary String: {0}", finalBinary);
		// Example Huffman Construction
		var HuffmanConstructionExample = new List<string> { "0", "1" };
		var curIdxBinaryRead = 0;
		QuickLog("Starting Huffman Tree Construction with the following leafing nodes: [{0}]", HuffmanConstructionExample.Join("],["));
		while (HuffmanConstructionExample.Count < 26)
		{
			var remainingBinary = finalBinary.Substring(curIdxBinaryRead);
			var matchingBinary = HuffmanConstructionExample.Single(a => remainingBinary.StartsWith(a));
			var idxOfMatch = HuffmanConstructionExample.IndexOf(matchingBinary);
			QuickLog("[{0}]{1}", matchingBinary, remainingBinary.Substring(matchingBinary.Length));
			HuffmanConstructionExample[idxOfMatch] += '0';
			if (idxOfMatch + 1 < HuffmanConstructionExample.Count)
				HuffmanConstructionExample.Insert(idxOfMatch + 1, matchingBinary + '1');
			else
				HuffmanConstructionExample.Add(matchingBinary + '1');
			curIdxBinaryRead += matchingBinary.Length;
			QuickLog("Huffman Tree Construction after reading {0} digit{1}: [{2}]", curIdxBinaryRead, curIdxBinaryRead == 1 ? "" : "s", HuffmanConstructionExample.Join("],["));
		}
		
		QuickLog("Reading The Constructed Huffman Tree:");
		while (curIdxBinaryRead < finalBinary.Length)
        {
			var remainingBinary = finalBinary.Substring(curIdxBinaryRead);
			var matchingBinary = HuffmanConstructionExample.Single(a => remainingBinary.StartsWith(a));
			var idxOfMatch = HuffmanConstructionExample.IndexOf(matchingBinary);
			QuickLog("[{0}]{1}", matchingBinary, remainingBinary.Length == matchingBinary.Length ? "" : remainingBinary.Substring(matchingBinary.Length));
			QuickLog("{0}", HuffmanConstructionExample.Select(a => string.Format("{1}{0}{2}", a, matchingBinary == a ? '[' : '(', matchingBinary == a ? ']' : ')')).Join(","));
			QuickLog("{0}", Enumerable.Range(0,26).Select(a => string.Format("{1}{0}{2}", alphabet[a], idxOfMatch == a ? '[' : '(', idxOfMatch == a ? ']' : ')')).Join(","));
			QuickLog("{0} = \"{1}\"", matchingBinary, alphabet[idxOfMatch]);
			curIdxBinaryRead += matchingBinary.Length;
		}

		QuickLog("Decoded word: {0}", expectedWord);
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
	protected string TwitchHelpMessage = "!{0} right/left [move between screens] | !{0} submit <answer> [Submits <answer>]";
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
		if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length > 8)
			yield break;
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
