using DSharpPlus.Commands;
using System.Text;

namespace KurisuBot
{
    public enum OperationType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Set
    }

    public class UniversalHelper
    {
        public static string AddSpacesInEnum(string enumString, int maxSpaceCount = 5)
        {
            StringBuilder sb = new StringBuilder(enumString.Length + maxSpaceCount);

            for(int i = 0; i < enumString.Length; i++)
            {
                if(i > 0 && char.IsUpper(enumString[i]))
                {
                    sb.Append(' ');
                }

                sb.Append(enumString[i]);
            }

            return sb.ToString();
        }
    }
}
