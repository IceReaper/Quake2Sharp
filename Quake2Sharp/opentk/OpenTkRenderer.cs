namespace Quake2Sharp.opentk
{
	using client.types;
	using render;
	using render.opengl;
	using render.types;
	using sys;
	using System.Drawing;

	public class OpenTkRenderer : OpenTkDriver, refexport_t, Ref
	{
		private const string DriverName = "OpenTk";
		private readonly KBD kbd = new OpenTkKBD();
		private RenderAPI impl;

		static OpenTkRenderer()
		{
			Renderer.register(new OpenTkRenderer());
		}

		private OpenTkRenderer()
		{
		}

		public bool Init(int vid_xpos, int vid_ypos)
		{
			Base.setGLDriver(this);

			if (!this.impl.R_Init(vid_xpos, vid_ypos))
				return false;

			this.activate();

			return this.impl.R_Init2();
		}

		public void Shutdown()
		{
			this.impl.R_Shutdown();
		}

		public void BeginRegistration(string map)
		{
			this.activate();
			this.impl.R_BeginRegistration(map);
		}

		public model_t RegisterModel(string name)
		{
			this.activate();

			return this.impl.R_RegisterModel(name);
		}

		public image_t RegisterSkin(string name)
		{
			this.activate();

			return this.impl.R_RegisterSkin(name);
		}

		public image_t RegisterPic(string name)
		{
			this.activate();

			return this.impl.Draw_FindPic(name);
		}

		public void SetSky(string name, float rotate, float[] axis)
		{
			this.activate();
			this.impl.R_SetSky(name, rotate, axis);
		}

		public void EndRegistration()
		{
			this.activate();
			this.impl.R_EndRegistration();
		}

		public void RenderFrame(refdef_t fd)
		{
			this.impl.R_RenderFrame(fd);
		}

		public void DrawGetPicSize(ref Size dim, string name)
		{
			this.impl.Draw_GetPicSize(ref dim, name);
		}

		public void DrawPic(int x, int y, string name)
		{
			this.impl.Draw_Pic(x, y, name);
		}

		public void DrawStretchPic(int x, int y, int w, int h, string name)
		{
			this.impl.Draw_StretchPic(x, y, w, h, name);
		}

		public void DrawChar(int x, int y, int num)
		{
			this.activate();
			this.impl.Draw_Char(x, y, num);
		}

		public void DrawTileClear(int x, int y, int w, int h, string name)
		{
			this.impl.Draw_TileClear(x, y, w, h, name);
		}

		public void DrawFill(int x, int y, int w, int h, int c)
		{
			this.impl.Draw_Fill(x, y, w, h, c);
		}

		public void DrawFadeScreen()
		{
			this.impl.Draw_FadeScreen();
		}

		public void DrawStretchRaw(int x, int y, int w, int h, int cols, int rows, byte[] data)
		{
			this.impl.Draw_StretchRaw(x, y, w, h, cols, rows, data);
		}

		public void CinematicSetPalette(byte[] palette)
		{
			this.impl.R_SetPalette(palette);
		}

		public void BeginFrame(float camera_separation)
		{
			this.impl.R_BeginFrame(camera_separation);
		}

		public void EndFrame()
		{
			this.endFrame();
		}

		public int apiVersion()
		{
			return Defines.API_VERSION;
		}

		public KBD getKeyboardHandler()
		{
			return this.kbd;
		}

		// ============================================================================
		// Ref interface
		// ============================================================================

		public string getName()
		{
			return OpenTkRenderer.DriverName;
		}

		public string toString()
		{
			return OpenTkRenderer.DriverName;
		}

		public refexport_t GetRefAPI(RenderAPI renderer)
		{
			this.impl = renderer;

			return this;
		}
	}
}
