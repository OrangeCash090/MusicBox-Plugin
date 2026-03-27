using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;

namespace MusicBox.UI.Pages;

public class MixerPage(MidiController controller) : MidiPage(controller) {
	private float[] _channelVolumes = [];
	private bool[] _channelMuted = [];
	private float _masterVolume = 1f;

	private const float faderHeight = 60f;
	private const float masterFaderH = 130f;
	private const float faderWidth = 6f;
	private const float knobH = 6f;
	private const float knobW = 14f;
	private const float instBtnW = 36f;
	private const float instBtnH = 36f;
	private const float muteBtnH = 20f;

	private const float stripHeight = 190f;
	private const float masterHeight = 20f + masterFaderH + 16f;

	private const float masterWidth = 52f;
	private const float stripGap = 6f;
	private const float minStripWidth = 44f;

	public override void Render() {
		RenderPageHeader("Mixer");
		ImGui.Spacing();

		if (Controller.CurrentSong == null) {
			ImGui.TextDisabled("No song loaded.");
			return;
		}

		RenderMixerGrid();
	}

	private void RenderMixerGrid() {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		float windowWidth = ImGui.GetWindowWidth();
		float padding = style.WindowPadding.X;
		float innerWidth = windowWidth - padding * 2f;

		if (innerWidth <= 0f) return;

		int totalTracks = Math.Min(Controller.GetTrackCount(), 16);
		if (totalTracks == 0) return;

		float gridWidth = innerWidth - masterWidth - stripGap;
		if (gridWidth <= minStripWidth) return;

		int stripsPerRow = totalTracks;

		while (stripsPerRow > 1) {
			float testWidth = (gridWidth - stripGap * (stripsPerRow - 1)) / stripsPerRow;

			if (testWidth >= minStripWidth)
				break;

			stripsPerRow--;
		}

		float stripWidth = (gridWidth - stripGap * (stripsPerRow - 1)) / stripsPerRow;
		int totalRows = (int)Math.Ceiling(totalTracks / (float)stripsPerRow);

		SyncChannelState(totalTracks);

		Vector2 start = ImGui.GetCursorScreenPos() with {
			X = ImGui.GetWindowPos().X + padding
		};

		for (int i = 0; i < totalTracks; i++) {
			int col = i % stripsPerRow;
			int row = i / stripsPerRow;

			float x = start.X + col * (stripWidth + stripGap);
			float y = start.Y + row * (stripHeight + stripGap);

			RenderStrip(i, x, y, stripWidth, drawList, style);
		}

		float gridHeight = totalRows * stripHeight + (totalRows - 1) * stripGap;
		float masterX = start.X + gridWidth + stripGap;
		float masterY = start.Y;

		RenderMasterVertical(masterX, masterY, masterWidth, stripHeight, drawList, style);

		float totalHeight = Math.Max(gridHeight, masterHeight);

		ImGui.SetCursorScreenPos(
			start with { Y = start.Y + totalHeight + style.ItemSpacing.Y }
		);

		RenderFooter();
	}

	private void RenderMasterVertical(float x, float y, float width, float height, ImDrawListPtr drawList, ImGuiStylePtr style) {
		uint colBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.FrameBg]);
		uint colMaster = ImGui.ColorConvertFloat4ToU32(new Vector4(0.545f, 0.361f, 0.961f, 1f));
		uint colDim = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TextDisabled]);
		uint colText = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);

		float centerX = x + width * 0.5f;

		drawList.AddRectFilled(
			new Vector2(x, y),
			new Vector2(x + width, y + height),
			ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TitleBgActive] with { W = 0.5f })
		);

		DrawCenteredText(drawList, "MST", centerX, y + 4f, 0.75f, colText);

		float faderTop = y + (height - masterFaderH) * 0.5f;
		float faderBot = faderTop + masterFaderH;

		float before = _masterVolume;

		RenderFader(
			drawList, style, "##master", centerX,
			faderTop, faderBot, masterFaderH,
			ref _masterVolume,
			colBg, colMaster, colMaster
		);

		if (Math.Abs(_masterVolume - before) > 0.01f)
			Controller.SetMasterVolume(_masterVolume);

		DrawVolumeDb(_masterVolume, centerX, faderBot + 4f, drawList, colDim);
	}

	private void SyncChannelState(int count) {
		if (_channelVolumes.Length == count) return;

		_channelVolumes = Enumerable.Range(0, count)
			.Select(i => Controller.GetChannelVolume(i))
			.ToArray();

		_channelMuted = Enumerable.Range(0, count)
			.Select(i => Controller.GetChannelMuted(i))
			.ToArray();
	}

	private void RenderStrip(int i, float x, float y, float width, ImDrawListPtr drawList, ImGuiStylePtr style) {
		uint colBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.FrameBg]);
		uint colFill = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.SliderGrab]);
		uint colDim = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TextDisabled]);
		uint colText = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);
		uint colMuteBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonActive] with { W = 0.35f });
		uint colMuteOn = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.3f, 0.3f, 0.6f));
		uint colStripBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TitleBg] with { W = 0.5f });

		bool muted = _channelMuted[i];
		float centerX = x + width * 0.5f;

		drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + stripHeight), colStripBg);

		int midiChannel = Controller.GetChannelForStrip(i);
		DrawCenteredText(drawList, $"{midiChannel + 1:D2}", centerX, y + 4f, 0.75f, colDim);

		float btnY = y + 28f;

		if (width >= instBtnW)
			RenderInstrumentPicker(i, x, btnY, width, midiChannel, colText);

		float trackTop = btnY + instBtnH + 8f;
		float trackBot = trackTop + faderHeight;

		float before = _channelVolumes[i];

		RenderFader(
			drawList, style, $"##fader_{i}", centerX,
			trackTop, trackBot, faderHeight,
			ref _channelVolumes[i],
			colBg, muted ? colBg : colFill, colDim
		);

		if (Math.Abs(_channelVolumes[i] - before) > 0.01f)
			Controller.SetChannelVolume(i, _channelVolumes[i]);

		DrawVolumeDb(_channelVolumes[i], centerX, trackBot + 4f, drawList, colDim);
		RenderMuteButton(i, centerX, trackBot + 28f, width, muted, drawList, colMuteBg, colMuteOn, colText, colDim);
	}

	private void DrawCenteredText(ImDrawListPtr drawList, string text, float centerX, float y, float scale, uint color) {
		float fontSize = ImGui.GetFontSize() * scale;
		Vector2 size = ImGui.CalcTextSize(text) * scale;

		drawList.AddText(
			ImGui.GetFont(), fontSize,
			new Vector2(centerX - size.X * 0.5f, y),
			color, text
		);
	}

	private void DrawVolumeDb(float volume, float centerX, float y, ImDrawListPtr drawList, uint color) {
		float db = volume > 0 ? 20f * MathF.Log10(volume) : float.NegativeInfinity;
		string text = float.IsNegativeInfinity(db) ? "-0" : $"{db:+0;-0}";

		DrawCenteredText(drawList, text, centerX, y, 0.75f, color);
	}

	private void RenderMuteButton(int i, float centerX, float y, float width, bool muted, ImDrawListPtr drawList, uint bg, uint on, uint text, uint dim) {
		float btnW = Math.Max(1f, Math.Min(28f, width - 8f));

		Vector2 min = new(centerX - btnW * 0.5f, y);
		Vector2 max = new(centerX + btnW * 0.5f, y + muteBtnH);

		drawList.AddRectFilled(min, max, muted ? on : bg);

		ImGui.SetCursorScreenPos(min);

		if (ImGui.InvisibleButton($"##mute_{i}", new Vector2(btnW, muteBtnH))) {
			_channelMuted[i] = !_channelMuted[i];
			Controller.SetChannelMuted(i, _channelMuted[i]);
		}

		DrawCenteredText(drawList, "M", centerX, y, 0.75f, muted ? text : dim);
	}

	private void RenderInstrumentPicker(int i, float x, float y, float width, int channel, uint textColor) {
		float btnX = x + (width - instBtnW) * 0.5f;
		string progStr = Controller.GetTrackInstrument(i);

		DrawCenteredText(
			ImGui.GetWindowDrawList(),
			progStr, btnX + instBtnW * 0.5f, y + 10f, 0.8f, textColor
		);

		ImGui.SetCursorScreenPos(new Vector2(btnX, y));

		if (ImGui.Button($"##inst_{i}", new Vector2(instBtnW, instBtnH))) {
			ImGui.OpenPopup($"##inst_picker_{i}");
		}

		if (ImGui.BeginPopup($"##inst_picker_{i}")) {
			ImGui.TextDisabled($"Ch {channel + 1} instrument");
			ImGui.Separator();

			for (int p = 0; p < MidiController.GmProgramNames.Length; p++) {
				bool selected = Controller.GetTrackInstrument(i) == p.ToString();

				if (ImGui.Selectable($"{p} - {MidiController.GmProgramNames[p]}##{p}_{i}", selected))
					Controller.SetChannelProgram(i, p);

				if (selected)
					ImGui.SetItemDefaultFocus();
			}

			ImGui.EndPopup();
		}
	}

	private void RenderFader(ImDrawListPtr drawList, ImGuiStylePtr style, string id, float centerX, float top, float bot, float height, ref float volume, uint bg, uint fill, uint border) {
		float trackX = centerX - faderWidth * 0.5f;

		drawList.AddRectFilled(new Vector2(trackX, top), new Vector2(trackX + faderWidth, bot), bg);

		float fillTop = bot - height * volume;

		if (fillTop < bot)
			drawList.AddRectFilled(new Vector2(trackX, fillTop), new Vector2(trackX + faderWidth, bot), fill);

		float knobY = Math.Clamp(fillTop - knobH * 0.5f, top - knobH * 0.5f, bot - knobH * 0.5f);

		Vector2 knobMin = new(centerX - knobW * 0.5f, knobY);
		Vector2 knobMax = new(centerX + knobW * 0.5f, knobY + knobH);

		ImGui.SetCursorScreenPos(new Vector2(centerX - knobW * 0.5f, top));
		ImGui.InvisibleButton(id, new Vector2(knobW, bot - top));

		if (ImGui.IsItemActive()) {
			float delta = -ImGui.GetIO().MouseDelta.Y / height;
			volume = Math.Clamp(volume + delta, 0f, 1f);
		}

		uint knobCol = ImGui.IsItemActive() || ImGui.IsItemHovered()
			? ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.SliderGrabActive])
			: ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.WindowBg]);

		drawList.AddRectFilled(knobMin, knobMax, knobCol);
		drawList.AddRect(knobMin, knobMax, border);
	}

	private void RenderFooter() {
		ImGui.Spacing();
		ImGui.Spacing();

		RenderSectionHeader("Info");
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
		ImGui.TextWrapped("The Mixer is a UI for controlling the properties of individual MIDI channels.\n\nYou can change the volume of each channel by moving the sliders, and also the instrument loaded for that specific channel by clicking on the number in the box above the slider.\n\nThere is also a mute button at the bottom of each slider, and the MST slider which controls the Master Volume.");
		ImGui.PopStyleColor();
	}
}