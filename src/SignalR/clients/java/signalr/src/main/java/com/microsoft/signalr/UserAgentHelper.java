// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public class UserAgentHelper {

    private final static String USER_AGENT = "User-Agent";

    public static String getUserAgentName() {
        return USER_AGENT;
    }

    public static String createUserAgentString() {
        StringBuilder agentBuilder = new StringBuilder("Microsoft SignalR/");

        // Parsing version numbers
        String detailedVersion = Version.getDetailedVersion();
        agentBuilder.append(getVersion(detailedVersion));
        agentBuilder.append("; ");
        agentBuilder.append(detailedVersion);
        agentBuilder.append("; ");

        // Getting the OS name
        agentBuilder.append(findOSName(System.getProperty("os.name")));
        agentBuilder.append("; Java; ");

        // Vendor and Version
        agentBuilder.append(getJavaVersion());
        agentBuilder.append("; ");
        agentBuilder.append(getJavaVendor());

        return agentBuilder.toString();
    }

    static String getVersion(String detailedVersion) {

        // Getting the index of the second . so we can return just the major and minor version.
        int shortVersionIndex = detailedVersion.indexOf(".", detailedVersion.indexOf(".") + 1);
        return detailedVersion.substring(0, shortVersionIndex);
    }

    static String getJavaVendor() {
        return System.getProperty("java.vendor");
    }

    static String getJavaVersion() {
        return System.getProperty("java.version");
    }

    static String findOSName(String operatingSystem) {
        operatingSystem = operatingSystem.toLowerCase();
        if (operatingSystem.indexOf("win") >= 0) {
            return "Windows NT";
        } else if (operatingSystem.contains("mac")) {
            return "macOS";
        } else if (operatingSystem.contains("nix") || operatingSystem.contains("nux") || operatingSystem.contains("aix")) {
            return "Linux";
        }

        return "";
    }
}
