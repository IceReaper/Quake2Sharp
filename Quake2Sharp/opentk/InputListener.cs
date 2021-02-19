namespace Quake2Sharp.opentk
{
	using OpenTK.Windowing.Common;
	using System.Collections.Generic;
	using System.Linq;

	public sealed class InputListener
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
			this.queue.Add(new InputEvent(InputEvent.KeyPress, args.Key));
		}

		public void KeyUp(KeyboardKeyEventArgs args)
		{
			this.queue.Add(new InputEvent(InputEvent.KeyRelease, args.Key));
		}

		public void MouseDown(MouseButtonEventArgs args)
		{
			this.queue.Add(new InputEvent(InputEvent.ButtonPress, args.Button));
		}

		public void MouseUp(MouseButtonEventArgs args)
		{
			this.queue.Add(new InputEvent(InputEvent.ButtonRelease, args.Button));
		}

		public void MouseMove(MouseMoveEventArgs args)
		{
			this.queue.Add(new InputEvent(InputEvent.MotionNotify, args.Delta));
		}

		public void MouseWheel(MouseWheelEventArgs args)
		{
			this.queue.Add(new InputEvent(InputEvent.WheelMoved, args.Offset));
		}
	}
}
