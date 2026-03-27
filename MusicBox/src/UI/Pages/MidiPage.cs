using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;

namespace MusicBox.UI.Pages;

public abstract class MidiPage(MidiController controller) {
	protected readonly MidiController Controller = controller;

	public abstract void Render();

	protected static void RenderPageHeader(string title, ImGuiCol bgColor = ImGuiCol.TitleBg) {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		Vector2 screenPos = ImGui.GetCursorScreenPos() with { X = ImGui.GetWindowPos().X };
		float width = ImGui.GetWindowWidth();
		float height = ImGui.GetFontSize() + style.FramePadding.Y * 2f;

		uint bg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)bgColor]);
		uint fg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);

		drawList.AddRectFilled(screenPos, screenPos + new Vector2(width, height), bg);
		drawList.AddText(screenPos + style.FramePadding, fg, title);

		ImGui.SetCursorScreenPos(screenPos + style.WindowPadding with { Y = height });
		ImGui.Dummy(style.ItemSpacing with { X = 0 });
	}

	protected static void RenderSectionHeader(string title, float indent = -1f) {
		ImGuiStylePtr style = ImGui.GetStyle();
		float x = indent < 0 ? style.WindowPadding.X : indent;

		ImGui.SetCursorPosX(x);
		ImGui.TextDisabled(title);
		ImGui.Separator();
		ImGui.Spacing();
	}
}
