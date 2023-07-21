using IdTech.common;

namespace Quake2Sharp.opentk;

using client;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using qcommon;
using render;
using render.opengl;
using System.ComponentModel;
using System.Drawing;

public abstract class OpenTkDriver : OpenTkGL, GLDriver
{
	private DisplayMode oldDisplayMode;
	private GameWindow window;

	public DisplayMode[] getModeList()
	{
		return Monitors.GetPrimaryMonitor().SupportedVideoModes
			.Where(videoMode => videoMode.RedBits + videoMode.GreenBits + videoMode.BlueBits == this.oldDisplayMode.BitDepth)
			.GroupBy(videoMode => $"{videoMode.Height},{videoMode.Height}")
			.Select(videoModes => videoModes.OrderBy(videoMode => videoMode.RefreshRate).Last())
			.OrderBy(videoMode => videoMode.Width)
			.ThenBy(videoMode => videoMode.Height)
			.Select(
				videoMode => new DisplayMode(
					videoMode.Width,
					videoMode.Height,
					videoMode.RefreshRate,
					videoMode.RedBits + videoMode.GreenBits + videoMode.BlueBits
				)
			)
			.ToArray();
	}

	public int setMode(Size dim, int mode, bool fullscreen)
	{
		var windowSize = new Size();

		VID.Printf(Defines.PRINT_ALL, "Initializing OpenGL display\n");
		VID.Printf(Defines.PRINT_ALL, $"...setting mode {mode}:");

		if (this.oldDisplayMode == null)
		{
			GLFW.Init();
			var videoMode = Monitors.GetPrimaryMonitor().SupportedVideoModes[0];

			this.oldDisplayMode =
				new(videoMode.Width, videoMode.Height, videoMode.RefreshRate, videoMode.RedBits + videoMode.GreenBits + videoMode.BlueBits);
		}

		if (!VID.GetModeInfo(ref windowSize, mode))
		{
			VID.Printf(Defines.PRINT_ALL, " invalid mode\n");

			return Base.rserr_invalid_mode;
		}

		VID.Printf(Defines.PRINT_ALL, $" {windowSize.Width} {windowSize.Height}{'\n'}");

		if (this.window != null)
			this.shutdown();

		if (fullscreen)
		{
			var displayMode =
				this.getModeList().FirstOrDefault(displayMode => displayMode.Width == windowSize.Width && displayMode.Height == windowSize.Height)
				?? this.oldDisplayMode;

			this.window = new(GameWindowSettings.Default, new()
			{
				Profile = ContextProfile.Compatability, Size = new(displayMode.Width, displayMode.Height), WindowState = WindowState.Fullscreen,
				Title = "Quake2Sharp"
			});

			VID.Printf(
				Defines.PRINT_ALL,
				$"...setting fullscreen {displayMode.Width}x{displayMode.Height}x{displayMode.BitDepth}@{displayMode.RefreshRate}Hz\n"
			);
		}
		else
		{
			this.window = new(GameWindowSettings.Default, new()
			{
				Profile = ContextProfile.Compatability, Size = new(windowSize.Width, windowSize.Height),
				Title = "Quake2Sharp"
			});

			VID.Printf(Defines.PRINT_ALL, $"...setting window {windowSize.Width}x{windowSize.Height}\n");
		}

		this.window.Focus();
		this.window.Closing += OpenTkDriver.QuitOnClose;

		OpenTkKBD.Window = this.window;
		this.window.KeyDown += OpenTkKBD.Listener.KeyDown;
		this.window.KeyUp += OpenTkKBD.Listener.KeyUp;
		this.window.MouseDown += OpenTkKBD.Listener.MouseDown;
		this.window.MouseUp += OpenTkKBD.Listener.MouseUp;
		this.window.MouseMove += OpenTkKBD.Listener.MouseMove;
		this.window.MouseWheel += OpenTkKBD.Listener.MouseWheel;
		this.window.TextInput += OpenTkKBD.Listener.TextInput;

		Program.UpdateLoop = this.window.Run;

		var initialized = false;

		this.window.RenderFrame += args =>
		{
			if (!initialized)
			{
				this.init(0, 0);
				initialized = true;
			}

			frame.Qcommon_Mainloop();
		};

		this.window.Resize += args =>
		{
			Base.setVid(this.window.ClientSize.X, this.window.ClientSize.Y);
			VID.NewWindow(this.window.ClientSize.X, this.window.ClientSize.Y);
		};

		Base.setVid(this.window.ClientSize.X, this.window.ClientSize.Y);
		VID.NewWindow(this.window.ClientSize.X, this.window.ClientSize.Y);

		return Base.rserr_ok;
	}

	private static void QuitOnClose(CancelEventArgs args)
	{
		Program.UpdateLoop = null;
		cmdparser.Cbuf_ExecuteText(Defines.EXEC_APPEND, "quit");
	}

	public void shutdown()
	{
		if (this.window == null)
			return;

		this.window.Closing -= OpenTkDriver.QuitOnClose;
		this.window.Dispose();
		OpenTkKBD.Window = null;
		this.window = null;
		Program.UpdateLoop = null;
	}

	public bool init(int xpos, int ypos)
	{
		if (this.window == null)
			return true;

		Base.setVid(this.window.ClientSize.X, this.window.ClientSize.Y);
		VID.NewWindow(this.window.ClientSize.X, this.window.ClientSize.Y);

		return true;
	}

	public void beginFrame(float cameraSeparation)
	{
	}

	public void endFrame()
	{
		this.glFlush();
		this.window?.SwapBuffers();
	}

	public void enableLogging(bool enable)
	{
	}

	public void logNewFrame()
	{
	}

	public void updateScreen(Action callback)
	{
		callback();
	}

	public void screenshot()
	{
	}

	protected void activate()
	{
	}

	public bool IsVSyncActive()
	{
		return window.VSync != VSyncMode.Off;
	}

	public int GetRefreshRate()
	{
		return (int)window.UpdateFrequency;
	}
}
