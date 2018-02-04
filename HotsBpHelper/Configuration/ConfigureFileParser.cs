﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetHelper;

namespace HotsBpHelper.ConfigurationHelper
{
    public class ConfigureFileParser
    {
        private readonly FilePath _configrationFilePath;

        public ConfigureFileParser(FilePath configurationFilePath)
        {
            _configrationFilePath = configurationFilePath;
            InitializeConfiguration();
        }

        public void InitializeConfiguration()
        {
            if (!_configrationFilePath.Exists())
                return;

            var lines = File.ReadAllLines(_configrationFilePath);
            foreach (var line in lines.Select(l => l.Trim()))
            {
                var valuePair = line.Split('=').Select(v => v.Trim()).ToList();
                if (valuePair.Count() < 2)
                    continue;

                _configurationDictionary[valuePair[0]] = valuePair[1];
            }
        }

        public string GetConfigurationValue(string key)
        {
            if (_configurationDictionary.ContainsKey(key))
                return _configurationDictionary[key].Trim();

            return string.Empty;
        }

        private readonly Dictionary<string, string> _configurationDictionary = new Dictionary<string, string>();
    }
}