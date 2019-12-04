package me.mirrur.templates;

import java.io.IOException;

public class TemplateException extends Exception {

	public TemplateException(String string) {
		super(string);
	}

	public TemplateException(IOException e) {
		super(e);
	}

	/**
	 * No idea what this is for. I'm from C#, kid...but eclipse is yelling at
	 * me, so, here goes nothing.
	 */
	private static final long serialVersionUID = -3481790514297246783L;

	
}
