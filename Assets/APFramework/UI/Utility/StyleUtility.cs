using Cysharp.Text;
using Godot;

namespace ChosenConcept.APFramework.UI.Utility
{
    public class StyleUtility
    {
        public static Color selected = new Color(10f / 255f, 239f / 255f, 254f / 255f, 1f);
        public static Color disableSelected = new Color(0f, 90f / 255f, 125f / 255f, 1f);
        public static Color disabled = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);

        public static string StringColored(string text, Color color)
        {
            return ZString.Concat("<color=#", color.ToHtml(), ">", text, "</color>");
        }

        public static string StringColoredRange(string text, Color color, int min, int max)
        {
            int actualMin = Mathf.Min(min, max);
            int actualMax = Mathf.Max(min, max);
            using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
            {
                if (actualMin > 0)
                    builder.Append(text.Substring(0, actualMin));
                builder.Append(StringColored(text.Substring(actualMin, actualMax - actualMin), color));
                if (text.Length - actualMax > 0)
                    builder.Append(text.Substring(actualMax, text.Length - actualMax));
                return builder.ToString();
            }
        }

        public static string Sized(string tag, int size)
        {
            return ZString.Concat("<size=", size, ">", tag, "</size>");
        }

        public static string StringTransparent(string text, int alpha)
        {
            return ZString.Concat("<alpha=#", alpha.ToString("X2"), ">");
        }

        public static string StringBold(string text)
        {
            return ZString.Concat("<b>", text, "</b>");
        }

        public static Color DarkenColor(Color color, float percentage)
        {
            float h, s, v;
            color.ToHsv(out h, out s, out v);
            return Color.FromHsv(h, s, v * Mathf.Clamp(percentage, 0f, 1f));
        }

        public static Color ClearColor(Color color) => new Color(color.R, color.G, color.B, 0);
        public static Color FullColor(Color color) => new Color(color.R, color.G, color.B, 1);
        public static Color AlphaColor(Color color, float alpha) => new Color(color.R, color.G, color.B, alpha);
    }
}