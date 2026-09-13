package org.kenjinx.android.viewmodels

import android.content.Context
import android.net.Uri
import android.util.Xml
import androidx.preference.PreferenceManager
import org.kenjinx.android.RegionCode
import org.kenjinx.android.SystemLanguage
import org.xmlpull.v1.XmlPullParser

object KenjiSettingsImport {
    private val booleans = setOf(
        "disableThreadedRendering", "enableDebugLogs", "enableDocked", "enableErrorLogs",
        "enableFsAccessLogs", "enableFsIntegrityChecks", "enableGraphicsLogs", "enableGuestLogs",
        "enableInfoLogs", "enableJitCacheEviction", "enableLowPowerPptc", "enableMacroHLE",
        "enableMotion", "enablePerformanceMode", "enablePptc", "enableShaderCache", "enableStubLogs",
        "enableTextureRecompression", "enableTraceLogs", "enableWarningLogs", "ignoreMissingServices",
        "isGrid", "stretchToFullscreen", "useNce", "useSwitchLayout", "useVirtualController"
    )
    private val integers = mapOf(
        "memoryManagerMode" to 0..2, "memoryConfiguration" to 0..4, "vSyncMode" to 0..1,
        "fsGlobalAccessLogMode" to 0..3, "orientationPreference" to 0..14,
        "overlayMenuPosition" to QuickSettings.OverlayMenuPosition.entries.indices,
        "virtualControllerPreset" to QuickSettings.VirtualControllerPreset.entries.indices
    )
    private val floats = setOf("controllerScale", "controllerStickSensitivity", "maxAnisotropy", "overlayMenuOpacity", "resScale")

    fun importSettings(context: Context, uri: Uri): Int {
        val values = mutableMapOf<String, Any>()
        context.contentResolver.openInputStream(uri)?.use { input ->
            val parser = Xml.newPullParser()
            parser.setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES, false)
            parser.setInput(input, null)
            var rootSeen = false
            var events = 0
            while (parser.nextToken() != XmlPullParser.END_DOCUMENT) {
                require(++events <= 10000) { "Settings file is too large" }
                require(parser.eventType != XmlPullParser.DOCDECL) { "Document declarations are not supported" }
                if (parser.eventType != XmlPullParser.START_TAG) continue
                if (!rootSeen) {
                    require(parser.name == "map") { "Select Kenji-NX's Android preferences XML file" }
                    rootSeen = true
                    continue
                }
                require(parser.depth == 2) { "Unexpected settings structure" }
                val key = parser.getAttributeValue(null, "name") ?: error("Setting has no name")
                val raw = parser.getAttributeValue(null, "value")
                when {
                    key in booleans -> {
                        require(parser.name == "boolean") { "Invalid type for $key" }
                        values[key] = raw?.toBooleanStrictOrNull() ?: error("Invalid $key")
                    }
                    key in integers -> {
                        require(parser.name == "int") { "Invalid type for $key" }
                        val value = raw?.toIntOrNull() ?: error("Invalid $key")
                        require(value in integers.getValue(key)) { "Unsupported $key value" }
                        values[key] = value
                    }
                    key in floats -> {
                        require(parser.name == "float") { "Invalid type for $key" }
                        val value = raw?.toFloatOrNull() ?: error("Invalid $key")
                        require(value.isFinite() && value >= -1f && value <= 100f) { "Invalid $key" }
                        values[key] = value
                    }
                    key == "system_language" || key == "region_code" -> {
                        require(parser.name == "string") { "Invalid type for $key" }
                        val value = parser.nextText()
                        if (key == "system_language") SystemLanguage.valueOf(value) else RegionCode.valueOf(value)
                        values[key] = value
                    }
                    else -> {
                        // File/URI permissions, driver paths and unrelated preferences are app-specific.
                        val depth = parser.depth
                        while (parser.nextToken() != XmlPullParser.END_DOCUMENT &&
                            !(parser.eventType == XmlPullParser.END_TAG && parser.depth == depth)) {
                            require(++events <= 10000 && parser.eventType != XmlPullParser.DOCDECL)
                        }
                    }
                }
            }
        } ?: error("Could not open settings file")
        require(values.isNotEmpty()) { "No supported Kenji-NX settings found" }
        // Parse and validate everything before updating any preferences.
        val editor = PreferenceManager.getDefaultSharedPreferences(context).edit()
        values.forEach { (key, value) ->
            when (value) {
                is Boolean -> editor.putBoolean(key, value)
                is Int -> editor.putInt(key, value)
                is Float -> editor.putFloat(key, value)
                is String -> editor.putString(key, value)
            }
        }
        check(editor.commit()) { "Could not save imported settings" }
        return values.size
    }
}
