package me.mirrur.templates;

import java.util.*;

public class TemplateDirective {
	private String name;
	private TemplateDirectiveCall calls;
	private String argument;

	public interface TemplateDirectiveCall {
		String transformer(String input, String argument);
	}

	public interface TemplateDirectiveCallLite {
		String transformer(String input);
	}

	static private class TemplateDirectiveInternal {
		public String name;
		public boolean hasArg;
		public TemplateDirectiveCall calls;
	}

	private static Map<String, TemplateDirectiveInternal> directives = new Hashtable<String, TemplateDirectiveInternal>();

	public static void addDirective(String directive, TemplateDirectiveCall call) {
		TemplateDirectiveInternal dir = new TemplateDirectiveInternal();
		dir.name = directive;
		dir.hasArg = true;
		dir.calls = call;
		directives.put(directive, dir);
	}

	public static void addDirective(String directive, TemplateDirectiveCallLite call) {
		TemplateDirectiveInternal dir = new TemplateDirectiveInternal();
		dir.name = directive;
		dir.hasArg = false;
		dir.calls = (inp, arg) -> call.transformer(inp);
		directives.put(directive, dir);
	}

	public static TemplateDirective getDirective(String name) {
		TemplateDirective d = new TemplateDirective();
		d.calls = directives.get(name).calls;
		d.name = directives.get(name).name;
		return d;
	}
	
	public static boolean hasArg(String name) {
		return directives.get(name).hasArg;
	}

	public String getArgument() {
		return argument;
	}

	public void setArgument(String argument) {
		this.argument = argument;
	}

	public String call(String input) {
		return calls.transformer(input, argument);
	}

	public String getName() {
		return name;
	}

	static {
	}
}
