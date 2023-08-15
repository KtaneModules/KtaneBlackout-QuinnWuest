using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text;
using System.Globalization;

public class BlackoutScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMBossModule Boss;
    public KMSelectable Sel;
    public MeshRenderer SurfaceRend;
    public TextMesh SurfaceText;
    public KMColorblindMode ColorblindMode;
    public TextMesh ColorblindText;

    public enum LogicGate
    {
        XOR,
        NOR,
        AND,
        NOT,
        OR,
    }

    private static readonly Color32[] _colors = new Color32[]
    {
        new Color32(0, 0, 0, 255),
        new Color32(70, 70, 255, 255),
        new Color32(70, 255, 70, 255),
        new Color32(70, 255, 255, 255),
        new Color32(255, 70, 70, 255),
        new Color32(255, 70, 255, 255),
        new Color32(255, 255, 70, 255),
        new Color32(255, 255, 255, 255)
    };

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int _stageCount;
    private string[] _ignoredModules;
    private readonly List<string> _currentSolves = new List<string>();

    private int _displayedColor;
    private int _internalColor;
    private static readonly string[] _colorNames = new string[8] { "BLACK", "BLUE", "GREEN", "CYAN", "RED", "MAGENTA", "YELLOW", "WHITE" };
    private Coroutine _fadeAnimation;
    private Coroutine _pressAnimation;
    private LogicGate _currentLogicGate = LogicGate.XOR;
    private bool _requirePress;
    private bool _successfullyPressed;
    private bool _colorblindMode;
    private bool _isAutosolving;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Sel.OnInteract += SelPress;
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        ColorblindText.gameObject.SetActive(_colorblindMode);

        if (_ignoredModules == null)
            _ignoredModules = Boss.GetIgnoredModules("Blackout", new string[] { "Blackout" });
        _stageCount = BombInfo.GetSolvableModuleNames().Count(i => !_ignoredModules.Contains(i));
        if (_stageCount == 0)
        {
            Debug.LogFormat("[Blackout #{0}] No stages generated.", _moduleId);
            Module.HandlePass();
            ColorblindText.text = "";
            _moduleSolved = true;
        }

        SurfaceText.text = (_currentSolves.Count).ToString();
        _displayedColor = Rnd.Range(0, 8);
        ColorblindText.text = _colorNames[_displayedColor];
        _internalColor = _displayedColor;
        if (_fadeAnimation != null)
            StopCoroutine(_fadeAnimation);
        _fadeAnimation = StartCoroutine(FadeColor(0, _displayedColor));
        Debug.LogFormat("[Blackout #{0}] Stage {1}: {2} was chosen.", _moduleId, _currentSolves.Count, _colorNames[_displayedColor]);
        _requirePress = _displayedColor == 0;
        if (_requirePress)
            Debug.LogFormat("[Blackout #{0}] BLACKOUT!", _moduleId);
    }

    private bool SelPress()
    {
        Sel.AddInteractionPunch();
        if (_moduleSolved)
            return false;
        if (!_requirePress)
        {
            if (_pressAnimation != null)
                StopCoroutine(_pressAnimation);
            _pressAnimation = StartCoroutine(PressAnimation());
            Debug.LogFormat("[Blackout #{0}] The screen was pressed when a blackout was not present. Strike.", _moduleId);
            Audio.PlaySoundAtTransform(string.Format(@"{0}out", _colorNames[_internalColor].ToLowerInvariant()), transform);
            Module.HandleStrike();
            return false;
        }
        if (_requirePress && !_successfullyPressed)
        {
            if (_pressAnimation != null)
                StopCoroutine(_pressAnimation);
            _pressAnimation = StartCoroutine(PressAnimation());
            Audio.PlaySoundAtTransform("blackout", transform);
            _successfullyPressed = true;
            Debug.LogFormat("[Blackout #{0}] The screen was correctly pressed when a blackout was present.", _moduleId);
        }
        return false;
    }

    private IEnumerator PressAnimation()
    {
        var str = _colorNames[_internalColor] + "OUT";
        SurfaceText.fontSize = (_internalColor == 5) ? 200 : 256;
        if (_internalColor != 0)
            str += "?";
        else
            str += "!";
        for (int i = 0; i < str.Length; i++)
        {
            SurfaceText.text = str.Substring(0, i + 1);
            yield return new WaitForSeconds(0.035f);
        }
        if (_moduleSolved)
            yield break;
        yield return new WaitForSeconds(0.5f);
        SurfaceText.fontSize = 256;
        SurfaceText.text = BombInfo.GetSolvedModuleNames().Count().ToString();
    }

    private IEnumerator FadeColor(int oldC, int newC)
    {
        var duration = 0.3f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            SurfaceRend.material.color = new Color32((byte)Easing.InOutQuad(elapsed, _colors[oldC].r, _colors[newC].r, duration), (byte)Easing.InOutQuad(elapsed, _colors[oldC].g, _colors[newC].g, duration), (byte)Easing.InOutQuad(elapsed, _colors[oldC].b, _colors[newC].b, duration), 255);
            SurfaceText.color = new Color32((byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), (byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), (byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), 255);
            ColorblindText.color = new Color32((byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), (byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), (byte)Easing.InOutQuad(elapsed, oldC == 7 ? 0 : 255, newC == 7 ? 0 : 255, duration), 255);
            yield return null;
            elapsed += Time.deltaTime;
        }
        SurfaceRend.material.color = _colors[newC];
        SurfaceText.color = new Color32((byte)(newC == 7 ? 0 : 255), (byte)(newC == 7 ? 0 : 255), (byte)(newC == 7 ? 0 : 255), 255);
        ColorblindText.color = new Color32((byte)(newC == 7 ? 0 : 255), (byte)(newC == 7 ? 0 : 255), (byte)(newC == 7 ? 0 : 255), 255);
    }

    private void Update()
    {
        if (_moduleSolved)
            return;
        var solvedModules = BombInfo.GetSolvedModuleNames();
        if (solvedModules.Count == 0)
            return;
        if (_currentSolves.Count != solvedModules.Count) // New stage
        {
            if (solvedModules.Count >= _stageCount)
            {
                _moduleSolved = true;
                _internalColor = 0;
                StartCoroutine(PressAnimation());
                Debug.LogFormat("[Blackout #{0}] ===================================", _moduleId);
                Debug.LogFormat("[Blackout #{0}] Last stage reached. Module solved.", _moduleId);
                StartCoroutine(FadeColor(_displayedColor, 0));
                Audio.PlaySoundAtTransform("blackout", transform);
                Module.HandlePass();
                return;
            }
            if (_requirePress && !_successfullyPressed)
            {
                Debug.LogFormat("[Blackout #{0}] A blackout was present, but the screen was not pressed. Strike.", _moduleId);
                Audio.PlaySoundAtTransform("oh_no", transform);
                Module.HandleStrike();
            }
            else if (!_isAutosolving)
                Audio.PlaySoundAtTransform("okay", transform);
            _successfullyPressed = false;
            SurfaceText.text = solvedModules.Count.ToString();
            var lastsolved = GetLastSolve(solvedModules, _currentSolves);
            var lastSolveReformatted = ReformatModName(lastsolved);
            _currentLogicGate = GetLogicGate(lastSolveReformatted);
            var oldInternalColor = _internalColor;
            var oldDisplayedColor = _displayedColor;
            _displayedColor = Rnd.Range(0, 8);
            ColorblindText.text = _colorNames[_displayedColor];
            _internalColor = GetNewColor(oldInternalColor, _displayedColor, _currentLogicGate);
            Debug.LogFormat("[Blackout #{0}] ===================================", _moduleId);
            Debug.LogFormat("[Blackout #{0}] Stage {1}: {2} was chosen.", _moduleId, solvedModules.Count, _colorNames[_displayedColor]);
            Debug.LogFormat("[Blackout #{0}] The last solved module was {1}.", _moduleId, lastsolved);
            Debug.LogFormat("[Blackout #{0}] The logic gate for this stage is {1}.", _moduleId, _currentLogicGate);
            Debug.LogFormat("[Blackout #{0}] Applying {1} and {2} using {3} results in {4}.", _moduleId, _colorNames[oldInternalColor], _colorNames[_displayedColor], _currentLogicGate, _colorNames[_internalColor]);
            if (_internalColor == 0)
            {
                _requirePress = true;
                Debug.LogFormat("[Blackout #{0}] BLACKOUT!", _moduleId);
            }
            else
                _requirePress = false;
            if (_fadeAnimation != null)
                StopCoroutine(_fadeAnimation);
            _fadeAnimation = StartCoroutine(FadeColor(oldDisplayedColor, _displayedColor));
        }
    }

    private string GetLastSolve(List<string> solved, List<string> cur)
    {
        for (int i = 0; i < cur.Count; i++)
            solved.Remove(cur.ElementAt(i));
        for (int i = 0; i < solved.Count; i++)
            _currentSolves.Add(solved.ElementAt(i));
        return solved.ElementAt(0);
    }

    private string ReformatModName(string modName)
    {
        var normalizedString = modName.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (var c in normalizedString)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(c);
        var sb = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        var str = "";
        foreach (var c in sb)
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray().Contains(c))
                str += c.ToString();
        return str;
    }

    private LogicGate GetLogicGate(string modName)
    {
        for (int i = 0; i < 5; i++)
        {
            var logic = (LogicGate)i;
            if (modName.Contains(logic.ToString()))
                return logic;
        }
        for (int i = 0; i < 5; i++)
        {
            var logic = ((LogicGate)i).ToString();
            var ixs = Enumerable.Range(0, logic.Length).Select(ix => modName.IndexOf(logic[ix])).ToArray();
            if (ixs.OrderBy(x => x).SequenceEqual(ixs) && ixs.Distinct().Count() == ixs.Count() && ixs[0] != -1)
                return (LogicGate)i;
        }
        return (LogicGate)(((int)_currentLogicGate + 1) % 5);
    }

    private int GetNewColor(int colorA, int colorB, LogicGate logic)
    {
        if (logic == LogicGate.XOR)
            return colorA ^ colorB;
        if (logic == LogicGate.NOR)
            return (colorA | colorB) ^ 7;
        if (logic == LogicGate.AND)
            return colorA & colorB;
        if (logic == LogicGate.NOT)
            return colorA ^ 7;
        if (logic == LogicGate.OR)
            return colorA | colorB;
        throw new InvalidOperationException("Invalid logic gate in GetNewColor method in Blackout.");
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} push [Push the screen.] | !{0} colorblind [Toggle colorblind mode.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(colou?rblind|cb)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            _colorblindMode = !_colorblindMode;
            ColorblindText.gameObject.SetActive(_colorblindMode);
            yield break;
        }
        m = Regex.Match(command, @"^\s*(push|press|submit)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        if (_requirePress && !_successfullyPressed)
            yield return "awardpoints 1";
        Sel.OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        _isAutosolving = true;
        while (!_moduleSolved)
        {
            if (_requirePress && !_successfullyPressed)
                Sel.OnInteract();
            yield return true;
        }
    }
}
