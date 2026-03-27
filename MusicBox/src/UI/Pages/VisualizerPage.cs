using System.Numerics;
using ImGuiNET;
using Melanchall.DryWetMidi.Core;
using MusicBox.Audio;
using Melanchall.DryWetMidi.Interaction;

namespace MusicBox.UI.Pages;

public class VisualizerPage(MidiController controller) : MidiPage(controller) {
	private enum VisMode {
		Simple,
		Piano
	}

	private VisMode _mode = VisMode.Simple;

	private const float BarSpacing = 4f;
	private const float MinBarHeight = 2f;

	private record struct PianoNote(float StartMs, float EndMs, int NoteNumber, int Channel);

	private readonly List<PianoNote> _pianoNotes = [];
	private string? _lastBuiltSong;

	private const float LookAheadMs = 3000f;
	private const float KeyboardHeight = 80f;
	private const float BlackKeyHeightFraction = 0.60f;
	private const float BlackKeyWidthFraction = 0.60f;
	private const float NoteInset = 2f;

	private const int NoteMin = 24;
	private const int NoteMax = 107;
	private const int NoteRange = NoteMax - NoteMin + 1;

	private readonly bool[] _isBlack = new bool[NoteRange];
	private readonly int[] _whiteIndex = new int[NoteRange];
	private readonly float[] _keyX = new float[NoteRange];

	private readonly int[] _activeChannel = new int[NoteRange];

	private static Vector4 ChannelColor(int channel, float alpha = 1f) {
		if (channel == 9)
			return new Vector4(0.65f, 0.65f, 0.65f, alpha);

		float hue = (channel * (1f / 15f) * 300f + 180f) % 360f;
		ImGui.ColorConvertHSVtoRGB(hue / 360f, 0.75f, 0.90f, out float r, out float g, out float b);
		return new Vector4(r, g, b, alpha);
	}

	private static bool RectRadioButton(string label, ref int current, int value) {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		bool selected = current == value;
		float fontSize = ImGui.GetFontSize();
		float rounding = style.FrameRounding * 0.4f;

		Vector2 labelSize = ImGui.CalcTextSize(label, true);
		float totalW = fontSize + style.ItemInnerSpacing.X + labelSize.X;

		Vector2 pos = ImGui.GetCursorScreenPos();
		ImGui.InvisibleButton(label, new Vector2(totalW, fontSize));

		bool clicked = ImGui.IsItemClicked();
		bool hovered = ImGui.IsItemHovered();
		if (clicked) current = value;

		Vector2 bMax = new(pos.X + fontSize, pos.Y + fontSize);

		uint borderCol = ImGui.ColorConvertFloat4ToU32(
			selected || hovered
				? style.Colors[(int)ImGuiCol.CheckMark]
				: style.Colors[(int)ImGuiCol.Border]
		);

		uint bgCol = ImGui.ColorConvertFloat4ToU32(
			selected ? style.Colors[(int)ImGuiCol.FrameBgActive]
			: hovered ? style.Colors[(int)ImGuiCol.FrameBgHovered]
			: style.Colors[(int)ImGuiCol.FrameBg]
		);

		drawList.AddRectFilled(pos, bMax, bgCol, rounding);
		drawList.AddRect(pos, bMax, borderCol, rounding, ImDrawFlags.None, 1.5f);

		if (selected) {
			float pad = fontSize * 0.25f;
			uint dotCol = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.CheckMark]);

			drawList.AddRectFilled(
				new Vector2(pos.X + pad, pos.Y + pad),
				new Vector2(bMax.X - pad, bMax.Y - pad),
				dotCol, rounding * 0.5f
			);
		}

		drawList.AddText(
			pos with { X = pos.X + fontSize + style.ItemInnerSpacing.X },
			ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]), label
		);

		return clicked;
	}

	private static bool IsBlackKey(int noteNumber) {
		int pitch = noteNumber % 12;
		return pitch is 1 or 3 or 6 or 8 or 10;
	}

	private static int WhiteKeyCount(int noteMin, int noteMax) {
		int count = 0;

		for (int n = noteMin; n <= noteMax; n++)
			if (!IsBlackKey(n))
				count++;

		return count;
	}

	private void RebuildPianoRoll() {
		if (Controller.CurrentSong == null) return;
		_lastBuiltSong = Controller.CurrentSongName;
		_pianoNotes.Clear();

		TempoMap? tempoMap = Controller.CurrentSong.GetTempoMap();

		foreach (Note? note in Controller.CurrentSong.GetTrackChunks().SelectMany(c => c.GetNotes())) {
			float startMs = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalMilliseconds;
			float endMs = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.EndTime, tempoMap).TotalMilliseconds;
			_pianoNotes.Add(new PianoNote(startMs, endMs, note.NoteNumber, note.Channel));
		}

		int whiteCount = 0;

		for (int n = NoteMin; n <= NoteMax; n++) {
			int i = n - NoteMin;
			bool black = IsBlackKey(n);

			_isBlack[i] = black;
			_whiteIndex[i] = black ? -1 : whiteCount;

			if (!black) whiteCount++;
		}

		for (int n = NoteMin; n <= NoteMax; n++) {
			int i = n - NoteMin;

			if (!_isBlack[i]) {
				_keyX[i] = _whiteIndex[i];
			} else {
				float leftEdge = _whiteIndex[n - 1 - NoteMin] + 1f;
				float rightEdge = _whiteIndex[n + 1 - NoteMin];

				_keyX[i] = (leftEdge + rightEdge) * 0.5f;
			}
		}
	}

	private void RefreshActiveChannels(float msCurrent) {
		_activeChannel.AsSpan().Fill(-1);

		foreach (PianoNote note in _pianoNotes) {
			if (note.NoteNumber is < NoteMin or > NoteMax) continue;
			if (msCurrent < note.StartMs || msCurrent > note.EndMs) continue;

			_activeChannel[note.NoteNumber - NoteMin] = note.Channel;
		}
	}

	private void RenderSimple(Vector2 panelMin, Vector2 panelMax, float innerWidth, float height) {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		int trackCount = Controller.GetTrackCount();
		if (trackCount == 0) return;

		float totalSpacing = BarSpacing * (trackCount - 1);
		float barWidth = (innerWidth - totalSpacing) / trackCount;
		float maxBarHeight = height - BarSpacing * 2f;

		for (int i = 0; i < trackCount; i++) {
			float loudness = Controller.GetChannelLoudness(i);
			float barHeight = MathF.Max(MinBarHeight, loudness * maxBarHeight);

			float x = panelMin.X + i * (barWidth + BarSpacing);
			float y = panelMax.Y - BarSpacing - barHeight;

			Vector4 cold = style.Colors[(int)ImGuiCol.FrameBg];
			Vector4 hot = style.Colors[(int)ImGuiCol.SliderGrabActive];
			uint colBar = ImGui.ColorConvertFloat4ToU32(Vector4.Lerp(cold, hot, loudness));

			drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + barWidth, panelMax.Y - BarSpacing), colBar);
		}
	}

	private void RenderPianoRoll(Vector2 panelMin, Vector2 panelMax, float innerWidth) {
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		if (Controller.CurrentSong == null) return;
		if (_lastBuiltSong != Controller.CurrentSongName) RebuildPianoRoll();

		int whiteCount = WhiteKeyCount(NoteMin, NoteMax);
		if (whiteCount == 0) return;

		float keyboardTop = panelMax.Y - KeyboardHeight;
		float rollHeight = keyboardTop - panelMin.Y;
		if (rollHeight <= 0f) return;

		float whiteKeyW = innerWidth / whiteCount;
		float blackKeyW = whiteKeyW * BlackKeyWidthFraction;
		float halfBlack = blackKeyW * 0.5f;

		float msCurrent = Controller.TimePosition;
		float pixPerMs = rollHeight / LookAheadMs;

		RefreshActiveChannels(msCurrent);

		uint gridCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.04f));
		uint octaveCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.12f));
		uint blackLaneBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.18f));

		drawList.PushClipRect(panelMin, panelMax with { Y = keyboardTop }, true);

		for (int n = NoteMin; n <= NoteMax; n++) {
			int i = n - NoteMin;
			float kx = _isBlack[i]
				? panelMin.X + _keyX[i] * whiteKeyW - halfBlack
				: panelMin.X + _keyX[i] * whiteKeyW;

			if (_isBlack[i]) {
				drawList.AddRectFilled(
					panelMin with { X = kx },
					new Vector2(kx + blackKeyW, keyboardTop),
					blackLaneBg
				);
			} else {
				drawList.AddLine(panelMin with { X = kx }, new Vector2(kx, keyboardTop), gridCol, 1f);
				if (n % 12 == 0)
					drawList.AddLine(panelMin with { X = kx }, new Vector2(kx, keyboardTop), octaveCol, 1.5f);
			}
		}

		foreach (PianoNote note in _pianoNotes) {
			if (note.NoteNumber is < NoteMin or > NoteMax) continue;

			float msUntilStart = note.StartMs - msCurrent;
			float msUntilEnd = note.EndMs - msCurrent;

			if (msUntilEnd < 0f) continue;
			if (msUntilStart > LookAheadMs) continue;

			float noteTop = keyboardTop - MathF.Min(msUntilEnd, LookAheadMs) * pixPerMs;
			float noteBottom = MathF.Min(keyboardTop, keyboardTop - MathF.Max(msUntilStart, 0f) * pixPerMs);

			if (noteBottom <= noteTop) continue;

			int i = note.NoteNumber - NoteMin;
			bool black = _isBlack[i];
			
			float kx = black
				? panelMin.X + _keyX[i] * whiteKeyW - halfBlack
				: panelMin.X + _keyX[i] * whiteKeyW;
			
			float keyW = black ? blackKeyW : whiteKeyW;
			float noteW = MathF.Max(2f, keyW - NoteInset * 2f);

			bool active = msUntilStart <= 0f;
			Vector4 col = ChannelColor(note.Channel);
			
			if (active) col = Vector4.Lerp(col, Vector4.One, 0.35f);

			drawList.AddRectFilled(
				new Vector2(kx + NoteInset, noteTop),
				new Vector2(kx + NoteInset + noteW, noteBottom),
				ImGui.ColorConvertFloat4ToU32(col)
			);
		}

		drawList.PopClipRect();

		uint whiteKeyCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.93f, 0.93f, 0.93f, 1f));
		uint blackKeyCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.10f, 0.10f, 1f));
		uint keyBorderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.55f));
		float blackH = KeyboardHeight * BlackKeyHeightFraction;

		drawList.PushClipRect(panelMin with { Y = keyboardTop }, panelMax, true);

		for (int n = NoteMin; n <= NoteMax; n++) {
			int i = n - NoteMin;
			if (_isBlack[i]) continue;

			float kx = panelMin.X + _keyX[i] * whiteKeyW;
			int ch = _activeChannel[i];
			
			uint fill = ch >= 0
				? ImGui.ColorConvertFloat4ToU32(ChannelColor(ch))
				: whiteKeyCol;

			drawList.AddRectFilled(new Vector2(kx + 0.5f, keyboardTop), panelMax with { X = kx + whiteKeyW - 0.5f }, fill);
			drawList.AddRect(new Vector2(kx + 0.5f, keyboardTop), panelMax with { X = kx + whiteKeyW - 0.5f }, keyBorderCol);
		}

		for (int n = NoteMin; n <= NoteMax; n++) {
			int i = n - NoteMin;
			if (!_isBlack[i]) continue;

			float kx = panelMin.X + _keyX[i] * whiteKeyW - halfBlack;
			int ch = _activeChannel[i];
			
			uint fill = ch >= 0
				? ImGui.ColorConvertFloat4ToU32(ChannelColor(ch))
				: blackKeyCol;

			drawList.AddRectFilled(new Vector2(kx, keyboardTop), new Vector2(kx + blackKeyW, keyboardTop + blackH), fill);
		}

		drawList.PopClipRect();
	}

	private void RenderVisualizer() {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		float width = ImGui.GetWindowWidth();
		float padding = style.WindowPadding.X;
		float innerWidth = width - padding * 2f;

		float cursorY = ImGui.GetCursorPosY();
		float height = ImGui.GetWindowHeight() - cursorY - style.WindowPadding.Y;

		if (innerWidth <= 0f || height <= 0f) return;

		Vector2 screenPos = ImGui.GetCursorScreenPos() with { X = ImGui.GetWindowPos().X };
		Vector2 panelMin = screenPos + new Vector2(padding, 0f);
		Vector2 panelMax = screenPos + new Vector2(width - padding, height);

		uint cardBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TitleBg] with { W = 0.6f });
		drawList.AddRectFilled(panelMin, panelMax, cardBg);

		if (_mode == VisMode.Simple)
			RenderSimple(panelMin, panelMax, innerWidth, height);
		else
			RenderPianoRoll(panelMin, panelMax, innerWidth);

		const float btnPad = 6f;
		Vector2 btnOrigin = panelMin + new Vector2(btnPad, btnPad);

		float fontSize = ImGui.GetFontSize();
		float simpleW = fontSize + style.ItemInnerSpacing.X + ImGui.CalcTextSize("Simple").X;
		float pianoW = fontSize + style.ItemInnerSpacing.X + ImGui.CalcTextSize("Piano").X;
		float pillW = simpleW + style.ItemSpacing.X + pianoW + btnPad * 2f;
		float pillH = fontSize + btnPad * 1.5f;

		uint pillBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.WindowBg] with { W = 0.80f });
		drawList.AddRectFilled(
			btnOrigin - new Vector2(btnPad * 0.5f, btnPad * 0.5f),
			btnOrigin + new Vector2(pillW, pillH),
			pillBg, style.FrameRounding
		);

		ImGui.SetCursorScreenPos(btnOrigin);

		int modeInt = (int)_mode;
		RectRadioButton("Simple", ref modeInt, (int)VisMode.Simple);
		ImGui.SameLine();
		RectRadioButton("Piano", ref modeInt, (int)VisMode.Piano);
		_mode = (VisMode)modeInt;

		ImGui.SetCursorPosY(cursorY + height);
		ImGui.Dummy(new Vector2(innerWidth, 0f));
	}

	public override void Render() {
		RenderPageHeader("Visualizer");
		ImGui.Spacing();
		RenderVisualizer();
	}
}