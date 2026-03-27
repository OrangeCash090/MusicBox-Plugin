namespace MusicBox.Utils;

public static class GameTimer {
	private struct TimedAction(Action action, float delayMs, bool isRepeating, float repeatIntervalMs, int id) {
		public readonly Action Action = action;
		public readonly bool IsRepeating = isRepeating;
		public readonly float RepeatIntervalMs = repeatIntervalMs;
		public readonly int Id = id;
		public float DelayMs = delayMs;
		public float ElapsedMs = 0f;
		public bool Cancelled = false;
	}

	private static readonly List<TimedAction> _timedActions = [];
	private static readonly object _lock = new();
	private static int _nextId = 1;
	public static float ElapsedTime;

	public static int AddDelayedAction(Action action, float delayMs) {
		lock (_lock) {
			int id = _nextId++;
			_timedActions.Add(new TimedAction(action, delayMs, false, 0f, id));
			return id;
		}
	}

	public static int AddRepeatingAction(Action action, float initialDelayMs, float repeatIntervalMs) {
		lock (_lock) {
			int id = _nextId++;
			_timedActions.Add(new TimedAction(action, initialDelayMs, true, repeatIntervalMs, id));
			return id;
		}
	}

	public static bool CancelAction(int actionId) {
		lock (_lock) {
			for (int i = 0; i < _timedActions.Count; i++) {
				if (_timedActions[i].Id != actionId) continue;
				TimedAction a = _timedActions[i];
				a.Cancelled = true;
				_timedActions[i] = a;
				return true;
			}

			return false;
		}
	}

	public static void Update(float deltaTimeMs) {
		lock (_lock) {
			ElapsedTime += deltaTimeMs;

			for (int i = _timedActions.Count - 1; i >= 0; i--) {
				TimedAction action = _timedActions[i];

				if (action.Cancelled) {
					_timedActions.RemoveAt(i);
					continue;
				}

				action.ElapsedMs += deltaTimeMs;

				if (action.ElapsedMs < action.DelayMs) {
					_timedActions[i] = action;
					continue;
				}

				try {
					action.Action.Invoke();
				} catch (Exception ex) {
					Console.WriteLine(ex);
					return;
				}

				if (action.IsRepeating) {
					action.ElapsedMs = 0f;
					action.DelayMs = action.RepeatIntervalMs;
					_timedActions[i] = action;
				} else {
					_timedActions.RemoveAt(i);
				}
			}
		}
	}

	public static void Clear() {
		lock (_lock) {
			_timedActions.Clear();
		}
	}
}