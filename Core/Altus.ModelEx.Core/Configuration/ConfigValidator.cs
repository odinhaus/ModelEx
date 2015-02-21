using Altus.Core.Component;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Altus.Core.Configuration
{
    public enum ConfigMessageLevel
    {
        Information,
        Warning,
        Error
    }

    public class ConfigMessage
    {
        private ConfigMessage() { }

        public string Message { get; private set; }
        public ConfigMessageLevel Level { get; private set; }

        public static ConfigMessage CreateInfo(string message)
        {
            return new ConfigMessage() { Message = message, Level = ConfigMessageLevel.Information };
        }
        public static ConfigMessage CreateWarn(string message)
        {
            return new ConfigMessage() { Message = message, Level = ConfigMessageLevel.Warning };
        }
        public static ConfigMessage CreateError(string message)
        {
            return new ConfigMessage() { Message = message, Level = ConfigMessageLevel.Error };
        }
    }

    public abstract class ConfigValidator<T> : InitializableComponent
    {
        protected ConfigValidator(bool updateConfig, string xmlNameSpace, string xsdPath)
        {
            this.UpdateConfig = updateConfig;
            this.XMLNamespace = xmlNameSpace;
            this.XSDPath = xsdPath;
        }

        public bool UpdateConfig { get; private set; }

        public bool Validate(string configPath, out IEnumerable<ConfigMessage> errors)
        {
            ConfigPath = configPath;
            XSDPath = Path.Combine(Path.GetDirectoryName(configPath), XSDPath);
            if (!OnValidateSchema())
            {
                errors = _schemaErrors.ToArray();
                return false;
            }
            else
            {
                T config = default(T);
                XmlSerializer s = new XmlSerializer(typeof(T));
                
                try
                {
                    using (StreamReader rdr = System.IO.File.OpenText(configPath))
                    {
                        config = (T)s.Deserialize(rdr);
                    }
                    Config = config;
                }
                catch
                {
                    HasErrors = true;
                    errors = new ConfigMessage[] { ConfigMessage.CreateError("The config file specified could either not be found, or does not conform to the correct schema.") };
                    return false;
                }
                bool ret = OnValidate(config, out errors);
                HasErrors = errors.Count(cm => cm.Level == ConfigMessageLevel.Error) > 0;
                HasWarnings = errors.Count(cm => cm.Level == ConfigMessageLevel.Warning) > 0;
                HasInfos = errors.Count(cm => cm.Level == ConfigMessageLevel.Information) > 0;
                if (UpdateConfig)
                {
                    using (FileStream fs = System.IO.File.Open(this.ConfigPath, FileMode.Create))
                    {
                        s.Serialize(fs, config);
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Validate the config file against the XSD, return false if their are errors
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateSchema()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += new ValidationEventHandler(OnSchemaValidationError);
            try
            {
                settings.Schemas.Add(this.XMLNamespace, this.XSDPath);
            }
            catch
            {
                _schemaErrors.Add(ConfigMessage.CreateError("Schema file " + XSDPath + " could not be found."));
                HasSchemaErrors = true;
                HasErrors = true;
            }
            if (!HasSchemaErrors)
            {
                // Create the XmlReader object.
                using (XmlReader reader = XmlReader.Create(this.ConfigPath, settings))
                {
                    // Parse the file. 
                    while (reader.Read()) ;
                }
            }
            return !HasSchemaErrors;
        }

        List<ConfigMessage> _schemaErrors = new List<ConfigMessage>();
        protected virtual void OnSchemaValidationError(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.WriteLine("\tWarning: Matching schema not found.  No validation occurred." + args.Message);
            else
            {
                HasSchemaErrors = true;
                HasErrors = true;
                _schemaErrors.Add(ConfigMessage.CreateError(string.Format("XML Schema error: {0}; Line Number: {1}; Position: {2}; File: {3}",
                    args.Message, args.Exception.LineNumber, args.Exception.LinePosition, args.Exception.SourceUri)));
            }

        }

        /// <summary>
        /// Validate the config object, return false if there are validation errors
        /// </summary>
        /// <param name="config"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        protected abstract bool OnValidate(T config, out IEnumerable<ConfigMessage> errors);

        public T Config { get; private set; }
        public string ConfigPath { get; private set; }
        public bool HasErrors { get; private set; }
        public bool HasWarnings { get; private set; }
        public bool HasInfos { get; private set; }
        public bool HasSchemaErrors { get; private set; }
        public string XMLNamespace { get; private set; }
        public string XSDPath { get; private set; }
    }
}
