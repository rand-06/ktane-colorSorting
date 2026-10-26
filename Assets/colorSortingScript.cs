using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;
using System.Text.RegularExpressions;

public class colorSortingScript : MonoBehaviour
{
	public GameObject button;
	public KMSelectable colorblindToggle;
	public Transform buttonParent;
	public TextMesh[] leafNumbers;
	
	private GameObject[] buttons;
	private TextMesh[] colorblindTexts;
	private int size;
	private float r;
	private List<int> config;
	private int selected = -1;
	private bool moduleSolved;
	private string[] coordinates;
	
	bool TwitchPlaysActive;

	private readonly List<Color> colors = new List<Color>
	{
		Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow
	};

	private const string colorLetters = "RGBCMY";

	// WAIT, I CAN DO THAT? WTF???
	bool check() =>
		Enumerable.Range(0, 6)
			.All(i => config
				.Skip(i * size * size)
				.Take(size * size)
				.All(x => x == config[i * size * size]));

	void press(int index)
	{
		if (selected == -1)
		{
			selected = index;
			buttons[index].GetComponent<MeshRenderer>().material.color = Color.white;
		}
		else
		{
			int temp = config[selected];
			config[selected] = config[index];
			config[index] = temp;
			buttons[index].GetComponent<MeshRenderer>().material.color = colors[config[index]];
			buttons[selected].GetComponent<MeshRenderer>().material.color = colors[config[selected]];
			Color tempColor = colorblindTexts[selected].color;
			colorblindTexts[selected].color = colorblindTexts[index].color;
			colorblindTexts[index].color = tempColor;
			if (colorblindTexts[0].text.Length == 1)
			{
				colorblindTexts[index].text = colorLetters[config[index]].ToString();
				colorblindTexts[selected].text = colorLetters[config[selected]].ToString();
			}

			selected = -1;
			if (!moduleSolved && check())
			{
				GetComponent<KMBombModule>().HandlePass();
				moduleSolved = true;
			}
		}
	}

	float getX(int index)
	{
		int sector = index / size / size;
		if (sector > 2) return -getX(index - 3 * size * size);
		index %= size * size;
		if (sector == 0)
		{
			return r * (index % size - index / size);
		}
		return r * (2*(index%size)+index/size+(float)Math.Sqrt(3));
	}

	float getY(int index)
	{
		int sector = index / size / size;
		if (sector > 2) return -getY(index - 3 * size * size);
		index %= size * size;
		if (sector == 0)
		{
			return r * (2 + (float)Math.Sqrt(3)*(index/size+index%size));
		}
		return r * (sector==1?1:-1) * (1+(float)Math.Sqrt(3)*(index/size));
	}
	
	void Awake()
	{
		ModConfig<ColorSortingSettings> modConfig = new ModConfig<ColorSortingSettings>("ColorSortingSettings");
		Settings = modConfig.Settings;
		modConfig.Settings = Settings;
		TryOverrideMission();
		size = Settings.size < 1 || Settings.size>20 ?3:Settings.size;
		buttons = Enumerable.Range(0, 6*size*size).Select(x => Instantiate(button, buttonParent)).ToArray();
		button.SetActive(false);
		GetComponent<KMSelectable>().Children = Enumerable.Range(0, 6*size*size+1)
			.Select(x => x==0?colorblindToggle:buttons[x-1].GetComponent<KMSelectable>()).ToArray();
		r = 0.06f / ((2 * size - 2) * (float)Math.Sqrt(3) + 3);
		Vector3 buttonSize = new Vector3(2*r,button.transform.localScale.y, 2*r);
		float buttonPosY = button.transform.localPosition.y;
		config = Enumerable.Range(0, 6 * size * size).Select(index => index/size/size).ToList().Shuffle();
		colorblindTexts = buttons.Select(x => x.transform.GetComponentInChildren<TextMesh>()).ToArray();
		GetComponent<KMBombModule>().OnActivate += delegate
		{
			if (TwitchPlaysActive) coordinates = new string[6 * size * size];
			else foreach (var leafNumber in leafNumbers) leafNumber.text = "";

			for (int i = 0; i < 6 * size * size; i++)
			{
				int index = i;
				buttons[index].GetComponent<MeshRenderer>().material.color = colors[config[index]];
				if (config[index] == 3 || config[index] == 5) colorblindTexts[index].color = Color.black;
				if (TwitchPlaysActive)
				{
					int coord = index % (size * size);
					coordinates[index] = (char)(coord % size + 'A') + (coord / size + 1).ToString();
					colorblindTexts[index].text = coordinates[index];
				}
				else
				{
					colorblindTexts[index].text = "";
				}

				buttons[index].transform.localScale = buttonSize;
				buttons[index].transform.localPosition = new Vector3(getX(index), buttonPosY, getY(index));
				buttons[index].GetComponent<KMSelectable>().OnInteract += delegate
				{
					press(index);
					return false;
				};
			}

			colorblindToggle.OnInteract += delegate
			{
				bool colorblindMode = colorblindTexts[0].text.Length == 1;
				for (int i = 0; i < 6 * size * size; i++)
				{
					colorblindTexts[i].text = (!colorblindMode
						? colorLetters[config[i]].ToString()
						: TwitchPlaysActive
							? coordinates[i]
							: "");
				}

				return false;
			};
		};
	}

	private ColorSortingSettings Settings = new ColorSortingSettings();
	
	void TryOverrideMission()
	{
		var desc = Game.Mission.Description ?? "";
		Match regexMatchCountVariants = Regex.Match(desc, @"\[Color Sorting\]\s(\d+)");
		if (!regexMatchCountVariants.Success) return;
		int? valueMatches = regexMatchCountVariants.Groups[1].Value.TryParseInt();
		if (valueMatches != null) Settings.size = valueMatches.Value;
	}
	
	class ColorSortingSettings
	{
		public int size = 3;
	}

	static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
	{
		new Dictionary<string, object>
		{
			{ "Filename", "ColorSortingSettings.json" },
			{ "Name", "Color Sorting Settings" },
			{ "Listings", new List<Dictionary<string, object>>{
				new Dictionary<string, object>
				{
					{ "Key", "size" },
					{ "Text", "Size" },
					{ "Description", "Size of the grid. Default is 3." }
				},
			} }
		}
	};
	
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} #X# to input a button to press. Chain the command by spaces. For example: !{0} 1A1 3B2. Use !{0} colorblind to toggle colorblind mode.";
#pragma warning restore 414
	
	public IEnumerator ProcessTwitchCommand(string Command)
	{
		yield return null;
		List<string> args = Command.ToUpper().Trim().Split(' ').Where(x => !string.IsNullOrEmpty(x)).ToList();
		if (args[0] == "COLORBLIND") { colorblindToggle.OnInteract(); yield break;}
		foreach (string arg in args)
		{
			if (Regex.IsMatch(arg, @"[1-6][A-T]\d+$"))
			{
				Match match = Regex.Match(arg, @"([1-6])([A-T])(\d+)$");
				int leaf = int.Parse(match.Groups[1].Value)-1;
				int col = char.Parse(match.Groups[2].Value)-'A';
				int row =  int.Parse(match.Groups[3].Value)-1;
				if (leaf > 5 || col >= size || row >= size) yield break;
				yield return null;
				press(leaf * size * size + row * size + col);
			}
			else yield break;
		}
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		GetComponent<KMBombModule>().HandlePass();
		moduleSolved = true;
	}
}
