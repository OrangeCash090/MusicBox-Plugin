using OnixRuntime.Api;

namespace MusicBox.Utils;

public class CommandQueue {
	private readonly Queue<string> _commands = new();

	public int BatchAmount { get; set; } = 10;

	public void QueueCommand(string command) {
		_commands.Enqueue(command);
	}
	
	public void AdvanceQueue() {
		int remaining = BatchAmount;

		while (remaining-- > 0 && _commands.TryDequeue(out string? cmd)) {
			Onix.Game.ExecuteCommand(cmd);
		}
	}

	public void Clear() {
		_commands.Clear();
	}
}
