using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Localization {
    /// <summary>
    /// code -> key -> text
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> locals;

    public enum Local {
        en, 
        ko, 
        ja, 
    }

    public enum Key {
        code, 
        language, 

        start, 
        settings, 
        exit, 
    }

    private const string BASIC_PATH_OF_LOCALIZATION = "Localization";

    private static Local currentLocal = UserSettings.LanguageCode;
    public static Local CurrentLocal {
        get => currentLocal;
        set {
            if(currentLocal != value) {
                currentLocal = value;

                UserSettings.LanguageCodeString = GetText(value, Key.code);
                OnLocalChanged?.Invoke(value);
            }
        }
    }

    public static Action<Local> OnLocalChanged = null;



    public static void LoadText() {
        TextAsset textFile = ResourceLoader.GetResource<TextAsset>(Path.Combine(BASIC_PATH_OF_LOCALIZATION, "Text"));
        locals = new Dictionary<string, Dictionary<string, string>>();

        string[] rows = textFile.text.Split("\r\n");

        string[] codes = rows[0].Split(',');
        Dictionary<string, string>[] codeDict = new Dictionary<string, string>[codes.Length - 1];
        for(int i = 0; i < codeDict.Length; i++) {
            codeDict[i] = new Dictionary<string, string>();
        }

        for(int y = 1; y < rows.Length; y++) {
            string[] row = rows[y].Split(',');
            for(int x = 1; x < row.Length; x++) {
                codeDict[x - 1].Add(row[0], row[x]);
            }
        }

        for(int i = 0; i < codeDict.Length; i++) {
            locals.Add(codes[i + 1], codeDict[i]);
        }
    }

    #region Utility
    public static string GetText(Local local, Key key) => locals[local.ToString()][key.ToString()];
    #endregion
}
