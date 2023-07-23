using OpenTK.Windowing.Common;

namespace Quake2Sharp.opentk;

public class InputListener
{
	private readonly List<InputEvent> queue = new();

	public InputEvent Next()
	{
		var @event = this.queue.FirstOrDefault();

		if (@event != null)
			this.queue.RemoveAt(0);

		return @event;
	}

	public void KeyDown(KeyboardKeyEventArgs args)
	{
		this.queue.Add(new(InputEvent.KeyPress, args.Key));
	}

	public void KeyUp(KeyboardKeyEventArgs args)
	{
		this.queue.Add(new(InputEvent.KeyRelease, args.Key));
	}

	public void MouseDown(MouseButtonEventArgs args)
	{
		this.queue.Add(new(InputEvent.ButtonPress, args.Button));
	}

	public void MouseUp(MouseButtonEventArgs args)
	{
		this.queue.Add(new(InputEvent.ButtonRelease, args.Button));
	}

	public void MouseMove(MouseMoveEventArgs args)
	{
		this.queue.Add(new(InputEvent.MotionNotify, args.Delta));
	}

	public void MouseWheel(MouseWheelEventArgs args)
	{
		this.queue.Add(new(InputEvent.WheelMoved, args.Offset));
	}

	public void TextInput(TextInputEventArgs args)
	{
		if (this.queue.Count > 0)
			this.queue[^1].Text = args.AsString;
	}
}
