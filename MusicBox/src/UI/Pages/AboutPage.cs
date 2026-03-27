using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;
using MusicBox.Utils;
using OnixRuntime.Api.Rendering;

namespace MusicBox.UI.Pages;

public class AboutPage(MidiController controller) : MidiPage(controller) {
	public override void Render() {
		RenderPageHeader("About");
		
		ImGui.Spacing();
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.Text("MusicBox");
		
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.TextDisabled("A Minecraft MIDI Player.");

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		
		RenderSectionHeader("Credits");
		
		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.Text("MusicBox Plugin -");
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.5f, 0f, 1f));
		ImGui.Text("OrangeCash090");
		ImGui.PopStyleColor();

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.Text("Onix Plugin API -");
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0.5f, 1f, 1f));
		ImGui.Text("Onix86");
		ImGui.PopStyleColor();

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.Text("ImGui -");
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 1f, 1f));
		ImGui.Text("ocornut");
		ImGui.PopStyleColor();

		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();

		ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
		ImGui.Text("Powered By");
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0.8f, 1f, 1f));
		ImGui.Text("Onix");
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0.6f, 1f, 1f));
		ImGui.Text("Client");
		ImGui.PopStyleColor();
		
		ImGui.Image(TexturePath.Assets("images/render.png").ToImGuiTexture(), new Vector2(256, 256));
	}
}
