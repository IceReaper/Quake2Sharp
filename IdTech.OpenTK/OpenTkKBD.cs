namespace Quake2Sharp.opentk;

using client;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using sys;

public class OpenTkKBD : KBD
{
	public static readonly InputListener Listener = new();
	public static GameWindow Window;

	public override void Init()
	{
	}

	public override void Update()
	{
		InputEvent @event;

		KBD.mx = 0;
		KBD.my = 0;

		while ((@event = OpenTkKBD.Listener.Next()) != null)
		{
			int key;

			switch (@event.Type)
			{
				case InputEvent.KeyPress:
				case InputEvent.KeyRelease:
					this.Do_Key_Event(OpenTkKBD.MapKey((Keys)@event.Value), @event.Type == InputEvent.KeyPress, @event.Text);

					break;

				case InputEvent.MotionNotify:
					if (IN.mouse_active)
					{
						KBD.mx += (int)((Vector2)@event.Value).X;
						KBD.my += (int)((Vector2)@event.Value).Y;
					}
					else
					{
						KBD.mx = 0;
						KBD.my = 0;
					}

					break;

				case InputEvent.ButtonPress:
				case InputEvent.ButtonRelease:
					key = OpenTkKBD.MapMouseButton((MouseButton)@event.Value);
					this.Do_Key_Event(key, @event.Type == InputEvent.ButtonPress, null);

					break;

				case InputEvent.WheelMoved:
					var dir = ((Vector2)@event.Value).Y;

					if (dir > 0)
					{
						this.Do_Key_Event(Key.K_MWHEELDOWN, true, null);
						this.Do_Key_Event(Key.K_MWHEELDOWN, false, null);
					}
					else
					{
						this.Do_Key_Event(Key.K_MWHEELUP, true, null);
						this.Do_Key_Event(Key.K_MWHEELUP, false, null);
					}

					break;
			}
		}
	}

	public override void Close()
	{
	}

	private static int MapMouseButton(MouseButton mouseButton)
	{
		return mouseButton switch
		{
			MouseButton.Button3 => Key.K_MOUSE2,
			MouseButton.Button2 => Key.K_MOUSE3,
			_ => Key.K_MOUSE1
		};
	}

	private static int MapKey(Keys keys)
	{
		var key = keys switch
		{
			Keys.Escape => Key.K_ESCAPE,

			Keys.F1 => Key.K_F1,
			Keys.F2 => Key.K_F2,
			Keys.F3 => Key.K_F3,
			Keys.F4 => Key.K_F4,
			Keys.F5 => Key.K_F5,
			Keys.F6 => Key.K_F6,
			Keys.F7 => Key.K_F7,
			Keys.F8 => Key.K_F8,
			Keys.F9 => Key.K_F9,
			Keys.F10 => Key.K_F10,
			Keys.F11 => Key.K_F11,
			Keys.F12 => Key.K_F12,

			Keys.PrintScreen => -1,
			Keys.ScrollLock => -1,
			Keys.Pause => Key.K_PAUSE,

			Keys.Tab => Key.K_TAB,
			Keys.CapsLock => -1,
			Keys.LeftShift => Key.K_SHIFT,
			Keys.LeftControl => Key.K_CTRL,
			Keys.LeftSuper => -1,
			Keys.LeftAlt => Key.K_ALT,
			Keys.RightAlt => Key.K_ALT,
			Keys.RightSuper => -1,
			Keys.Menu => -1,
			Keys.RightControl => Key.K_CTRL,
			Keys.RightShift => Key.K_SHIFT,
			Keys.Enter => Key.K_ENTER,
			Keys.Backspace => Key.K_BACKSPACE,

			Keys.Insert => Key.K_INS,
			Keys.Delete => Key.K_DEL,
			Keys.Home => Key.K_HOME,
			Keys.End => Key.K_END,
			Keys.PageUp => Key.K_PGUP,
			Keys.PageDown => Key.K_PGDN,

			Keys.Up => Key.K_UPARROW,
			Keys.Right => Key.K_RIGHTARROW,
			Keys.Down => Key.K_DOWNARROW,
			Keys.Left => Key.K_LEFTARROW,

			Keys.NumLock => -1,
			Keys.KeyPadEnter => Key.K_ENTER,

			Keys.GraveAccent => '`',
			_ => (int)keys
		};

		if (key >= 'A' && key <= 'Z')
			key = key - 'A' + 'a';

		if (key > 255)
			key = 0;

		return key;
	}

	public override void Do_Key_Event(int key, bool down, string? text)
	{
		if (key != -1)
			Key.Event(key, down, Timer.Milliseconds(), text);
	}

	public override void installGrabs()
	{
		if (OpenTkKBD.Window != null)
			OpenTkKBD.Window.CursorState = CursorState.Grabbed;
	}

	public override void uninstallGrabs()
	{
		if (OpenTkKBD.Window != null)
			OpenTkKBD.Window.CursorState = CursorState.Normal;
	}
}