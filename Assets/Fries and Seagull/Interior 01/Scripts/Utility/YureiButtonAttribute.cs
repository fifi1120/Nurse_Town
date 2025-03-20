using System;

namespace Seagull.Interior_01.Utility {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class YureiButtonAttribute : Attribute {
        public string text { get; set; } = null;
        
        public YureiButtonAttribute(string buttonText) {
            text = buttonText;
        }
    }
}