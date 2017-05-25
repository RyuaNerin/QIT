using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

namespace TiX.Utilities
{
    internal class SettingBinder
    {
        private enum BindingType
        {
            Bool,
            String,
            Int
        }
        private struct BindingOption
        {
            public BindingType  type;
            public object       Obj;
            public bool         Skip;

            public PropertyInfo Property;
            public CheckBox     CheckBox;
            public TextBox      TextBox;
            public IDictionary<int, RadioButton> RadioButtons;
        }

        private readonly List<BindingOption> m_binding = new List<BindingOption>();

        public void Add<T>(T obj, Expression<Func<T, bool>> expression, CheckBox checkBox, bool skip = false)
        {
            this.Add(obj, expression.Body, BindingType.Bool, checkBox, null, null, skip);
        }
        public void Add<T>(T obj, Expression<Func<T, string>> expression, TextBox textBox, bool skip = false)
        {
            this.Add(obj, expression.Body, BindingType.String, null, textBox, null, skip);
        }
        public void Add<T>(T obj, Expression<Func<T, int>> expression, Dictionary<int, RadioButton> radioButtons, bool skip = false)
        {
            this.Add(obj, expression.Body, BindingType.Int, null, null, radioButtons, skip);
        }

        private void Add(object obj, Expression expression, BindingType type, CheckBox checkbox, TextBox textBox, Dictionary<int, RadioButton> radioButtons, bool skip)
        {
            if (!(expression is MemberExpression))
                throw new NotSupportedException("expression must be MemberExpression");

            this.m_binding.Add(new BindingOption
            {
                Property     = obj.GetType().GetProperty(((MemberExpression)expression).Member.Name),
                Obj          = obj,
                Skip         = skip,

                type         = type,
                CheckBox     = checkbox,
                TextBox      = textBox,
                RadioButtons = radioButtons,
            });
        }

        public void FromSetting()
        {
            foreach (var binding in this.m_binding)
            {
                var value = binding.Property.GetValue(binding.Obj);

                switch (binding.type)
                {
                    case BindingType.Bool:
                        binding.CheckBox.Checked = (bool)value;
                        break;

                    case BindingType.String:
                        binding.TextBox.Text = (string)value;
                        break;

                    case BindingType.Int:
                        {
                            var v = Convert.ToInt32(value);
                            foreach (var st in binding.RadioButtons)
                                st.Value.Checked = st.Key == v;
                        }
                        break;
                }
            }
        }

        public void ToSetting()
        {
            foreach (var binding in this.m_binding)
            {
                if (binding.Skip)
                    continue;

                object value = null;
                switch (binding.type)
                {
                    case BindingType.Bool:
                        value = binding.CheckBox.Checked;
                        break;
                    case BindingType.String:
                        value = binding.TextBox.Text;
                        break;
                    case BindingType.Int:
                        value = 0;
                        foreach (var st in binding.RadioButtons)
                        {
                            if (st.Value.Checked)
                            {
                                value = st.Key;
                                break;
                            }
                        }
                        break;
                }

                binding.Property.SetValue(binding.Obj, value);
            }
        }
    }
}