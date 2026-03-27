using System.Text.Json;
using System.Text.Json.Serialization;
using Melanchall.DryWetMidi.Core;
using MusicBox.Utils;
using OnixRuntime.Api;
using OnixRuntime.Api.Entities;
using OnixRuntime.Api.Maths;

namespace MusicBox.Audio;

public class InstrumentsJson {
	[JsonPropertyName("instruments")] 
	public List<List<JsonElement>> Instruments { get; set; } = [];
}

public class DrumsJson {
	[JsonPropertyName("drums")] 
	public List<List<JsonElement>> Drums { get; set; } = [];
}

public enum NoteType {
	Instrument,
	Percussion
}

public class SoundSystem {
	private readonly List<(string, float)> InstrumentMap = [];
	private readonly List<(string, float)> DrumMap = [];

	private readonly CommandQueue _commandQueue = new();

	public SoundSystem() {
		LoadInstruments();
		LoadDrums();
	}

	private void LoadInstruments() {
		string path = AssetHelper.AssetPath + "Data/instruments.jsonc";
		string json = File.ReadAllText(path);

		InstrumentsJson data = JsonSerializer.Deserialize<InstrumentsJson>(json, new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip
		})!;

		InstrumentMap.Clear();

		foreach (List<JsonElement> entry in data.Instruments) {
			InstrumentMap.Add(ParseEntry(entry));
		}
	}

	private void LoadDrums() {
		string path = AssetHelper.AssetPath + "Data/drums.json";
		string json = File.ReadAllText(path);

		DrumsJson data = JsonSerializer.Deserialize<DrumsJson>(json)!;

		DrumMap.Clear();

		foreach (List<JsonElement> entry in data.Drums) {
			DrumMap.Add(ParseEntry(entry));
		}
	}

	private static (string, float) ParseEntry(List<JsonElement> entry) {
		if (entry.Count < 2)
			return ("", 0);

		string sound = entry[0].ValueKind == JsonValueKind.String
			? entry[0].GetString() ?? ""
			: "";

		float pitchOffset = entry[1].ValueKind == JsonValueKind.Number
			? entry[1].GetSingle()
			: 0;

		return (sound, pitchOffset);
	}

	public void PlayNoteCommand(NoteOnEvent message, int program, NoteType noteType, BlockPos position, bool followPlayer = false) {
		int origin = message.NoteNumber - 66;
		(string sound, float offset) instrument = noteType == NoteType.Instrument ? InstrumentMap[program] : DrumMap[message.NoteNumber];

		float pitch = noteType == NoteType.Instrument ? MathF.Pow(2, (origin + instrument.offset) / 12f) : MathF.Pow(2, instrument.offset / 12f);
		float volume = message.Velocity / 128f;

		if (string.IsNullOrEmpty(instrument.sound)) return;

		if (followPlayer) {
			_commandQueue.QueueCommand(
				$"/playsound {instrument.sound} @a ~~~ {volume} {pitch}"
			);
		} else {
			_commandQueue.QueueCommand(
				$"/playsound {instrument.sound} @a {position.X} {position.Y} {position.Z} {volume} {pitch}"
			);
		}
	}

	public void PlayNoteWorld(NoteOnEvent message, int program, NoteType noteType, BlockPos position, bool followPlayer = false) {
		int origin = message.NoteNumber - 66;
		(string sound, float offset) instrument = noteType == NoteType.Instrument ? InstrumentMap[program] : DrumMap[message.NoteNumber];

		float pitch = noteType == NoteType.Instrument ? MathF.Pow(2, (origin + instrument.offset) / 12f) : MathF.Pow(2, instrument.offset / 12f);
		float volume = message.Velocity / 128f;

		if (string.IsNullOrEmpty(instrument.sound)) return;
		Onix.Game.AudioEngine.PlayInWorld(instrument.sound, !followPlayer ? position.Center : Onix.LocalPlayer!.BlockPosition.Center, volume, pitch);
	}

	public void PlayNoteUI(NoteOnEvent message, int program, NoteType noteType) {
		int origin = message.NoteNumber - 66;
		(string sound, float offset) instrument = noteType == NoteType.Instrument ? InstrumentMap[program] : DrumMap[message.NoteNumber];

		float pitch = noteType == NoteType.Instrument ? MathF.Pow(2, (origin + instrument.offset) / 12f) : MathF.Pow(2, instrument.offset / 12f);
		float volume = message.Velocity / 128f;

		if (string.IsNullOrEmpty(instrument.sound)) return;
		Onix.Game.AudioEngine.PlayUi(instrument.sound, volume, pitch);
	}

	public void Update() {
		if (Onix.LocalPlayer!.PermissionLevel == PlayerPermissionLevel.Operator) {
			_commandQueue.AdvanceQueue();
		}
	}
}