namespace Quake2Sharp.opentk;

public class InputEvent
{
	public const int KeyPress = 0;
	public const int KeyRelease = 1;
	public const int MotionNotify = 2;
	public const int ButtonPress = 3;
	public const int ButtonRelease = 4;
	public const int WheelMoved = 5;
	public readonly int Type;
	public readonly object Value;
	public string? Text;

	public InputEvent(int type, object value)
	{
		this.Type = type;
		this.Value = value;
	}
}