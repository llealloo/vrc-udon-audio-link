#if UDONSHARP
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace AudioLink
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioLinkMidiControl : UdonSharpBehaviour
    {
        // Configuration version that this version of AudioLink uses for Midi control
        private const int _midiConfigFormatVersion = 1;

        public AudioLinkController _audioLinkController;
        public bool _midiEnabled;
        public int _midiChannel;
        public int _resetWait = 2;
        public TextAsset _midiConfig;

        public Text _currentDeckName;

        private ThemeColorController _themeColorController;
        private DataDictionary _json;
        private DataList _midiDecks;
        private bool _usableConfig;
        private int _selectedDeck;
        private int _selectedband;
        private bool _countdownReset;
        private int _resetTimer;

        private string[] _midiKeyMap = new string[128];
        private string[] _midiKnobMap = new string[128];

        void Start()
        {

            _themeColorController = _audioLinkController.themeColorController;
            _resetTimer = _resetWait * 50;
            LoadConfig();
            LoadDeck(0);

        }

        void FixedUpdate()
        {
            if (_countdownReset)
            {
                _resetTimer--;
                if (_resetTimer <= 0)
                {
                    // Trigger Controller Reset
                    _audioLinkController.ResetSettings();
                    _themeColorController.ResetThemeColors();

                    _countdownReset = false;
                    _resetTimer = _resetWait * 50;
                }
            }
        }

        public void NextDeck()
        {
            _selectedDeck++;
            if (_selectedDeck >= _midiDecks.Count) _selectedDeck = 0;
            LoadDeck(_selectedDeck);
        }

        public void PrevDeck()
        {
            _selectedDeck--;
            if (_selectedDeck < 0) _selectedDeck = _midiDecks.Count - 1;
            LoadDeck(_selectedDeck);
        }

        private void LoadConfig()
        {

            // Check if a config file is present, else use built-in
            if (VRC.SDKBase.Utilities.IsValid(_midiConfig))
            {

                if (!VRCJson.TryDeserializeFromJson(_midiConfig.text, out DataToken jsonDataToken)) // Json had a parsing error
                {
                    Debug.LogError("[AudioLink-Midi] Json Parsing Failed!");
                    return;
                }

                _json = jsonDataToken.DataDictionary;
                if ((int)_json["ConfigFormat"].Number != _midiConfigFormatVersion) // Config uses different formatting
                {
                    Debug.LogError("[AudioLink-Midi] Configuration uses a different configuration format! Please use a newer or older file with the same format (" + _midiConfigFormatVersion + ")!");
                    return;
                }

                _midiDecks = _json["MidiDecks"].DataList;
                _usableConfig = true;

            }
            else
            {

                Debug.LogWarning("[AudioLink-Midi] Midi Controller has no configuration file!");

            }

        }

        private void LoadDeck(int id)
        {

            if (_usableConfig)
            {

                _midiKeyMap = new string[128];
                _midiKnobMap = new string[128];

                DataDictionary selectedDeck = _midiDecks[id].DataDictionary;

                DataDictionary deckKeys = selectedDeck["Keys"].DataDictionary;

                DataToken[] keys = deckKeys.GetKeys().ToArray();
                for (int index = 0; index < keys.Length; index++)
                {

                    string key = keys[index].String;
                    _midiKeyMap[(int)deckKeys[key].DataDictionary["ID"].Number] = key;

                }

                DataDictionary deckKnobs = selectedDeck["Knobs"].DataDictionary;

                DataToken[] knobs = deckKnobs.GetKeys().ToArray();
                for (int index = 0; index < knobs.Length; index++)
                {

                    string knob = knobs[index].String;
                    _midiKnobMap[(int)deckKnobs[knob].DataDictionary["ID"].Number] = knob;

                }

                if (VRC.SDKBase.Utilities.IsValid(_currentDeckName)) _currentDeckName.text = selectedDeck["Name"].String;

            }
            else
            {

                Debug.LogWarning("[AudioLink-Midi] Configuration is not loaded or broken!");

            }

        }

        private bool AllowMidi(int channel)
        {

            if (VRC.SDKBase.Utilities.IsValid(_audioLinkController)) if (VRC.SDKBase.Utilities.IsValid(_themeColorController)) return _midiEnabled && channel == _midiChannel && Networking.IsOwner(_audioLinkController.gameObject);
            return false;

        }

        private void MidiNoteChange(int channel, int key, int value, bool state)
        {

            // Check Off Midi
            if (AllowMidi(channel))
            {

                switch (_midiKeyMap[key])
                {

                    case "Enabled":
                        // Toggle AudioLink on key release
                        if (!state) _audioLinkController.powerToggle.isOn = !_audioLinkController.powerToggle.isOn;
                        break;

                    case "ColorChord":
                        // Toggle ColorChord on key release
                        if (!state) _themeColorController.themeColorToggle.isOn = !_themeColorController.themeColorToggle.isOn;
                        break;

                    case "AutoGain":
                        // Toggle AutoGain on key release
                        if (!state) _audioLinkController.autoGainToggle.isOn = !_audioLinkController.autoGainToggle.isOn;
                        break;

                    case "Reset":
                        _countdownReset = state;
                        if (!state) _resetTimer = _resetWait * 50;
                        break;

                    case "BandSelect0":
                        // Set Selected Band to 1 when held
                        _selectedband = state ? 1 : 0;
                        _themeColorController.SelectCustomColor0();
                        break;

                    case "BandSelect1":
                        // Set Selected Band to 2 when held
                        _selectedband = state ? 2 : 0;
                        _themeColorController.SelectCustomColorN(state ? 1 : 0);
                        break;

                    case "BandSelect2":
                        // Set Selected Band to 3 when held
                        _selectedband = state ? 3 : 0;
                        _themeColorController.SelectCustomColorN(state ? 2 : 0);
                        break;

                    case "BandSelect3":
                        // Set Selected Band to 4 when held
                        _selectedband = state ? 4 : 0;
                        _themeColorController.SelectCustomColorN(state ? 3 : 0);
                        break;

                    case "vEnable": // Software Enable
                        _audioLinkController.powerToggle.isOn = state;
                        break;

                    case "vColorChord": // Software ColorChord
                        _themeColorController.themeColorToggle.isOn = state;
                        break;

                    case "vAutoGain": // Software AutoGain
                        _audioLinkController.autoGainToggle.isOn = state;
                        break;

                    default:
                        // No-op
                        break;

                }

            }

        }

        public override void MidiControlChange(int channel, int key, int value)
        {

            // Check Change Midi
            if (AllowMidi(channel))
            {

                float floatValue = (float)value / 127f;

                float cutoff = floatValue / 4;

                switch (_midiKnobMap[key])
                {

                    case "Gain":
                        // Gain change
                        _audioLinkController.gainSlider.value = floatValue * 2;
                        break;

                    case "Bass":
                        // Bass change
                        if (VRC.SDKBase.Utilities.IsValid(_audioLinkController.bassSlider)) _audioLinkController.bassSlider.value = floatValue * 2;
                        break;

                    case "Treble":
                        // Treble change
                        if (VRC.SDKBase.Utilities.IsValid(_audioLinkController.trebleSlider)) _audioLinkController.trebleSlider.value = floatValue * 2;
                        break;

                    case "Length":
                        // Length change
                        _audioLinkController.fadeLengthSlider.value = floatValue;
                        break;

                    case "Falloff":
                        // Falloff change
                        _audioLinkController.fadeExpFalloffSlider.value = floatValue;
                        break;

                    case "BandSelectHue":
                        // Selected Hue change
                        if (_selectedband == 0)
                        {
                            // Gain change instead
                            _audioLinkController.gainSlider.value = floatValue * 2;
                        }
                        else _themeColorController.sliderHue.value = floatValue;
                        break;

                    case "BandSelectSaturation":
                        // Selected Saturation change
                        if (_selectedband == 0)
                        {
                            // Bass change instead
                            if (VRC.SDKBase.Utilities.IsValid(_audioLinkController.bassSlider)) _audioLinkController.bassSlider.value = floatValue * 2;
                        }
                        else _themeColorController.sliderSaturation.value = floatValue;
                        break;

                    case "BandSelectValue":
                        // Selected Value change
                        if (_selectedband == 0)
                        {
                            // Treble change instead
                            if (VRC.SDKBase.Utilities.IsValid(_audioLinkController.trebleSlider)) _audioLinkController.trebleSlider.value = floatValue * 2;
                        }
                        else _themeColorController.sliderValue.value = floatValue;
                        break;

                    case "BandSelectThreshold":
                        // Selected Threshold change
                        switch (_selectedband)
                        {
                            case 1:
                                _audioLinkController.threshold0Slider.value = floatValue;
                                break;

                            case 2:
                                _audioLinkController.threshold1Slider.value = floatValue;
                                break;

                            case 3:
                                _audioLinkController.threshold2Slider.value = floatValue;
                                break;

                            case 4:
                                _audioLinkController.threshold3Slider.value = floatValue;
                                break;

                            case 0:
                                // Length change instead
                                _audioLinkController.fadeLengthSlider.value = floatValue;
                                break;
                        }
                        break;

                    case "BandSelectStart":
                        // Selected Start change
                        switch (_selectedband)
                        {
                            case 1:
                                _audioLinkController.x0Slider.value = cutoff;
                                break;

                            case 2:
                                _audioLinkController.x1Slider.value = cutoff + .25f;
                                break;

                            case 3:
                                _audioLinkController.x2Slider.value = cutoff + .5f;
                                break;

                            case 4:
                                _audioLinkController.x3Slider.value = cutoff + .75f;
                                break;

                            case 0:
                                // Falloff change instead
                                _audioLinkController.fadeExpFalloffSlider.value = floatValue;
                                break;
                        }
                        break;

                    case "vBandSelect": // Software Band Select
                        if (value > 4) value = 4;
                        _selectedband = value;
                        break;

                    default:
                        // No-op
                        break;

                }

            }

        }

        public override void MidiNoteOn(int channel, int key, int value)
        {

            MidiNoteChange(channel, key, value, true);

        }

        public override void MidiNoteOff(int channel, int key, int value)
        {

            MidiNoteChange(channel, key, value, false);

        }

    }

}
#endif