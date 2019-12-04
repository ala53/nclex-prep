package me.mirrur.templates;

import java.util.*;

public class TemplateBlock {
	private String blockName;
	private String defaultValue;
	private String postData;
	private List<TemplateDirective> directives = new ArrayList<TemplateDirective>();

	public TemplateBlock(String name) {
		blockName = name;
		postData = "";
		defaultValue = "";
	}

	public List<TemplateDirective> getDirectives() {
		return directives;
	}

	public void setDirectives(List<TemplateDirective> value) {
		directives = value;
	}
	public String getBlockName() {
		return blockName;
	}

	public String getDefaultValue() {
		return defaultValue;
	}

	public void setDefaultValue(String value) {
		defaultValue = value;
	}
	
	public String getPostData() {
		return postData;
	}
	
	public void setPostData(String value) {
		postData = value;
	}
}
