using System.Numerics;
using ImGuiNET;
using MusicBox.Audio;
using MusicBox.Utils;
using OnixRuntime.Api.Rendering;

namespace MusicBox.UI.Pages;

public class PlayerPage(MidiController controller) : MidiPage(controller) {
	private int _selectedSong;
	private bool _scrubbing;

	private bool Playing => Controller.Playing;

	private const float CardHeight = 70f;
	private const float TrackThickness = 3f;
	private const float KnobRadius = 6f;
	private const float HitHeight = 16f;
	private const float ButtonSpacing = 36f;

	private const float PlaybackSectionHeight = 8f + 8f + CardHeight + 48f;

	private void SetTimePosition(float val) {
		Controller.TimePosition = val;
	}

	public override void Render() {
		ImGuiStylePtr style = ImGui.GetStyle();

		RenderPageHeader("Player");
		ImGui.Spacing();

		ImGui.SetCursorPosX(style.WindowPadding.X);
		ImGui.Text("Now Playing:");
		ImGui.SameLine();

		string currentSong = Controller.CurrentSongName;

		if (ImGui.Button($"{currentSong}"))
			ImGui.OpenPopup("##song_picker");

		if (ImGui.BeginPopup("##song_picker")) {
			string[] songs = Controller.GetAllSongs();

			ImGui.TextDisabled("Select a song");
			ImGui.Separator();
			ImGui.Spacing();

			for (int i = 0; i < songs.Length; i++) {
				bool selected = i == _selectedSong;

				if (ImGui.Selectable(songs[i], selected)) {
					_selectedSong = i;
					Controller.Load(songs[_selectedSong]);
					Controller.Play();
				}

				if (selected)
					ImGui.SetItemDefaultFocus();
			}

			ImGui.EndPopup();
		}

		ImGui.Spacing();
		ImGui.Spacing();

		RenderSectionHeader("Song Info");
		ImGui.SetCursorPosX(style.WindowPadding.X);

		if (ImGui.BeginTable("##song_info", 2, ImGuiTableFlags.SizingFixedFit)) {
			ImGui.TableSetupColumn("##key", ImGuiTableColumnFlags.WidthFixed, 100f);
			ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

			void Row(string key, string value) {
				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				ImGui.TextDisabled(key);
				ImGui.TableSetColumnIndex(1);
				ImGui.Text(value);
			}

			Row("File", currentSong);
			Row("Duration", Controller.GetDuration());
			Row("Tempo", $"{Controller.GetTempo()} BPM");
			Row("Time Sig", $"{Controller.GetTimeSignature()}");
			Row("Tracks", $"{Controller.GetTrackCount()}");
			Row("Notes", $"{Controller.GetNoteCount()}");

			ImGui.EndTable();
		}

		float bottomY = ImGui.GetWindowHeight() - PlaybackSectionHeight - style.WindowPadding.Y;
		float safeY = MathF.Max(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight(), bottomY);
		ImGui.SetCursorPosY(safeY);

		ImGui.Spacing();
		ImGui.Spacing();

		RenderSectionHeader("Playback");
		ImGui.SetCursorPosX(style.WindowPadding.X);

		RenderPlayController();
	}

	private void RenderPlayController() {
		ImGuiStylePtr style = ImGui.GetStyle();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		float width = ImGui.GetWindowWidth();
		float padding = style.WindowPadding.X;
		float innerWidth = width - padding * 2f;

		if (innerWidth <= 0f) return;

		Vector2 screenPos = ImGui.GetCursorScreenPos() with { X = ImGui.GetWindowPos().X };

		uint cardBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TitleBg] with { W = 0.6f });
		drawList.AddRectFilled(screenPos + new Vector2(padding, 0f), screenPos + new Vector2(width - padding, CardHeight), cardBg);

		float duration = Controller.GetDurationSeconds();
		float elapsed = Math.Clamp(Controller.TimePosition / 1000f, 0f, duration);
		float progress = duration > 0f ? elapsed / duration : 0f;

		string elapsedStr = TimeSpan.FromSeconds(elapsed).ToString(@"m\:ss");
		string durationStr = TimeSpan.FromSeconds(duration).ToString(@"m\:ss");

		uint dimText = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.TextDisabled]);
		uint brightText = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.Text]);

		float trackY = screenPos.Y + CardHeight * 0.42f;
		float trackLeft = screenPos.X + padding * 2;
		float trackRight = screenPos.X + innerWidth;
		float trackWidth = trackRight - trackLeft;

		if (trackWidth <= 0f) return;

		uint trackBg = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.FrameBg]);
		uint trackFill = ImGui.ColorConvertFloat4ToU32(style.Colors[(int)ImGuiCol.ButtonActive]);

		drawList.AddLine(new Vector2(trackLeft, trackY), new Vector2(trackRight, trackY), trackBg, TrackThickness);

		float fillX = trackLeft + trackWidth * progress;
		if (fillX > trackLeft)
			drawList.AddLine(new Vector2(trackLeft, trackY), new Vector2(fillX, trackY), trackFill, TrackThickness);

		drawList.AddRectFilled(
			new Vector2(fillX, trackY) - new Vector2(KnobRadius / 2f, KnobRadius / 2f),
			new Vector2(fillX, trackY) + new Vector2(KnobRadius / 2f, KnobRadius / 2f),
			brightText
		);

		float labelY = screenPos.Y + CardHeight * 0.62f;
		float fontSize = ImGui.GetFontSize() * 0.85f;

		drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(trackLeft, labelY - 4f), dimText, elapsedStr);

		Vector2 durationSize = ImGui.CalcTextSize(durationStr);
		drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(trackRight - durationSize.X, labelY - 4f), dimText, durationStr);

		ImGui.SetCursorScreenPos(new Vector2(trackLeft, trackY - HitHeight / 2f));
		ImGui.InvisibleButton("##scrubber", new Vector2(Math.Max(1f, trackWidth), HitHeight));

		if (ImGui.IsItemActive()) {
			float mouseX = ImGui.GetIO().MousePos.X;
			float t = Math.Clamp((mouseX - trackLeft) / trackWidth, 0f, 1f);

			SetTimePosition(t * (duration * 1000f));
			_scrubbing = true;
		} else {
			if (_scrubbing) {
				Controller.Play();
				_scrubbing = false;
			}
		}

		float buttonHeight = ImGui.GetCursorPosY();

		ImGui.SetCursorPosX(Math.Max(0f, trackWidth / 2f));

		if (!Playing) {
			if (ImGui.ImageButton("PlayButton", TexturePath.Assets("Images/play.png").ToImGuiTexture(), new Vector2(16, 16)))
				Controller.Play();
		} else {
			if (ImGui.ImageButton("PauseButton", TexturePath.Assets("Images/pause.png").ToImGuiTexture(), new Vector2(16, 16)))
				Controller.Pause();
		}

		ImGui.SetCursorPos(new Vector2(Math.Max(0f, trackWidth / 2f - ButtonSpacing), buttonHeight));

		if (ImGui.ImageButton("PreviousButton", TexturePath.Assets("Images/reverse.png").ToImGuiTexture(), new Vector2(16, 16)))
			Controller.PreviousSong();

		ImGui.SetCursorPos(new Vector2(Math.Max(0f, trackWidth / 2f + ButtonSpacing), buttonHeight));

		if (ImGui.ImageButton("NextButton", TexturePath.Assets("Images/fast_forward.png").ToImGuiTexture(), new Vector2(16, 16)))
			Controller.NextSong();
	}
}