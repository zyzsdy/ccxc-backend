using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Ccxc.Core.Utils
{
    public class SystemOption
    {
        internal const string DefaultConfigFileName = "Config.default.xml";

        /// <summary>
        /// 从单个配置文件xml中解析配置文件。
        /// </summary>
        /// <typeparam name="T">Option实体类</typeparam>
        /// <param name="configPath">配置文件路径</param>
        /// <returns></returns>
        public static T GetOption<T>(string configPath) where T : class, new()
        {
            var configDirectory = Path.GetDirectoryName(configPath) ?? "Config";
            if (!Directory.Exists(configDirectory))
            {
                Logger.Info("Config目录不存在，建立该目录");
                Directory.CreateDirectory(configDirectory);
            }

            //更新默认配置文件
            var defaultConfigFile = Path.GetFileNameWithoutExtension(configPath) + ".default.xml";
            var defaultConfigFullPath = Path.Combine(configDirectory, defaultConfigFile);
            DefaultConfigFileGenerate<T>(defaultConfigFullPath);

            //判断配置文件是否存在，如果不存在则生成默认配置，返回new T()
            if (!File.Exists(configPath))
            {
                Logger.Info($"配置文件 {Path.GetFullPath(configPath)} 未找到，将使用默认配置。" +
                    $"请查看生成的 {defaultConfigFile} 配置文件模板，" +
                    $"并将需要修改的配置复制到 {Path.GetFileName(configPath)} 中");

                var configXml = new XmlDocument();
                var xmlVersion = configXml.CreateXmlDeclaration("1.0", "utf-8", null);
                var configRoot = configXml.CreateElement("ConfigRoot");
                configXml.AppendChild(xmlVersion);
                configXml.AppendChild(configRoot);
                configXml.Save(configPath);
                return new T();
            }

            //从配置文件中读取配置
            return GetConfigObjectFromSingleFile<T>(configPath);
        }

        private static void DefaultConfigFileGenerate<T>(string defaultConfigFile) where T : class, new()
        {
            var configXml = new XmlDocument();
            var xmlVersion = configXml.CreateXmlDeclaration("1.0", "utf-8", null);
            var configRoot = configXml.CreateElement("ConfigRoot");
            configXml.AppendChild(xmlVersion);
            configXml.AppendChild(configRoot);
            var defaultOption = new T();

            foreach (var props in typeof(T).GetProperties())
            {
                var info = Attribute.GetCustomAttributes(props);
                foreach (var attr in info)
                {
                    if (!(attr is OptionDescriptionAttribute attribute)) continue;
                    var name = props.Name;
                    var value = props.GetValue(defaultOption).ToString();
                    var desc = attribute.Desc;

                    var configOption = configXml.CreateElement("Option");
                    configOption.SetAttribute("Name", name);
                    configOption.SetAttribute("Value", value);

                    var configComment = configXml.CreateComment(desc);

                    configRoot.AppendChild(configComment);
                    configRoot.AppendChild(configOption);
                }
            }

            configXml.Save(defaultConfigFile);
        }

        private static T GetConfigObjectFromSingleFile<T>(string configFile) where T : class, new()
        {
            var tenantConfigXml = new XmlDocument();
            tenantConfigXml.Load(configFile);
            var root = tenantConfigXml.SelectSingleNode("ConfigRoot");

            var tenantOption = new T();

            foreach (var props in typeof(T).GetProperties())
            {
                var info = Attribute.GetCustomAttributes(props);
                foreach (var attr in info)
                {
                    if (attr is OptionDescriptionAttribute)
                    {
                        var name = props.Name;
                        var existedConfig = root.SelectSingleNode($"//Option[@Name=\"{name}\"]");

                        if (existedConfig != null)
                        {
                            string ecValue = null;
                            Debug.Assert(existedConfig.Attributes != null, "existedConfig.Attributes != null");
                            foreach (XmlAttribute ecAtt in existedConfig.Attributes)
                            {
                                if (ecAtt.Name == "Value")
                                {
                                    ecValue = ecAtt.Value;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(ecValue))
                            {
                                switch (props.PropertyType.Name)
                                {
                                    case "Int32":
                                        int.TryParse(ecValue, out int i32);
                                        props.SetValue(tenantOption, i32);
                                        break;
                                    case "Int64":
                                        long.TryParse(ecValue, out long i64);
                                        props.SetValue(tenantOption, i64);
                                        break;
                                    case "String":
                                        props.SetValue(tenantOption, ecValue);
                                        break;
                                    case "Boolean":
                                        bool.TryParse(ecValue, out bool b);
                                        props.SetValue(tenantOption, b);
                                        break;
                                    case "Double":
                                        double.TryParse(ecValue, out double dbl);
                                        props.SetValue(tenantOption, dbl);
                                        break;
                                    default:
                                        {
                                            var tryParse = props.PropertyType.GetMethod("TryParse", new Type[] { typeof(string), props.PropertyType });
                                            if (tryParse == null)
                                            {
                                                throw new NotImplementedException($"无法将数据解析为{props.PropertyType.Name}类型，请实现 bool {props.PropertyType.FullName}.TryParse(string, out {props.PropertyType}) 方法.");
                                            }
                                            var tryParseInvokeArgs = new object[] { ecValue, null };
                                            tryParse.Invoke(null, tryParseInvokeArgs);
                                            props.SetValue(tenantOption, tryParseInvokeArgs[1]);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }

            return tenantOption;
        }
    }

    /// <summary>
    /// 标注该属性为一个系统配置项
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class OptionDescriptionAttribute : Attribute
    {
        /// <summary>
        /// 该系统配置项的描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 标注该属性为一个系统配置项
        /// </summary>
        /// <param name="value">该配置项的描述</param>
        public OptionDescriptionAttribute(string value)
        {
            Desc = value;
        }
    }
}
