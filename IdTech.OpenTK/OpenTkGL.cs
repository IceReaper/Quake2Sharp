namespace Quake2Sharp.opentk;

using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using render.opengl;

public class OpenTkGL : IOpenGL
{
	public void glAlphaFunc(int func, float @ref)
	{
		GL.AlphaFunc((AlphaFunction)func, @ref);
	}

	public void glBegin(int mode)
	{
		GL.Begin((PrimitiveType)mode);
	}

	public void glBindTexture(int target, int texture)
	{
		GL.BindTexture((TextureTarget)target, texture);
	}

	public void glBlendFunc(int sfactor, int dfactor)
	{
		GL.BlendFunc((BlendingFactor)sfactor, (BlendingFactor)dfactor);
	}

	public void glClear(int mask)
	{
		GL.Clear((ClearBufferMask)mask);
	}

	public void glClearColor(float red, float green, float blue, float alpha)
	{
		GL.ClearColor(red, green, blue, alpha);
	}

	public void glColor3f(float red, float green, float blue)
	{
		GL.Color3(red, green, blue);
	}

	public void glColor3ub(byte red, byte green, byte blue)
	{
		GL.Color3(red, green, blue);
	}

	public void glColor4f(float red, float green, float blue, float alpha)
	{
		GL.Color4(red, green, blue, alpha);
	}

	public void glColor4ub(byte red, byte green, byte blue, byte alpha)
	{
		GL.Color4(red, green, blue, alpha);
	}

	public void glColorPointer(int size, bool unsigned, int stride, int[] pointer)
	{
		GL.ColorPointer(size, (ColorPointerType)OpenGL.GL_UNSIGNED_BYTE, stride, pointer);
	}

	public void glColorPointer(int size, int stride, float[] pointer)
	{
		GL.ColorPointer(size, (ColorPointerType)OpenGL.GL_FLOAT, stride, pointer);
	}

	public void glCullFace(int mode)
	{
		GL.CullFace((CullFaceMode)mode);
	}

	public void glDeleteTextures(int[] textures)
	{
		GL.DeleteTextures(textures.Length, textures);
	}

	public void glDepthFunc(int func)
	{
		GL.DepthFunc((DepthFunction)func);
	}

	public void glDepthMask(bool flag)
	{
		GL.DepthMask(flag);
	}

	public void glDepthRange(double zNear, double zFar)
	{
		GL.DepthRange(zNear, zFar);
	}

	public void glDisable(int cap)
	{
		GL.Disable((EnableCap)cap);
	}

	public void glDisableClientState(int cap)
	{
		GL.DisableClientState((ArrayCap)cap);
	}

	public void glDrawArrays(int mode, int first, int count)
	{
		GL.DrawArrays((PrimitiveType)mode, first, count);
	}

	public void glDrawBuffer(int mode)
	{
		GL.DrawBuffer((DrawBufferMode)mode);
	}

	public void glDrawElements(int mode, int[] indices)
	{
		GL.DrawElements((PrimitiveType)mode, indices.Length, (DrawElementsType)OpenGL.GL_UNSIGNED_INT, indices);
	}

	public void glEnable(int cap)
	{
		GL.Enable((EnableCap)cap);
	}

	public void glEnableClientState(int cap)
	{
		GL.EnableClientState((ArrayCap)cap);
	}

	public void glEnd()
	{
		GL.End();
	}

	public void glFinish()
	{
		GL.Finish();
	}

	public void glFlush()
	{
		GL.Flush();
	}

	public void glFrustum(double left, double right, double bottom, double top, double zNear, double zFar)
	{
		GL.Frustum(left, right, bottom, top, zNear, zFar);
	}

	public int glGetError()
	{
		return (int)GL.GetError();
	}

	public void glGetFloat(int pname, float[] @params)
	{
		GL.GetFloat((GetPName)pname, @params);
	}

	public string glGetString(int name)
	{
		return GL.GetString((StringName)name);
	}

	public void glHint(int target, int mode)
	{
		GL.Hint((HintTarget)target, (HintMode)mode);
	}

	public void glInterleavedArrays(int format, int stride, float[] pointer)
	{
		GL.InterleavedArrays((InterleavedArrayFormat)format, stride, pointer);
	}

	public void glLoadIdentity()
	{
		GL.LoadIdentity();
	}

	public void glLoadMatrix(float[] m)
	{
		GL.LoadMatrix(m);
	}

	public void glMatrixMode(int mode)
	{
		GL.MatrixMode((MatrixMode)mode);
	}

	public void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar)
	{
		GL.Ortho(left, right, bottom, top, zNear, zFar);
	}

	public void glPixelStorei(int pname, int param)
	{
		GL.PixelStore((PixelStoreParameter)pname, param);
	}

	public void glPointSize(float size)
	{
		GL.PointSize(size);
	}

	public void glPolygonMode(int face, int mode)
	{
		GL.PolygonMode((MaterialFace)face, (PolygonMode)mode);
	}

	public void glPopMatrix()
	{
		GL.PopMatrix();
	}

	public void glPushMatrix()
	{
		GL.PushMatrix();
	}

	public void glReadPixels(int x, int y, int width, int height, int format, int type, byte[] pixels)
	{
		GL.ReadPixels(x, y, width, height, (PixelFormat)format, (PixelType)type, pixels);
	}

	public void glRotatef(float angle, float x, float y, float z)
	{
		GL.Rotate(angle, x, y, z);
	}

	public void glScalef(float x, float y, float z)
	{
		GL.Scale(x, y, z);
	}

	public void glScissor(int x, int y, int width, int height)
	{
		GL.Scissor(x, y, width, height);
	}

	public void glShadeModel(int mode)
	{
		GL.ShadeModel((ShadingModel)mode);
	}

	public void glTexCoord2f(float s, float t)
	{
		GL.TexCoord2(s, t);
	}

	public void glTexCoordPointer(int size, int stride, float[] pointer)
	{
		GL.TexCoordPointer(size, (TexCoordPointerType)OpenGL.GL_FLOAT, stride, pointer);
	}

	public void glTexEnvi(int target, int pname, int param)
	{
		GL.TexEnv((TextureEnvTarget)target, (TextureEnvParameter)pname, param);
	}

	public void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, byte[] pixels)
	{
		GL.TexImage2D(
			(TextureTarget)target,
			level,
			(PixelInternalFormat)internalformat,
			width,
			height,
			border,
			(PixelFormat)format,
			(PixelType)type,
			pixels
		);
	}

	public void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int[] pixels)
	{
		GL.TexImage2D(
			(TextureTarget)target,
			level,
			(PixelInternalFormat)internalformat,
			width,
			height,
			border,
			(PixelFormat)format,
			(PixelType)type,
			pixels
		);
	}

	public void glTexParameterf(int target, int pname, float param)
	{
		GL.TexParameter((TextureTarget)target, (TextureParameterName)pname, param);
	}

	public void glTexParameteri(int target, int pname, int param)
	{
		GL.TexParameter((TextureTarget)target, (TextureParameterName)pname, param);
	}

	public void glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int[] pixels)
	{
		GL.TexSubImage2D((TextureTarget)target, level, xoffset, yoffset, width, height, (PixelFormat)format, (PixelType)type, pixels);
	}

	public void glTranslatef(float x, float y, float z)
	{
		GL.Translate(x, y, z);
	}

	public void glVertex2f(float x, float y)
	{
		GL.Vertex2(x, y);
	}

	public void glVertex3f(float x, float y, float z)
	{
		GL.Vertex3(x, y, z);
	}

	public void glVertexPointer(int size, int stride, float[] pointer)
	{
		GL.VertexPointer(size, (VertexPointerType)OpenGL.GL_FLOAT, stride, pointer);
	}

	public void glViewport(int x, int y, int width, int height)
	{
		GL.Viewport(x, y, width, height);
	}

	public void glColorTable(int target, int internalFormat, int width, int format, int type, byte[] data)
	{
		GL.ColorTable((ColorTableTarget)target, (InternalFormat)internalFormat, width, (PixelFormat)format, (PixelType)type, data);
	}

	public void glActiveTextureARB(int texture)
	{
		GL.ActiveTexture((TextureUnit)texture);
	}

	public void glClientActiveTextureARB(int texture)
	{
		GL.ClientActiveTexture((TextureUnit)texture);
	}

	public void glPointParameterEXT(int pname, float[] pfParams)
	{
		GL.PointParameter((PointParameterName)pname, pfParams);
	}

	public void glPointParameterfEXT(int pname, float param)
	{
		GL.PointParameter((PointParameterName)pname, param);
	}

	public void glLockArraysEXT(int first, int count)
	{
		throw new NotSupportedException();
	}

	public void glArrayElement(int index)
	{
		GL.ArrayElement(index);
	}

	public void glUnlockArraysEXT()
	{
		throw new NotSupportedException();
	}

	public void glMultiTexCoord2f(int target, float s, float t)
	{
		GL.MultiTexCoord2((TextureUnit)target, s, t);
	}

	public void setSwapInterval(int interval)
	{
		GLFW.SwapInterval(interval);
	}
}