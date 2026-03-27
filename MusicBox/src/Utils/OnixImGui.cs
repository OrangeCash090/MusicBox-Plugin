using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OnixRuntime.Api;
using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Api.Utils;

namespace MusicBox.Utils {
	public class OnixImGui {
		private static TexturePath _whiteTexture = null!;
		internal static Dictionary<int, TexturePath> _textures = new();
		private static string _thisImguiId = Guid.NewGuid().ToString();
		private static TexturePath _fontAtlasTexture = TexturePath.Assets($"{_thisImguiId}/FontAtlas");
		private static bool _hasLoadedFontAtlasOnce = false;
		private static bool _isInitialized = false;
		private static bool _retriedUpload = false;
		private static Stopwatch _reuploadRetryTime = new();
		public static bool MouseFree = false;

		public static void InitializeImGui(Action<ImFontAtlasPtr>? loadFontsCallback = null) {
			if (loadFontsCallback is null) {
				loadFontsCallback = (fonts) => {
					string fontPath = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Packages\\MICROSOFT.MINECRAFTUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Assets\\Fonts\\Mojangles.ttf");
					if (File.Exists(fontPath)) {
						fonts.AddFontFromFileTTF(fontPath, 16.0f);
					} else {
						fonts.AddFontDefault();
					}
				};
			}

			ImGui.CreateContext();
			_whiteTexture = TexturePath.Game("textures/ui/White");
			var colors = ImGui.GetStyle().Colors;

			colors[(int)ImGuiCol.Text] = new Vector4(0.95f, 0.89f, 0.78f, 1.00f); // warm parchment
			colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.55f, 0.47f, 0.36f, 1.00f); // muted wood
			colors[(int)ImGuiCol.WindowBg] = new Vector4(0.13f, 0.09f, 0.06f, 0.94f); // very dark oak
			colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
			colors[(int)ImGuiCol.PopupBg] = new Vector4(0.16f, 0.11f, 0.07f, 0.96f); // dark oak popup
			colors[(int)ImGuiCol.Border] = new Vector4(0.42f, 0.30f, 0.18f, 0.60f); // mid wood grain
			colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
			colors[(int)ImGuiCol.FrameBg] = new Vector4(0.28f, 0.19f, 0.11f, 0.54f); // wood plank
			colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.55f, 0.38f, 0.18f, 0.40f); // lighter plank
			colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.55f, 0.38f, 0.18f, 0.67f);
			colors[(int)ImGuiCol.TitleBg] = new Vector4(0.10f, 0.07f, 0.04f, 1.00f); // darkest oak
			colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.28f, 0.18f, 0.08f, 1.00f); // rich dark brown
			colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.10f, 0.07f, 0.04f, 0.51f);
			colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.20f, 0.14f, 0.08f, 1.00f);
			colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.08f, 0.05f, 0.03f, 0.53f);
			colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.42f, 0.29f, 0.15f, 1.00f); // wood mid-tone
			colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.55f, 0.38f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.68f, 0.50f, 0.24f, 1.00f);
			colors[(int)ImGuiCol.CheckMark] = new Vector4(0.85f, 0.70f, 0.20f, 1.00f); // gold note
			colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.72f, 0.57f, 0.18f, 1.00f); // gold
			colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.85f, 0.70f, 0.20f, 1.00f);
			colors[(int)ImGuiCol.Button] = new Vector4(0.55f, 0.38f, 0.15f, 0.40f); // warm wood button
			colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.72f, 0.52f, 0.20f, 1.00f); // gold hover
			colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.85f, 0.65f, 0.18f, 1.00f); // bright gold active
			colors[(int)ImGuiCol.Header] = new Vector4(0.55f, 0.38f, 0.15f, 0.31f);
			colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.72f, 0.52f, 0.20f, 0.80f);
			colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.85f, 0.65f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.Separator] = new Vector4(0.42f, 0.30f, 0.18f, 0.50f);
			colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.72f, 0.52f, 0.20f, 0.78f);
			colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.85f, 0.65f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.72f, 0.52f, 0.20f, 0.20f);
			colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.72f, 0.52f, 0.20f, 0.67f);
			colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.85f, 0.65f, 0.18f, 0.95f);
			colors[(int)ImGuiCol.Tab] = new Vector4(0.22f, 0.15f, 0.08f, 0.86f); // dark oak tab
			colors[(int)ImGuiCol.TabHovered] = new Vector4(0.72f, 0.52f, 0.20f, 0.80f);
			colors[(int)ImGuiCol.PlotLines] = new Vector4(0.72f, 0.57f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.95f, 0.78f, 0.30f, 1.00f);
			colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.72f, 0.52f, 0.12f, 1.00f);
			colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.90f, 0.70f, 0.20f, 1.00f);
			colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.20f, 0.14f, 0.08f, 1.00f);
			colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.42f, 0.30f, 0.18f, 1.00f);
			colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.30f, 0.21f, 0.12f, 1.00f);
			colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
			colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.85f, 0.65f, 0.18f, 0.06f); // subtle gold stripe
			colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.72f, 0.52f, 0.20f, 0.35f);
			colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.95f, 0.78f, 0.20f, 0.90f);
			colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(0.95f, 0.89f, 0.78f, 0.70f);
			colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.13f, 0.09f, 0.06f, 0.20f);
			colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.13f, 0.09f, 0.06f, 0.35f);

			ImGui.GetStyle().WindowRounding = 0f;
			ImGui.GetStyle().FrameRounding = 0f;

			var io = ImGui.GetIO();
			io.MouseDrawCursor = false;
			io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

			var fonts = io.Fonts;
			loadFontsCallback(fonts);

			_isInitialized = true;
		}

		public static void ListenToEvents() {
			Onix.Events.Input.InputHud += OnInput;
			Onix.Events.Input.RawTextChar += OnRawTextChar;
			Onix.Events.Input.InputReset += OnInputReset;
		}

		public static void StopListeningToEvents() {
			Onix.Events.Input.InputHud -= OnInput;
			Onix.Events.Input.RawTextChar -= OnRawTextChar;
			Onix.Events.Input.InputReset -= OnInputReset;
		}

		public static void UploadFontData(RendererGame gfx) {
			if (_hasLoadedFontAtlasOnce) {
				_retriedUpload = true;
			} else {
				_reuploadRetryTime.Start();
			}

			_hasLoadedFontAtlasOnce = true;
			var fonts = ImGui.GetIO().Fonts;
			fonts.GetTexDataAsRGBA32(out IntPtr fontAtlasPixelData, out int fontAtlasWidth, out var fontAtlasHeight);
			RawImageData fontData = RawImageData.Create(fontAtlasWidth, fontAtlasHeight);
			Marshal.Copy(fontAtlasPixelData, fontData.Data, 0, fontData.Data.Length);

			_textures[_fontAtlasTexture.GetHashCode()] = _fontAtlasTexture;
			fonts.TexID = _fontAtlasTexture.GetHashCode();
			gfx.UploadTexture(_fontAtlasTexture, fontData);
		}

		public static void NewFrame(RendererGame gfx, float deltaTime) {
			if (!_isInitialized) return;
			var io = ImGui.GetIO();

			io.DisplayFramebufferScale = new Vector2(Onix.Gui.GuiScaleInverse, Onix.Gui.GuiScaleInverse);
			io.DisplaySize = new Vector2(gfx.Width / io.DisplayFramebufferScale.X, gfx.Height / io.DisplayFramebufferScale.Y);
			io.DeltaTime = deltaTime;
			io.MousePosPrev = io.MousePos;
			//io.KeyCtrl = Onix.Input.IsDown(_ctrlInputKey);
			//io.KeyAlt = Onix.Input.IsDown(_altInputKey);
			//io.KeyShift = Onix.Input.IsDown(_shiftInputKey);
			io.MousePos = new Vector2(Onix.Gui.RawMousePosition.X, Onix.Gui.RawMousePosition.Y);
			io.FontGlobalScale = 1f + (0.5f * Onix.Gui.GuiScale - 1);

			if (gfx.GetTextureStatus(_fontAtlasTexture) is RendererTextureStatus.Missing or RendererTextureStatus.Unloaded || !_hasLoadedFontAtlasOnce || (!_retriedUpload && _reuploadRetryTime.Elapsed > TimeSpan.FromSeconds(1))) {
				UploadFontData(gfx);
			}

			ImGui.NewFrame();
		}

		public static bool OnInput(InputKey key, bool isDown) {
			if (!_isInitialized) return false;

			if (key == MidiPlayer.ToggleUIKey && isDown) {
				MouseFree = !MouseFree;
			}

			if (MouseFree) {
				var io = ImGui.GetIO();
				if (key.IsMouse && key != InputKey.Type.Scroll) {
					int btnIndex = (int)key.ToImGuiMouse();
					io.AddMouseButtonEvent(btnIndex, isDown);
					return io.WantCaptureMouse;
				} else if (key == InputKey.Type.Scroll) {
					io.AddMouseWheelEvent(0, isDown ? -1 : 1);
					return io.WantCaptureMouse;
				} else {
					var imguiKey = key.ToImGuiKey();
					if (imguiKey != ImGuiKey.None) {
						if (key == InputKey.Type.Ctrl)
							io.AddKeyEvent(ImGuiKey.ModCtrl, isDown);
						if (key == InputKey.Type.Shift)
							io.AddKeyEvent(ImGuiKey.ModShift, isDown);
						if (key == InputKey.Type.Alt)
							io.AddKeyEvent(ImGuiKey.ModAlt, isDown);
						io.AddKeyEvent(imguiKey, isDown);
					}

					return MouseFree && isDown;
				}
			}

			return MouseFree && isDown;
		}

		public static void OnRawTextChar(int character, int scancode, bool isDown, bool isControlDown, bool isShiftDown, bool isAltDown) {
			if (!_isInitialized || isControlDown) return;
			var io = ImGui.GetIO();
			io.AddInputCharacter((uint)character);
		}

		public static void OnInputReset() {
			ImGui.GetIO().ClearInputKeys();
			ImGui.GetIO().ClearInputMouse();
		}

		private delegate void ImDrawCallback(ImDrawListPtr parent_list, ImDrawCmdPtr cmd);

		public static void EndFrameAndRender(RendererGame gfx) {
			if (!_isInitialized) return;

			ImGui.EndFrame();
			ImGui.Render();
			var drawData = ImGui.GetDrawData();
			var rootPositionScale = gfx.PushRenderOffset(drawData.DisplayPos, drawData.FramebufferScale);
			var mb = gfx.MeshBuilder;
			for (int cmdListI = 0; cmdListI < drawData.CmdListsCount; cmdListI++) {
				var cmdList = drawData.CmdLists[cmdListI];
				for (int drawCmdI = 0; drawCmdI < cmdList.CmdBuffer.Size; drawCmdI++) {
					var drawCmd = cmdList.CmdBuffer[drawCmdI];

					if (drawCmd.UserCallback != IntPtr.Zero) {
						if (drawCmd.UserCallbackData == -1) {
							gfx.SetDefaultState(false);
							rootPositionScale.Dispose();
							rootPositionScale = gfx.PushRenderOffset(drawData.DisplayPos, drawData.FramebufferScale);
						} else {
							var callback = Marshal.GetDelegateForFunctionPointer<ImDrawCallback>(drawCmd.UserCallback);
							callback(cmdList, drawCmd);
						}
					} else {
						TexturePath texturePath = _whiteTexture;
						if (_textures.TryGetValue((int)drawCmd.GetTexID(), out var tempTexturePath))
							texturePath = tempTexturePath;

						var clipRect = new Rect(drawCmd.ClipRect.X, drawCmd.ClipRect.Y, drawCmd.ClipRect.Z, drawCmd.ClipRect.W);
						if (clipRect.IsEmpty) continue;

						using (var clippingRect = gfx.PushClippingRectangle(clipRect)) {
							using (var renderSession = mb.NewSession(ColorF.White, MeshBuilderPrimitiveType.Triangle, texturePath)) {
								for (int indexI = (int)drawCmd.IdxOffset; indexI < (int)(drawCmd.IdxOffset + drawCmd.ElemCount); indexI += 3) {
									ushort index0 = cmdList.IdxBuffer[indexI + 2];
									ushort index1 = cmdList.IdxBuffer[indexI + 1];
									ushort index2 = cmdList.IdxBuffer[indexI + 0];
									var vertex0 = cmdList.VtxBuffer[index0 + (int)drawCmd.VtxOffset];
									var vertex1 = cmdList.VtxBuffer[index1 + (int)drawCmd.VtxOffset];
									var vertex2 = cmdList.VtxBuffer[index2 + (int)drawCmd.VtxOffset];
									mb.Color(vertex2.col);
									mb.Vertex(vertex2.pos.X, vertex2.pos.Y, vertex2.uv.X, vertex2.uv.Y);
									mb.Color(vertex1.col);
									mb.Vertex(vertex1.pos.X, vertex1.pos.Y, vertex1.uv.X, vertex1.uv.Y);
									mb.Color(vertex0.col);
									mb.Vertex(vertex0.pos.X, vertex0.pos.Y, vertex0.uv.X, vertex0.uv.Y);

									mb.Color(vertex0.col);
									mb.Vertex(vertex0.pos.X, vertex0.pos.Y, vertex0.uv.X, vertex0.uv.Y);
									mb.Color(vertex1.col);
									mb.Vertex(vertex1.pos.X, vertex1.pos.Y, vertex1.uv.X, vertex1.uv.Y);
									mb.Color(vertex2.col);
									mb.Vertex(vertex2.pos.X, vertex2.pos.Y, vertex2.uv.X, vertex2.uv.Y);
								}
							}
						}
					}
				}
			}

			rootPositionScale.Dispose();
		}
	}

	public static class ImGuiTexturePathExtensions {
		public static nint ToImGuiTexture(this TexturePath texture) {
			int hashCode = texture.GetHashCode();
			OnixImGui._textures[hashCode] = texture;
			return (nint)hashCode;
		}
	}


	public static class ImGuiInputKeyExtensions {
		public static ImGuiMouseButton ToImGuiMouse(this InputKey key) {
			return key switch {
				{ Value: InputKey.Type.LMB } => ImGuiMouseButton.Left,
				{ Value: InputKey.Type.RMB } => ImGuiMouseButton.Right,
				{ Value: InputKey.Type.MMB } => ImGuiMouseButton.Middle,
				{ Value: InputKey.Type.MouseButton5 } => (ImGuiMouseButton)3,
				{ Value: InputKey.Type.MouseButton6 } => (ImGuiMouseButton)4,
				_ => throw new InvalidDataException()
			};
		}

		public static ImGuiKey ToImGuiKey(this InputKey key) {
			return key switch {
				{ Value: InputKey.Type.Tab } => ImGuiKey.Tab,
				{ Value: InputKey.Type.Left } => ImGuiKey.LeftArrow,
				{ Value: InputKey.Type.Right } => ImGuiKey.RightArrow,
				{ Value: InputKey.Type.Up } => ImGuiKey.UpArrow,
				{ Value: InputKey.Type.Down } => ImGuiKey.DownArrow,
				{ Value: InputKey.Type.PageUp } => ImGuiKey.PageUp,
				{ Value: InputKey.Type.PageDown } => ImGuiKey.PageDown,
				{ Value: InputKey.Type.Home } => ImGuiKey.Home,
				{ Value: InputKey.Type.End } => ImGuiKey.End,
				{ Value: InputKey.Type.Insert } => ImGuiKey.Insert,
				{ Value: InputKey.Type.Delete } => ImGuiKey.Delete,
				{ Value: InputKey.Type.Backspace } => ImGuiKey.Backspace,
				{ Value: InputKey.Type.Space } => ImGuiKey.Space,
				{ Value: InputKey.Type.Enter } => ImGuiKey.Enter,
				{ Value: InputKey.Type.Escape } => ImGuiKey.Escape,
				{ Value: InputKey.Type.Alt } => ImGuiKey.LeftAlt,
				{ Value: InputKey.Type.LAlt } => ImGuiKey.LeftAlt,
				{ Value: InputKey.Type.RAlt } => ImGuiKey.RightAlt,
				{ Value: InputKey.Type.Ctrl } => ImGuiKey.LeftCtrl,
				{ Value: InputKey.Type.LCtrl } => ImGuiKey.LeftCtrl,
				{ Value: InputKey.Type.RCtrl } => ImGuiKey.RightCtrl,
				{ Value: InputKey.Type.Shift } => ImGuiKey.LeftShift,
				{ Value: InputKey.Type.LShift } => ImGuiKey.LeftShift,
				{ Value: InputKey.Type.RShift } => ImGuiKey.RightShift,
				{ Value: InputKey.Type.ContextMenu } => ImGuiKey.Menu,
				{ Value: InputKey.Type.A } => ImGuiKey.A,
				{ Value: InputKey.Type.B } => ImGuiKey.B,
				{ Value: InputKey.Type.C } => ImGuiKey.C,
				{ Value: InputKey.Type.D } => ImGuiKey.D,
				{ Value: InputKey.Type.E } => ImGuiKey.E,
				{ Value: InputKey.Type.F } => ImGuiKey.F,
				{ Value: InputKey.Type.G } => ImGuiKey.G,
				{ Value: InputKey.Type.H } => ImGuiKey.H,
				{ Value: InputKey.Type.I } => ImGuiKey.I,
				{ Value: InputKey.Type.J } => ImGuiKey.J,
				{ Value: InputKey.Type.K } => ImGuiKey.K,
				{ Value: InputKey.Type.L } => ImGuiKey.L,
				{ Value: InputKey.Type.M } => ImGuiKey.M,
				{ Value: InputKey.Type.N } => ImGuiKey.N,
				{ Value: InputKey.Type.O } => ImGuiKey.O,
				{ Value: InputKey.Type.P } => ImGuiKey.P,
				{ Value: InputKey.Type.Q } => ImGuiKey.Q,
				{ Value: InputKey.Type.R } => ImGuiKey.R,
				{ Value: InputKey.Type.S } => ImGuiKey.S,
				{ Value: InputKey.Type.T } => ImGuiKey.T,
				{ Value: InputKey.Type.U } => ImGuiKey.U,
				{ Value: InputKey.Type.V } => ImGuiKey.V,
				{ Value: InputKey.Type.W } => ImGuiKey.W,
				{ Value: InputKey.Type.X } => ImGuiKey.X,
				{ Value: InputKey.Type.Y } => ImGuiKey.Y,
				{ Value: InputKey.Type.Z } => ImGuiKey.Z,
				{ Value: InputKey.Type.Num0 } => ImGuiKey._0,
				{ Value: InputKey.Type.Num1 } => ImGuiKey._1,
				{ Value: InputKey.Type.Num2 } => ImGuiKey._2,
				{ Value: InputKey.Type.Num3 } => ImGuiKey._3,
				{ Value: InputKey.Type.Num4 } => ImGuiKey._4,
				{ Value: InputKey.Type.Num5 } => ImGuiKey._5,
				{ Value: InputKey.Type.Num6 } => ImGuiKey._6,
				{ Value: InputKey.Type.Num7 } => ImGuiKey._7,
				{ Value: InputKey.Type.Num8 } => ImGuiKey._8,
				{ Value: InputKey.Type.Num9 } => ImGuiKey._9,
				{ Value: InputKey.Type.F1 } => ImGuiKey.F1,
				{ Value: InputKey.Type.F2 } => ImGuiKey.F2,
				{ Value: InputKey.Type.F3 } => ImGuiKey.F3,
				{ Value: InputKey.Type.F4 } => ImGuiKey.F4,
				{ Value: InputKey.Type.F5 } => ImGuiKey.F5,
				{ Value: InputKey.Type.F6 } => ImGuiKey.F6,
				{ Value: InputKey.Type.F7 } => ImGuiKey.F7,
				{ Value: InputKey.Type.F8 } => ImGuiKey.F8,
				{ Value: InputKey.Type.F9 } => ImGuiKey.F9,
				{ Value: InputKey.Type.F10 } => ImGuiKey.F10,
				{ Value: InputKey.Type.F11 } => ImGuiKey.F11,
				{ Value: InputKey.Type.F12 } => ImGuiKey.F12,
				{ Value: InputKey.Type.F13 } => ImGuiKey.F13,
				{ Value: InputKey.Type.F14 } => ImGuiKey.F14,
				{ Value: InputKey.Type.F15 } => ImGuiKey.F15,
				{ Value: InputKey.Type.F16 } => ImGuiKey.F16,
				{ Value: InputKey.Type.F17 } => ImGuiKey.F17,
				{ Value: InputKey.Type.F18 } => ImGuiKey.F18,
				{ Value: InputKey.Type.F19 } => ImGuiKey.F19,
				{ Value: InputKey.Type.F20 } => ImGuiKey.F20,
				{ Value: InputKey.Type.F21 } => ImGuiKey.F21,
				{ Value: InputKey.Type.F22 } => ImGuiKey.F22,
				{ Value: InputKey.Type.F23 } => ImGuiKey.F23,
				{ Value: InputKey.Type.F24 } => ImGuiKey.F24,
				{ Value: InputKey.Type.Numpad0 } => ImGuiKey.Keypad0,
				{ Value: InputKey.Type.Numpad1 } => ImGuiKey.Keypad1,
				{ Value: InputKey.Type.Numpad2 } => ImGuiKey.Keypad2,
				{ Value: InputKey.Type.Numpad3 } => ImGuiKey.Keypad3,
				{ Value: InputKey.Type.Numpad4 } => ImGuiKey.Keypad4,
				{ Value: InputKey.Type.Numpad5 } => ImGuiKey.Keypad5,
				{ Value: InputKey.Type.Numpad6 } => ImGuiKey.Keypad6,
				{ Value: InputKey.Type.Numpad7 } => ImGuiKey.Keypad7,
				{ Value: InputKey.Type.Numpad8 } => ImGuiKey.Keypad8,
				{ Value: InputKey.Type.Numpad9 } => ImGuiKey.Keypad9,
				{ Value: InputKey.Type.PrintScreen } => ImGuiKey.PrintScreen,
				_ => ImGuiKey.None
			};
		}
	}
}