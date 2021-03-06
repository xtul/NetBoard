﻿using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace NetBoard.Controllers.Helpers {
	public static class AppsettingsManipulation {
        public static void AddOrUpdateAppSetting<T>(string sectionPathKey, T value) {
            try {
                var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                string json = File.ReadAllText(filePath);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);

                SetValueRecursively(sectionPathKey, jsonObj, value);

                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(filePath, output);

            } catch (Exception ex) {
                Console.WriteLine("Error writing app settings | {0}", ex.Message);
            }
        }

        private static void SetValueRecursively<T>(string sectionPathKey, dynamic jsonObj, T value) {
            // split the string at the first ':' character
            var remainingSections = sectionPathKey.Split(":", 2);

            var currentSection = remainingSections[0];
            if (remainingSections.Length > 1) {
                // continue with the procress, moving down the tree
                var nextSection = remainingSections[1];
                SetValueRecursively(nextSection, jsonObj[currentSection], value);
            } else {
                // we've got to the end of the tree, set the value
                jsonObj[currentSection] ??= new JObject();
                jsonObj[currentSection] = value;
            }
        }
    }
}
