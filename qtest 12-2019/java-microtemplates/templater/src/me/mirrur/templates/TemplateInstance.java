package me.mirrur.templates;

import java.util.*;
//import java.lang.*;

public class TemplateInstance {
	private Map<String, Object> substitutions = new Hashtable<String, Object>();
	private Template template;
	private int statusCode;

	public TemplateInstance(Template template) {
		this.template = template;
		statusCode = 200;
	}

	public TemplateInstance inject(String key, String value) {
		substitutions.put(key, value);
		return this;
	}

	public TemplateInstance inject(String key, TemplateInstance value) {
		substitutions.put(key, value);
		return this;
	}

	public TemplateInstance statusCode(int status) {
		statusCode = status;
		return this;
	}

	public TemplateInstance notFound() {
		statusCode = 404;
		return this;
	}

	public TemplateInstance conflict() {
		statusCode = 409;
		return this;
	}

	public TemplateInstance internalError() {
		statusCode = 500;
		return this;
	}

	public TemplateInstance badRequest() {
		statusCode = 400;
		return this;
	}

	public TemplateInstance forbidden() {
		statusCode = 403;
		return this;
	}

	public TemplateInstance unauthorized() {
		statusCode = 401;
		return this;
	}
	
	public int getStatusCode() {
		return statusCode;
	}

	// Wraps the current template in the base template
	public TemplateInstance wrap() throws TemplateException {
		TemplateInstance wrapper = Template.getDefaultTemplate();
		//Copy the current template data
		wrapper.substitutions = new Hashtable<String, Object>(substitutions);
		wrapper.statusCode = statusCode;
		wrapper.inject("content", this);
		return wrapper;
	}

	public String toString() {
		StringBuilder sb = new StringBuilder();
		InjectTemplate(this, sb);
		String str = sb.toString();
		return str;
	}

	private void InjectTemplate(TemplateInstance instance, StringBuilder sb) {
		for (TemplateBlock block : instance.template.getBlocks()) {
			// First, find out what the fillin will be
			// Prioritize the subtemplate
			Object fillIn = instance.substitutions.get(block.getBlockName());
			// Then check the base template
			if (fillIn == null)
				fillIn = instance.substitutions.get(block.getBlockName());
			// Then, the default value if still none
			if (fillIn == null)
				fillIn = block.getDefaultValue();

			if (fillIn.getClass() == String.class) {
				// Process the directives
				for (TemplateDirective directive : block.getDirectives()) {
					fillIn = directive.call((String) fillIn);
				}

				sb.append(fillIn); // Inject the string data
			} else if (fillIn.getClass() == TemplateInstance.class) {
				if (block.getDirectives().size() != 0) {
					// Slow path: compile the template in isolation, convert it
					// to string, and then merge the string output into the
					// current template after processing directives
					StringBuilder sbx = new StringBuilder();
					// Do the building
					InjectTemplate((TemplateInstance) fillIn, sbx);
					fillIn = sbx.toString();
					// Apply the directives
					for (TemplateDirective directive : block.getDirectives()) {
						fillIn = directive.call((String) fillIn);
					}
					// And inject it into the actual parent
					sb.append(fillIn);
				}

				// Fast path: directly inject the template's block data
				InjectTemplate((TemplateInstance) fillIn, sb);
			}

			sb.append(block.getPostData());
		}
	}
}
