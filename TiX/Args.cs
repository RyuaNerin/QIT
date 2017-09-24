using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;

namespace TiX
{
    internal class Args
    {
        public Args()
        {
        }

        public static Args CreateOption<T>(T option)
        {
            var tixOption = Activator.CreateInstance<Args>();

            foreach (var prop in option.GetType().GetProperties())
            {
                if (!prop.CustomAttributes.Any(e => e.AttributeType == typeof(OptionAttribute)))
                    continue;

                if (prop.PropertyType == typeof(T))
                {
                    prop.SetValue(tixOption, option);
                    break;
                }
            }

            return tixOption;
        }

        [Option("install")] public int    OptionInstallation    { get; set; }

        [Option("statis")]  public bool   CaptureScreenPart     { get; set; }

        [Option("pipe"  )]  public bool   UsePipe               { get; set; }
        [Option("notext")]  public bool   TweetWithoutText      { get; set; }

        [Option("text"  )]  public string Text                  { get; set; }
        [Option("reply" )]  public string In_Reply_To_Status_Id { get; set; }

        [Option("sd"    )]  public string SchemeData            { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> Files { get; set; }

        public static readonly Args Default = new Args();

        public static Args Parse(string[] args)
        {
            var option = new Args();
            try
            {
                return Parser.Default.ParseArguments(args, option) ? option : Default;
            }
            catch
            {
                return Default;
            }
        }
        public override string ToString()
        {
            var sb = new StringBuilder(256);

            return AppendOption(sb, this).ToString();
        }

        private static StringBuilder AppendOption(StringBuilder sb, object obj)
        {
            var type = obj.GetType();

            foreach (PropertyInfo oProp in type.GetProperties())
            {
                var option = oProp.GetCustomAttribute<OptionAttribute>();
                if (option == null)
                    continue;

                var value = oProp.GetValue(obj);
                if (value != null && !value.Equals(GetDefaultValue(oProp.PropertyType)))
                {
                    sb.Append($"--{option.LongName} ");

                    if (oProp.PropertyType != typeof(bool))
                    {
                        if (oProp.PropertyType.IsEnum)
                            value = Convert.ChangeType(value, Enum.GetUnderlyingType(oProp.PropertyType));

                        sb.Append($"\"{value}\" ");
                    }
                }
            }

            var vprop = type.GetProperties().FirstOrDefault(e => e.GetCustomAttribute<ValueListAttribute>() != null);
            if (vprop != null)
            {
                var lst = vprop.GetValue(obj) as IEnumerable<string>;
                if (lst != null)

                    foreach (var v in lst)
                        sb.Append($"\"{v}\" ");
            }

            return sb;
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
