package me.mirrur.templates;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;

public final class FileHelpers {

	private FileHelpers() {
	}

	public static String loadFileText(File file) throws IOException {
		String data = String.join("\n", Files.readAllLines(file.toPath()));
		if (data.startsWith("﻿")) //UTF8 BOM in ASCII
			data = data.substring("﻿".length());
		
		return data;
	}
}
