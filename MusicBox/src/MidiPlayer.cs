using MusicBox.Audio;
using MusicBox.UI;
using MusicBox.Utils;
using OnixRuntime.Api;
using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Api.UI;

namespace MusicBox;

public class MidiPlayer {
	public MidiController? Controller;
	public MidiUI? UI;
	public MusicBoxConfig? Config;
	
	public static InputKey ToggleUIKey = InputKey.Type.Ctrl;

	public void Initialize(MusicBoxConfig config) {
		OnixImGui.ListenToEvents();
		OnixImGui.InitializeImGui();
		
		Onix.Events.Common.HudRenderGame += OnHudRenderGame;
		Onix.Events.Common.WorldRender += OnWorldRender;
		Onix.Events.Common.ChatMessage += OnChatMessage;

		Controller = new MidiController(config);
		UI = new MidiUI(Controller);
		Config = config;
	}

	public void Dispose() {
		OnixImGui.StopListeningToEvents();
		
		Onix.Events.Common.HudRenderGame -= OnHudRenderGame;
		Onix.Events.Common.WorldRender -= OnWorldRender;
		Onix.Events.Common.ChatMessage -= OnChatMessage;
	}

	private void OnHudRenderGame(RendererGame gfx, float delta) {
		if (UI == null || Config == null) return;

		ToggleUIKey = Config.ToggleUIKey;
		
		if (OnixImGui.MouseFree) {
			Onix.Gui.MouseGrabbed = false;
		}

		if (UI.HideUIOnClose && !OnixImGui.MouseFree) return;
		UI.Render(gfx, delta);
	}

	private void OnWorldRender(RendererWorld gfx, float delta) {
		if (Onix.LocalPlayer == null) return;
		if (UI == null) return;
		
		float deltaMS = delta * 1000f;
		
		if (!UI.PlayWhileClosed && !OnixImGui.MouseFree) return;
		
		Controller?.Update(deltaMS);
		GameTimer.Update(deltaMS);
	}

	private bool OnChatMessage(string message, string username, string xuid, ChatMessageType type) {
		return type == ChatMessageType.SystemMessage && message.Contains("Player");
	}
}