using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;
using MusicBox.Utils;
using OnixRuntime.Api;

namespace MusicBox.UI.Pages;

public class SettingsPage(MidiController controller) : MidiPage(controller) {
	private static int _loopMode;
	private static int _playMode;

	public bool _hideUI = true;
	public bool _playClosed = true;

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

		if (clicked) {
			current = value;
		}

		float boxY = pos.Y + (fontSize - fontSize) * 0.5f;
		Vector2 bMin = pos with { Y = boxY };
		Vector2 bMax = new(pos.X + fontSize, boxY + fontSize);

		uint borderCol = ImGui.ColorConvertFloat4ToU32(
			selected || hovered
				? style.Colors[(int)ImGuiCol.CheckMark]
				: style.Colors[(int)ImGuiCol.Border]
		);

		uint bgCol = ImGui.ColorConvertFloat4ToU32(
			selected
				? style.Colors[(int)ImGuiCol.FrameBgActive]
				: hovered
					? style.Colors[(int)ImGuiCol.FrameBgHovered]
					: style.Colors[(int)ImGuiCol.FrameBg]
		);

		drawList.AddRectFilled(bMin, bMax, bgCol, rounding);
		drawList.AddRect(bMin, bMax, borderCol, rounding, ImDrawFlags.None, 1.5f);

		if (selected) {
			float pad = fontSize * 0.25f;
			uint dotCol = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.CheckMark]);

			drawList.AddRectFilled(
				new Vector2(bMin.X + pad, bMin.Y + pad),
				new Vector2(bMax.X - pad, bMax.Y - pad),
				dotCol, rounding * 0.5f
			);
		}

		uint textCol = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);

		drawList.AddText(
			pos with { X = pos.X + fontSize + style.ItemInnerSpacing.X },
			textCol, label
		);

		return clicked;
	}

	public override void Render() {
		RenderPageHeader("Settings");

		RenderSectionHeader("Mode Settings");

		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.TextDisabled("Looping Mode");

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		if (RectRadioButton("Once", ref _loopMode, 0))
			Controller.LoopMode = LoopType.Once;

		ImGui.SameLine();

		if (RectRadioButton("Loop", ref _loopMode, 1))
			Controller.LoopMode = LoopType.Loop;

		ImGui.SameLine();

		if (RectRadioButton("AutoPlay", ref _loopMode, 2))
			Controller.LoopMode = LoopType.AutoPlay;

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.TextDisabled("Playing Mode");

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		if (RectRadioButton("UI", ref _playMode, 0))
			Controller.PlayMode = PlayType.UI;

		ImGui.SameLine();

		if (RectRadioButton("World", ref _playMode, 1))
			Controller.PlayMode = PlayType.World;

		ImGui.SameLine();

		if (RectRadioButton("Command", ref _playMode, 2))
			Controller.PlayMode = PlayType.Commands;

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.TextDisabled("Position:");
		ImGui.SameLine();
		ImGui.Text(!Controller.FollowPlayer ? $"({Controller.PlayPosition.X}, {Controller.PlayPosition.Y}, {Controller.PlayPosition.Z})" : "LOCKED");

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		if (ImGui.Button("Set Position")) {
			Controller.PlayPosition = Onix.LocalPlayer!.BlockPosition;
		}

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();

		RenderSectionHeader("Misc Settings");

		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.Checkbox("Hide UI On Close", ref _hideUI);

		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.Checkbox("Play While Not Focused", ref _playClosed);

		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.Checkbox("Position Is At Player", ref Controller.FollowPlayer);

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);

		ImGui.TextDisabled("Song Directory:");
		ImGui.SameLine();

		if (ImGui.Button(Controller.SongDirectory)) {
			Controller.SetSongDirectory(NativeFileDialog.PickFolder("Choose A Folder With Midi Files") ?? "");
		}
	}
}