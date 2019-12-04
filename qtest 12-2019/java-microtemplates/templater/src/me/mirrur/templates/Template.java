package me.mirrur.templates;

import java.nio.file.*;
import java.util.*;

import java.io.*;

public class Template {
	private static String basePath = Paths.get("").toAbsolutePath().toString();
	private static String defaultTemplateName = "$template";
	private static Map<String, Template> cachedTemplates = new Hashtable<String, Template>();

	public static void setDefaultTemplateName(String value) {
		defaultTemplateName = value;
	}

	public static String getBasePath() {
		return basePath;
	}

	public static void setBasePath(String basePath) {
		Template.basePath = basePath;
	}

	public static TemplateInstance getDefaultTemplate() throws TemplateException {
		return getInstance(defaultTemplateName);
	}

	public static synchronized TemplateInstance getInstance(String templateName) throws TemplateException {
		if (cachedTemplates.containsKey(templateName)) {
			Template t = cachedTemplates.get(templateName);
			if (t.lastModified != t.file.lastModified()) {
				// Reload the template
				cachedTemplates.remove(templateName);
				return getInstance(templateName);
			}
			// Otherwise, return this
			return new TemplateInstance(t);
		} else {
			File file = new File(basePath, templateName + (templateName.endsWith(".html") ? "" : ".html"));
			if (!file.exists()) {
				throw new TemplateException("Template file not found. Searched path: " + file.toString());
			}
			cachedTemplates.put(templateName, new Template(file, templateName));

			return new TemplateInstance(cachedTemplates.get(templateName));
		}
	}

	private List<TemplateBlock> blocks = new ArrayList<TemplateBlock>();
	private File file;
	private long lastModified;
	private String templateData;

	private final String delimiter = "$$";
	private final String directiveSymbol = "$!";
	private final String defaultDirective = directiveSymbol + "default";

	private Template(File file, String templateName) throws TemplateException {
		this.file = file;
		lastModified = file.lastModified();
		// Load the template
		try {
			templateData = FileHelpers.loadFileText(file);
		} catch (IOException e) {
			throw new TemplateException(e);
		}
		// Special case the first block
		// Because we need to start parsing somewhere
		TemplateBlock introBlock = new TemplateBlock("__IINTERNAL__INTRO__HIDDEN__BLOCK");
		blocks.add(introBlock);

		// And interate to get all the other blocks
		int lastIterEndPos = 0;
		int currentIterPos = 0;
		TemplateBlock lastBlock = introBlock;

		while (true) {
			lastIterEndPos = currentIterPos;
			// Find the start of the current block
			currentIterPos = templateData.indexOf(delimiter, lastIterEndPos);
			// Handle this being the the end of the document
			if (currentIterPos == -1) {
				// The "post data" is the end of the document
				lastBlock.setPostData(templateData.substring(lastIterEndPos, templateData.length()));
				break; // We've reached the end
			} else {
				// Post data is inbetween the two tags
				lastBlock.setPostData(templateData.substring(lastIterEndPos, currentIterPos));
			}

			// Iterate forward over the intro delimiter
			currentIterPos += delimiter.length();

			// Find the end of the tag
			int endOfTag = templateData.indexOf(delimiter, currentIterPos);

			if (endOfTag == -1) {
				// ERROR: The tag is unclosed
				// And print the error message
				throw new TemplateException(
						"Unclosed tag (line " + getLineNumber(currentIterPos) + ", offset "
								+ getPosInLine(currentIterPos) + ") beginning with \"" + templateData
										.substring(currentIterPos, Math.min(templateData.length(), currentIterPos + 50))
						+ "...\"");
			}

			String tagData = templateData.substring(currentIterPos, endOfTag);

			TemplateBlock currentBlock = processTagData(tagData, currentIterPos);
			blocks.add(currentBlock);
			// Update iteration position to the end of tag
			currentIterPos = endOfTag + delimiter.length();

			lastBlock = currentBlock;
		}
	}

	private int getLineNumber(int pos) {

		String beforeText = templateData.substring(0, pos);
		int line = 1;
		for (char c : beforeText.toCharArray()) {
			if (c == '\n') {
				line++;
			}
		}

		return line;
	}

	private int getPosInLine(int pos) {

		String beforeText = templateData.substring(0, pos);
		int offsetInLine = 0;
		for (char c : beforeText.toCharArray()) {
			offsetInLine++;
			if (c == '\n') {
				offsetInLine = 0;
			}
		}

		return offsetInLine;
	}

	private TemplateBlock processTagData(String tagData, int pos) throws TemplateException {
		String[] tagDataArray = tagData.split(" "); // Split along space lines
		String tagName = tagDataArray[0];
		String defaultValue = "";
		List<TemplateDirective> directives = new ArrayList<TemplateDirective>();

		for (int offset = 1; offset < tagDataArray.length; offset++) {
			String arg = tagDataArray[offset];
			if (arg.startsWith(directiveSymbol)) {
				if (arg.equals(defaultDirective)) {
					// Default is a special case: everything after it is the
					// default value regardless of directives included in that
					defaultValue = String.join(" ", Arrays.copyOfRange(tagDataArray, offset + 1, tagDataArray.length));
					break;
				} else {
					// Process the directive
					try {
						// Get name without "overhead"
						String dirName = arg.substring(directiveSymbol.length());
						boolean hasArg = TemplateDirective.hasArg(dirName);
						TemplateDirective dir = TemplateDirective.getDirective(dirName);
						if (hasArg) // Then the next element is its argument
						{
							offset += 1;
							if (offset >= tagDataArray.length) {
								throw new TemplateException("Directive \"" + tagName + "\" (line " + getLineNumber(pos)
										+ ", offset " + getPosInLine(pos) + ") is missing its argument.");
							}
							dir.setArgument(tagDataArray[offset]);
						}
						directives.add(dir);
					} catch (NullPointerException e) {
						throw new TemplateException("Directive \"" + tagName + "\" (line " + getLineNumber(pos)
								+ ", offset " + getPosInLine(pos) + ") is not recognized.");
					}
				}
			} else {
				// ERROR: Unknown symbol
				throw new TemplateException("Garbage data (line " + getLineNumber(pos) + ", offset " + getPosInLine(pos)
						+ "): \"" + arg + "\" is not part of default directive and is missing the directive symbol ("
						+ directiveSymbol + ").");
			}
		}

		TemplateBlock res = new TemplateBlock(tagName);
		res.setDirectives(directives);
		res.setDefaultValue(defaultValue);
		return res;
	}

	public List<TemplateBlock> getBlocks() {
		return blocks;
	}
}
