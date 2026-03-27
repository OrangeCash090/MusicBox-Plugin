using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MusicBox.Utils;
using OnixRuntime.Api.Maths;

namespace MusicBox.Audio;

public enum LoopType {
	Once,
	Loop,
	AutoPlay
}

public enum PlayType {
	UI,
	World,
	Commands
}

public class MidiController(MusicBoxConfig config) {
	public static readonly string[] GmProgramNames = [
		"Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honky-tonk Piano",
		"Electric Piano 1", "Electric Piano 2", "Harpsichord", "Clavi",
		"Celesta", "Glockenspiel", "Music Box", "Vibraphone",
		"Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
		"Drawbar Organ", "Percussive Organ", "Rock Organ", "Church Organ",
		"Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
		"Nylon Guitar", "Steel Guitar", "Jazz Guitar", "Clean Guitar",
		"Muted Guitar", "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics",
		"Acoustic Bass", "Finger Bass", "Pick Bass", "Fretless Bass",
		"Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2",
		"Violin", "Viola", "Cello", "Contrabass",
		"Tremolo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani",
		"String Ensemble 1", "String Ensemble 2", "Synth Strings 1", "Synth Strings 2",
		"Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit",
		"Trumpet", "Trombone", "Tuba", "Muted Trumpet",
		"French Horn", "Brass Section", "Synth Brass 1", "Synth Brass 2",
		"Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
		"Oboe", "English Horn", "Bassoon", "Clarinet",
		"Piccolo", "Flute", "Recorder", "Pan Flute",
		"Blown Bottle", "Shakuhachi", "Whistle", "Ocarina",
		"Square Lead", "Sawtooth Lead", "Calliope Lead", "Chiff Lead",
		"Charang Lead", "Voice Lead", "Fifths Lead", "Bass+Lead",
		"New Age Pad", "Warm Pad", "Polysynth Pad", "Choir Pad",
		"Bowed Pad", "Metallic Pad", "Halo Pad", "Sweep Pad",
		"Rain FX", "Soundtrack FX", "Crystal FX", "Atmosphere FX",
		"Brightness FX", "Goblins FX", "Echoes FX", "Sci-fi FX",
		"Sitar", "Banjo", "Shamisen", "Koto",
		"Kalimba", "Bagpipe", "Fiddle", "Shanai",
		"Tinkle Bell", "Agogo", "Steel Drums", "Woodblock",
		"Taiko Drum", "Melodic Tom", "Synth Drum", "Reverse Cymbal",
		"Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet",
		"Telephone Ring", "Helicopter", "Applause", "Gunshot"
	];

	private float[] _channelVolumes = Enumerable.Repeat(1f, 16).ToArray();
	private bool[] _channelMuted = new bool[16];
	private int[] _channelProgram = new int[16];

	private float[] _channelLoudnesses = new float[16];
	private float[] _channelLoudnessDecayRates = new float[16];
	private float[] _channelLoudnessSmoothed = new float[16];

	private readonly Dictionary<(int channel, long tick), float> _noteDurationsMs = new();

	private int[] _activeChannels = [];
	private float _masterVolume = 1f;
	private List<TimedEvent> _events = [];
	private TempoMap? _tempoMap;

	public readonly SoundSystem Sound = new();

	public bool Playing;
	public bool Ended;
	public float TimePosition;

	public string SongDirectory => config.SongDirectory.Text;

	public MidiFile? CurrentSong;
	public string CurrentSongName = "None";

	public LoopType LoopMode = LoopType.Once;
	public PlayType PlayMode = PlayType.UI;

	public BlockPos PlayPosition = BlockPos.Zero;
	public bool FollowPlayer = false;

	public int[] Channels { get; } = new int[16];

	public void SetSongDirectory(string dir) {
		config.SongDirectory.Text = dir;
	}

	public string[] GetAllSongs() {
		if (string.IsNullOrWhiteSpace(SongDirectory) || !Directory.Exists(SongDirectory))
			return [];

		return Directory
			.EnumerateFiles(SongDirectory, "*.mid", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(SongDirectory, path))
			.ToArray();
	}

	public string GetDuration() {
		if (CurrentSong == null) return "0:00";
		MetricTimeSpan d = (MetricTimeSpan)CurrentSong.GetDuration(TimeSpanType.Metric);
		return d.Seconds < 10 ? $"{d.Minutes}:0{d.Seconds}" : $"{d.Minutes}:{d.Seconds}";
	}

	public float GetDurationSeconds() {
		if (CurrentSong == null) return 0f;
		MetricTimeSpan d = (MetricTimeSpan)CurrentSong.GetDuration(TimeSpanType.Metric);
		return (float)d.TotalSeconds;
	}

	public int GetTempo() {
		if (CurrentSong == null) return 0;
		return (int)_tempoMap!.GetTempoAtTime(new MetricTimeSpan(0, 0, 0)).BeatsPerMinute;
	}

	public string GetTimeSignature() {
		if (CurrentSong == null) return "4/4";
		TimeSignature time = _tempoMap!.GetTimeSignatureAtTime(new MetricTimeSpan(0, 0, 0));

		return $"{time.Numerator}/{time.Denominator}";
	}

	public int GetTrackCount() => _activeChannels.Length;
	public int GetNoteCount() => _noteDurationsMs.Keys.Count;

	public int GetChannelForStrip(int strip) =>
		(uint)strip < (uint)_activeChannels.Length ? _activeChannels[strip] : strip;

	private void RefreshActiveChannels() {
		_activeChannels = CurrentSong!
			.GetTrackChunks()
			.SelectMany(chunk => chunk.Events.OfType<NoteOnEvent>())
			.Where(n => n.Velocity > 0)
			.Select(n => (int)n.Channel)
			.Distinct()
			.OrderBy(ch => ch == 9 ? int.MaxValue : ch)
			.ToArray();
	}

	public string GetTrackInstrument(int strip) {
		int channel = GetChannelForStrip(strip);
		if (channel == 9) return "PC";

		int program = _channelProgram[channel] >= 0 ? _channelProgram[channel] : Channels[channel];
		return program.ToString();
	}

	public void SetChannelProgram(int strip, int program) {
		int channel = GetChannelForStrip(strip);
		if (channel == 9) return;
		_channelProgram[channel] = Math.Clamp(program, 0, 127);
	}

	public void SetMasterVolume(float volume) => _masterVolume = Math.Clamp(volume, 0f, 1f);

	public void SetChannelVolume(int strip, float volume) {
		int channel = GetChannelForStrip(strip);
		if ((uint)channel >= 16) return;
		_channelVolumes[channel] = Math.Clamp(volume, 0f, 1f);
	}

	public void SetChannelMuted(int strip, bool muted) {
		int channel = GetChannelForStrip(strip);

		if ((uint)channel >= 16) return;
		_channelMuted[channel] = muted;
	}

	public float GetChannelVolume(int strip) {
		int channel = GetChannelForStrip(strip);
		return (uint)channel < 16 ? _channelVolumes[channel] : 1f;
	}

	public bool GetChannelMuted(int strip) {
		int channel = GetChannelForStrip(strip);
		return (uint)channel < 16 && _channelMuted[channel];
	}

	public float GetChannelLoudness(int strip) {
		int channel = GetChannelForStrip(strip);
		if ((uint)channel >= 16) return 0f;

		float linear = Math.Clamp(_channelLoudnessSmoothed[channel], 0f, 1f);
		return MathF.Sin(linear * MathF.PI * 0.5f);
	}
	
	public void ResetChannelVolumes() {
		_channelVolumes = Enumerable.Repeat(1f, 16).ToArray();
		_channelMuted = new bool[16];
		_channelLoudnesses = new float[16];
		_channelLoudnessDecayRates = new float[16];
		_channelLoudnessSmoothed = new float[16];
	}

	public void Load(string path) {
		string fileName = path.Split("\\")[^1];
		MidiFile midi;

		try {
			midi = MidiFile.Read(
				SongDirectory + "\\" + path,
				new ReadingSettings {
					InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.SnapToLimits
				}
			);
		} catch (Exception e) {
			Console.WriteLine($"Song broken: {e.Message}");
			return;
		}

		CurrentSong = midi;
		CurrentSongName = fileName;
		TimePosition = 0f;
		_tempoMap = midi.GetTempoMap();
		_events = midi.GetTimedEvents().ToList();

		_noteDurationsMs.Clear();
		foreach (Note note in midi.GetTrackChunks().SelectMany(chunk => chunk.GetNotes())) {
			(int, long Time) key = (note.Channel, note.Time);

			if (!_noteDurationsMs.ContainsKey(key)) {
				_noteDurationsMs[key] = (float)TimeConverter
					.ConvertTo<MetricTimeSpan>(note.Length, _tempoMap)
					.TotalMilliseconds;
			}
		}

		for (int i = 0; i < Channels.Length; i++) Channels[i] = 0;
		for (int i = 0; i < _channelProgram.Length; i++) _channelProgram[i] = -1;

		RefreshActiveChannels();
	}

	public void Play() {
		if (CurrentSong == null) return;

		if (Ended) {
			TimePosition = 0f;
			Ended = false;
		}

		Playing = true;
		GameTimer.Clear();

		foreach (TimedEvent timedEvent in _events) {
			float eventTimeMs = (float)TimeConverter
				.ConvertTo<MetricTimeSpan>(timedEvent.Time, _tempoMap)
				.TotalMilliseconds;

			MidiEvent midiEvent = timedEvent.Event;
			long tick = timedEvent.Time;

			if (eventTimeMs >= TimePosition) {
				GameTimer.AddDelayedAction(
					() => HandleMidiEvent(midiEvent, tick),
					eventTimeMs - TimePosition
				);
			}
		}
	}

	public void Pause() {
		Playing = false;
		GameTimer.Clear();
	}

	public void Stop() {
		Playing = false;
		Ended = true;
		ResetChannelVolumes();
		GameTimer.Clear();
	}

	public void NextSong() {
		List<string> songs = GetAllSongs().ToList();
		int curSongIndex = songs.IndexOf(CurrentSongName);
		
		if (curSongIndex != songs.Count - 1) {
			Load(songs[curSongIndex + 1]);
			Play();
		}
	}

	public void PreviousSong() {
		List<string> songs = GetAllSongs().ToList();
		int curSongIndex = songs.IndexOf(CurrentSongName);
		
		if (curSongIndex != 0) {
			Load(songs[curSongIndex - 1]);
			Play();
		}
	}

	public void Update(float delta) {
		if (CurrentSong == null || !Playing) return;
		Sound.Update();

		float dtSeconds = delta * 0.001f;
		const float easeSpeed = 12f;
		float smoothFactor = 1f - MathF.Exp(-easeSpeed * dtSeconds);

		for (int i = 0; i < 16; i++) {
			if (_channelLoudnesses[i] > 0f)
				_channelLoudnesses[i] = MathF.Max(0f, _channelLoudnesses[i] - _channelLoudnessDecayRates[i] * delta);
			
			_channelLoudnessSmoothed[i] += (_channelLoudnesses[i] - _channelLoudnessSmoothed[i]) * smoothFactor;
		}

		if (TimePosition >= GetDurationSeconds() * 1000f) {
			switch (LoopMode) {
				case LoopType.Once: Stop(); break;
				case LoopType.Loop:
					Ended = true;
					Play();
					break;
				case LoopType.AutoPlay: NextSong(); break;
			}
		}

		TimePosition += delta;
	}

	private float Lerp(float v0, float v1, float t) {
		return v0 + t * (v1 - v0);
	}

	private void HandleMidiEvent(MidiEvent midiEvent, long tick) {
		switch (midiEvent) {
			case ProgramChangeEvent pc:
				if (_channelProgram[pc.Channel] < 0)
					Channels[pc.Channel] = pc.ProgramNumber;
				break;

			case NoteOnEvent noteOn when noteOn.Velocity > 0:
				PlayNote(noteOn, tick);
				break;
		}
	}

	private void PlayNote(NoteOnEvent noteOn, long tick) {
		int channel = noteOn.Channel;
		if (_channelMuted[channel]) return;

		float effectiveVolume = (noteOn.Velocity / 127f) * _channelVolumes[channel] * _masterVolume;
		byte scaledVelocity = (byte)Math.Clamp((int)(effectiveVolume * 127f), 0, 127);

		NoteOnEvent scaledNote = new(noteOn.NoteNumber, (SevenBitNumber)scaledVelocity) {
			Channel = noteOn.Channel
		};

		float noteBias = Lerp(0.7f, 1.0f, noteOn.NoteNumber / 127f);
		float peak = Math.Clamp(effectiveVolume * noteBias, 0f, 1f);

		float durationMs = _noteDurationsMs.TryGetValue((channel, tick), out float d) && d > 0f ? d : Lerp(80f, 800f, noteOn.Velocity / 127f);

		if (peak > _channelLoudnesses[channel]) {
			_channelLoudnesses[channel] = peak;
			_channelLoudnessDecayRates[channel] = peak / durationMs;
		}

		if (channel == 9) {
			switch (PlayMode) {
				case PlayType.UI: Sound.PlayNoteUI(scaledNote, 0, NoteType.Percussion); break;
				case PlayType.World: Sound.PlayNoteWorld(scaledNote, 0, NoteType.Percussion, PlayPosition, FollowPlayer); break;
				case PlayType.Commands: Sound.PlayNoteCommand(scaledNote, 0, NoteType.Percussion, PlayPosition, FollowPlayer); break;
			}
		} else {
			int program = _channelProgram[channel] >= 0 ? _channelProgram[channel] : Channels[channel];

			switch (PlayMode) {
				case PlayType.UI: Sound.PlayNoteUI(scaledNote, program, NoteType.Instrument); break;
				case PlayType.World: Sound.PlayNoteWorld(scaledNote, program, NoteType.Instrument, PlayPosition, FollowPlayer); break;
				case PlayType.Commands: Sound.PlayNoteCommand(scaledNote, program, NoteType.Instrument, PlayPosition, FollowPlayer); break;
			}
		}
	}
}