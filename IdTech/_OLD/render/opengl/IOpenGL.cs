/*
Copyright (C) 1997-2001 Id Software, Inc.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

*/
namespace Quake2Sharp.render.opengl;

public interface IOpenGL
{
	void glActiveTextureARB(int texture);
	void glAlphaFunc(int func, float @ref);
	void glArrayElement(int index);
	void glBegin(int mode);
	void glBindTexture(int target, int texture);
	void glBlendFunc(int sfactor, int dfactor);
	void glClear(int mask);
	void glClearColor(float red, float green, float blue, float alpha);
	void glClientActiveTextureARB(int texture);
	void glColor3f(float red, float green, float blue);
	void glColor3ub(byte red, byte green, byte blue);
	void glColor4f(float red, float green, float blue, float alpha);
	void glColor4ub(byte red, byte green, byte blue, byte alpha);
	void glColorPointer(int size, bool unsigned, int stride, int[] pointer);
	void glColorPointer(int size, int stride, float[] pointer);
	void glColorTable(int target, int internalFormat, int width, int format, int type, byte[] data);
	void glCullFace(int mode);
	void glDeleteTextures(int[] textures);
	void glDepthFunc(int func);
	void glDepthMask(bool flag);
	void glDepthRange(double zNear, double zFar);
	void glDisable(int cap);
	void glDisableClientState(int cap);
	void glDrawArrays(int mode, int first, int count);
	void glDrawBuffer(int mode);
	void glDrawElements(int mode, int[] indices);
	void glEnable(int cap);
	void glEnableClientState(int cap);
	void glEnd();
	void glFinish();
	void glFlush();
	void glFrustum(double left, double right, double bottom, double top, double zNear, double zFar);
	int glGetError();
	void glGetFloat(int pname, float[] @params);
	string glGetString(int name);
	void glHint(int target, int mode);
	void glInterleavedArrays(int format, int stride, float[] pointer);
	void glLockArraysEXT(int first, int count);
	void glLoadIdentity();
	void glLoadMatrix(float[] m);
	void glMatrixMode(int mode);
	void glMultiTexCoord2f(int target, float s, float t);
	void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);
	void glPixelStorei(int pname, int param);
	void glPointParameterEXT(int pname, float[] pfParams);
	void glPointParameterfEXT(int pname, float param);
	void glPointSize(float size);
	void glPolygonMode(int face, int mode);
	void glPopMatrix();
	void glPushMatrix();
	void glReadPixels(int x, int y, int width, int height, int format, int type, byte[] pixels);
	void glRotatef(float angle, float x, float y, float z);
	void glScalef(float x, float y, float z);
	void glScissor(int x, int y, int width, int height);
	void glShadeModel(int mode);
	void glTexCoord2f(float s, float t);
	void glTexCoordPointer(int size, int stride, float[] pointer);
	void glTexEnvi(int target, int pname, int param);
	void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, byte[] pixels);
	void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int[] pixels);
	void glTexParameterf(int target, int pname, float param);
	void glTexParameteri(int target, int pname, int param);
	void glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int[] pixels);
	void glTranslatef(float x, float y, float z);
	void glUnlockArraysEXT();
	void glVertex2f(float x, float y);
	void glVertex3f(float x, float y, float z);
	void glVertexPointer(int size, int stride, float[] pointer);
	void glViewport(int x, int y, int width, int height);

	/*
	 * util extensions
	 */
	void setSwapInterval(int interval);
}
