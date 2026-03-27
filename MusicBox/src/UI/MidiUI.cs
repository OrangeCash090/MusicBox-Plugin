using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;
using MusicBox.UI.Pages;
using MusicBox.Utils;
using OnixRuntime.Api.Rendering;

namespace MusicBox.UI;

public class MidiUI(MidiController controller) {
	public MidiController Controller = controller;

	private enum Page {
		Player,
		Mixer,
		Visualizer,
		Settings,
		About,
	}

	private Page _currentPage = Page.Player;
	private static readonly string[] PageLabels = ["Player", "Mixer", "Visualizer", "Settings", "About"];

	private readonly PlayerPage _playerPage = new(controller);
	private readonly MixerPage _mixerPage = new(controller);
	private readonly VisualizerPage _visualizerPage = new(controller);
	private readonly SettingsPage _settingsPage = new(controller);
	private readonly AboutPage _aboutPage = new(controller);

	public bool HideUIOnClose => _settingsPage._hideUI;
	public bool PlayWhileClosed => _settingsPage._playClosed;

	private void RenderTitleBar() {
		ImGuiStylePtr style = ImGui.GetStyle();

		ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.FirstUseEver);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.Begin("##MusicBoxCustom", ImGuiWindowFlags.NoTitleBar);
		ImGui.PopStyleVar();

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 pos = ImGui.GetWindowPos();
		float width = ImGui.GetWindowWidth();

		float titlebarH = ImGui.GetFontSize() + style.FramePadding.Y * 2f;
		bool isFocused = ImGui.IsWindowFocused();

		Vector4 titleColorVec = isFocused ? style.Colors[(int)ImGuiCol.TitleBgActive] : style.Colors[(int)ImGuiCol.TitleBg];
		uint titleColor = ImGui.ColorConvertFloat4ToU32(titleColorVec);

		drawList.AddRectFilled(pos, pos + new Vector2(width, titlebarH), titleColor);

		float iconSize = titlebarH - style.FramePadding.Y * 2f;
		float iconY = pos.Y + style.FramePadding.Y;

		drawList.AddImage(
			TexturePath.Assets("Images/icon.png").ToImGuiTexture(),
			new Vector2(pos.X + style.FramePadding.X, iconY),
			new Vector2(pos.X + style.FramePadding.X + iconSize, iconY + iconSize)
		);

		uint textColor = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);
		float textX = pos.X + style.FramePadding.X + iconSize + style.ItemSpacing.X;
		float textY = pos.Y + style.FramePadding.Y;

		drawList.AddText(new Vector2(textX, textY), textColor, "MusicBox");

		float tabX = textX + ImGui.CalcTextSize("MusicBox").X + style.ItemSpacing.X * 3f;

		for (int i = 0; i < PageLabels.Length; i++) {
			string label = PageLabels[i];
			bool isActive = _currentPage == (Page)i;

			float tabW = ImGui.CalcTextSize(label).X + style.FramePadding.X * 2f;
			Vector2 tabMin = pos with { X = tabX };
			Vector2 tabMax = new(tabX + tabW, pos.Y + titlebarH);

			ImGui.SetCursorScreenPos(tabMin);
			ImGui.InvisibleButton($"##tab_{label}", new Vector2(tabW, titlebarH));

			bool hovered = ImGui.IsItemHovered();
			if (ImGui.IsItemClicked()) _currentPage = (Page)i;

			if (isActive)
				drawList.AddRectFilled(tabMin, tabMax, ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonActive]));
			else if (hovered)
				drawList.AddRectFilled(tabMin, tabMax, ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonHovered]));

			if (isActive)
				drawList.AddLine(
					tabMin with { Y = tabMax.Y - 2 }, tabMax with { Y = tabMax.Y - 2 },
					ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonActive] with { W = 1f }), 2f
				);

			drawList.AddText(new Vector2(tabMin.X + style.FramePadding.X, tabMin.Y + style.FramePadding.Y), textColor, label);
			tabX += tabW + style.ItemSpacing.X;
		}

		ImGui.SetCursorPos(style.WindowPadding with { Y = titlebarH + style.WindowPadding.Y });
	}

	private void RenderWindow() {
		RenderTitleBar();

		switch (_currentPage) {
			case Page.Player: _playerPage.Render(); break;
			case Page.Mixer: _mixerPage.Render(); break;
			case Page.Visualizer: _visualizerPage.Render(); break;
			case Page.Settings: _settingsPage.Render(); break;
			case Page.About: _aboutPage.Render(); break;
		}

		ImGui.End();
	}

	public void Render(RendererGame gfx, float delta) {
		OnixImGui.NewFrame(gfx, delta);
		RenderWindow();
		OnixImGui.EndFrameAndRender(gfx);
	}
}